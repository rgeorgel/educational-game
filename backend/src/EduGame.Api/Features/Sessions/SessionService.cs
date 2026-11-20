using EduGame.Api.Common;
using EduGame.Api.Features.Map;
using EduGame.Api.Features.Progress;
using EduGame.Domain.Content;
using EduGame.Domain.Players;
using EduGame.Domain.Progression;
using EduGame.Domain.Sessions;
using EduGame.Infrastructure;
using EduGame.Problems;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EduGame.Api.Features.Sessions;

public sealed record SkillRef(string Slug, string Name);

/// <summary>Problema como o cliente vê: sem resposta e sem dicas ainda não reveladas.</summary>
public sealed record ProblemDto(
    Guid Id,
    int Number,
    int Total,
    string Mode,
    string Type,
    string Prompt,
    string? Visual,
    IReadOnlyList<string>? Choices,
    IReadOnlyList<string?>? Sequence,
    string? Code,
    GridPuzzle? Grid,
    FractionFigure? Figure,
    bool AcceptsNegative,
    bool AcceptsDecimal,
    int HintsTotal,
    IReadOnlyList<string> Hints,
    SkillRef Skill);

public sealed record NextProblemResponse(bool Done, ProblemDto? Problem);

public sealed record HintResponse(IReadOnlyList<string> Hints, int HintsTotal);

public sealed record AnswerResponse(
    bool Correct,
    string CorrectAnswer,
    string Explanation,
    int XpEarned,
    int RatingBefore,
    int RatingAfter,
    int Answered,
    int CorrectCount,
    int Total,
    bool SessionFinished);

public sealed record StageRef(int Id, string Name);

public sealed record SessionSummary(
    Guid SessionId,
    string Mode,
    string SubjectSlug,
    string SubjectName,
    StageRef? Stage,
    bool Finished,
    int Correct,
    int Answered,
    int Total,
    int Stars,
    int BestStars,
    int XpEarned,
    StageRef? NextStage,
    bool NextStageUnlocked,
    bool? GuardianPassed,
    LevelDto? ResultLevel,
    IReadOnlyList<AchievementDto> Achievements,
    SubjectProgressDto Progress,
    IReadOnlyList<string> Marks,
    DateOnly? DailyDate);

public enum SessionError { NotFound, Locked, Finished, AlreadyAnswered, NotServed, NoHintsLeft, NotAvailable }

public sealed class SessionException(SessionError error, string message) : Exception(message)
{
    public SessionError Error { get; } = error;
}

/// <summary>
/// Regras de uma sessão de jogo (etapa, Desafio do Guardião ou nivelamento): escolhe o próximo
/// problema perto do nível do jogador, revela dicas, valida respostas, atualiza rating/XP/atividade
/// e fecha a sessão (estrelas, promoção de nível, medalhas).
/// </summary>
public sealed class SessionService(AppDbContext db, ProgressService progress, GameClock clock)
{
    private static readonly ProblemFactory Factory = ProblemFactory.Default;

    // ---------- Início ----------

    public async Task<GameSession> StartStageAsync(PlayerProfile profile, int stageId, CancellationToken ct)
    {
        var stage = await db.Stages.Include(s => s.Region).SingleOrDefaultAsync(s => s.Id == stageId, ct)
            ?? throw new SessionException(SessionError.NotFound, "Etapa não encontrada.");

        var subjectId = stage.Region!.SubjectId;
        var ordered = await StageOrder.ForSubjectAsync(db, subjectId, ct);
        var bestStars = await BestStarsAsync(profile.Id, ct);
        var current = await progress.CurrentLevelAsync(profile.Id, subjectId, ct);
        var index = ordered.FindIndex(s => s.Id == stage.Id);
        // Etapa pedida pelo professor (tarefa de uma turma do aluno) pode ser jogada mesmo bloqueada no mapa.
        if (!StageOrder.IsUnlocked(ordered, index, bestStars, current.Order) && !await IsAssignedAsync(profile.Id, stage.Id, ct))
            throw new SessionException(SessionError.Locked, "Esta etapa ainda está bloqueada. Conclua a anterior primeiro!");

        return await CreateAsync(new GameSession
        {
            ProfileId = profile.Id,
            SubjectId = subjectId,
            StageId = stage.Id,
            Mode = SessionModes.Stage,
            SkillIds = [.. stage.SkillIds],
            ProblemCount = stage.ProblemCount,
        }, ct);
    }

    public async Task<GameSession> StartGuardianAsync(PlayerProfile profile, Subject subject, CancellationToken ct)
    {
        var status = await progress.ForSubjectAsync(profile.Id, subject, ct);
        if (!status.GuardianAvailable)
            throw new SessionException(SessionError.NotAvailable, $"O Desafio do Guardião abre quando o avanço do nível chega a {LevelProgress.GuardianThreshold}%.");

        var level = await progress.CurrentLevelAsync(profile.Id, subject.Id, ct);
        var skills = await db.Skills.Where(s => s.SubjectId == subject.Id && s.LevelId == level.Id).Select(s => s.Id).ToListAsync(ct);
        return await CreateAsync(new GameSession
        {
            ProfileId = profile.Id,
            SubjectId = subject.Id,
            LevelId = level.Id,
            Mode = SessionModes.Guardian,
            SkillIds = skills,
            ProblemCount = Guardian.ProblemCount,
        }, ct);
    }

    public async Task<GameSession> StartPlacementAsync(PlayerProfile profile, Subject subject, CancellationToken ct)
    {
        var status = await progress.ForSubjectAsync(profile.Id, subject, ct);
        if (!status.PlacementAvailable)
            throw new SessionException(SessionError.NotAvailable, "O teste de nivelamento só pode ser feito antes de começar a trilha.");

        var skills = await db.Skills.Where(s => s.SubjectId == subject.Id).Select(s => s.Id).ToListAsync(ct);
        return await CreateAsync(new GameSession
        {
            ProfileId = profile.Id,
            SubjectId = subject.Id,
            Mode = SessionModes.Placement,
            SkillIds = skills,
            ProblemCount = Placement.ProblemCount,
            PlacementEstimate = Placement.StartRating(profile.SchoolGrade),
        }, ct);
    }

    /// <summary>Desafio diário do tópico (um por dia). Se já começou hoje e não terminou, retoma.</summary>
    public async Task<GameSession> StartDailyAsync(PlayerProfile profile, Subject subject, CancellationToken ct)
    {
        var today = clock.Today;
        var existing = await db.GameSessions.SingleOrDefaultAsync(s => s.ProfileId == profile.Id && s.SubjectId == subject.Id && s.DailyDate == today, ct);
        if (existing is not null)
        {
            if (existing.IsFinished)
                throw new SessionException(SessionError.Finished, "Você já fez o desafio de hoje. Volte amanhã para um novo!");
            return existing;
        }

        var level = await progress.CurrentLevelAsync(profile.Id, subject.Id, ct);
        var skills = await db.Skills.Where(s => s.SubjectId == subject.Id && s.LevelId == level.Id).Select(s => s.Id).ToListAsync(ct);
        return await CreateAsync(new GameSession
        {
            ProfileId = profile.Id,
            SubjectId = subject.Id,
            LevelId = level.Id,
            Mode = SessionModes.Daily,
            SkillIds = skills,
            ProblemCount = 1,
            DailyDate = today,
        }, ct);
    }

    /// <summary>Revisão: 8 problemas das habilidades que mais precisam de prática.</summary>
    public async Task<GameSession> StartReviewAsync(PlayerProfile profile, Subject subject, CancellationToken ct)
    {
        var current = await progress.CurrentLevelAsync(profile.Id, subject.Id, ct);
        var rows = await db.SkillMasteries
            .Where(m => m.ProfileId == profile.Id)
            .Join(db.Skills, m => m.SkillId, s => s.Id, (m, s) => new { m, s.SubjectId, LevelSlug = s.Level!.Slug, LevelOrder = s.Level.Order })
            .Where(x => x.SubjectId == subject.Id && x.LevelOrder <= current.Order)
            .ToListAsync(ct);
        var picked = Review.PickSkills(
            rows.Select(x => new Review.Candidate(x.m.SkillId, LevelProgress.BySlug(x.LevelSlug), x.m.Rating, x.m.AttemptsCount, x.m.LastPracticedAt)),
            clock.Now);
        if (picked.Count == 0)
            throw new SessionException(SessionError.NotAvailable, "Jogue algumas etapas primeiro: a revisão usa o que você já praticou.");

        return await CreateAsync(new GameSession
        {
            ProfileId = profile.Id,
            SubjectId = subject.Id,
            Mode = SessionModes.Review,
            SkillIds = [.. picked],
            ProblemCount = Review.ProblemCount,
        }, ct);
    }

    /// <summary>Marca o nivelamento como dispensado: o jogador prefere começar do início.</summary>
    public async Task SkipPlacementAsync(PlayerProfile profile, Subject subject, CancellationToken ct)
    {
        var sp = await GetOrCreateSubjectProgressAsync(profile.Id, subject.Id, ct);
        sp.PlacementDone = true;
        await db.SaveChangesAsync(ct);
    }

    private async Task<GameSession> CreateAsync(GameSession session, CancellationToken ct)
    {
        session.StartedAt = clock.Now;
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    // ---------- Próximo problema ----------

    public async Task<NextProblemResponse> NextAsync(GameSession session, CancellationToken ct)
    {
        if (session.IsFinished) return new(true, null);

        var problems = await LoadProblemsAsync(session.Id, ct);
        var pending = problems.LastOrDefault(p => p.Attempt is null);
        if (pending is not null) return new(false, await ToDtoAsync(pending, session, ct));
        if (problems.Count >= session.ProblemCount) return new(true, null);

        var (skillId, generatorKey, target, fixedSeed) = await ChooseNextAsync(session, problems, ct);

        var template = await db.ProblemTemplates.SingleAsync(t => t.SkillId == skillId && t.GeneratorKey == generatorKey, ct);
        var seed = fixedSeed ?? Random.Shared.Next();
        var avoid = problems.Select(p => ProblemFactory.Fingerprint(p.Content)).ToHashSet();
        // A calibração desloca a dificuldade "real" do gerador.
        GeneratedProblem generated;
        if (generatorKey == ProblemFactory.CuratedKey)
        {
            var curated = await PickCuratedAsync(session, skillId, target - template.DifficultyOffset, fixedSeed, ct);
            seed = curated.Id; // a "semente" de um problema curado é o id dele
            generated = new GeneratedProblem(curated.Content, curated.Difficulty);
        }
        else
        {
            generated = Factory.Generate(generatorKey, target - template.DifficultyOffset, seed, avoid);
        }

        var problem = new SessionProblem
        {
            SessionId = session.Id,
            Order = problems.Count,
            SkillId = skillId,
            GeneratorKey = generatorKey,
            Seed = seed,
            Difficulty = generated.Difficulty + template.DifficultyOffset,
            Content = generated.Content,
            ServedAt = clock.Now,
        };
        db.SessionProblems.Add(problem);
        await db.SaveChangesAsync(ct);
        return new(false, await ToDtoAsync(problem, session, ct));
    }

    private async Task<(int SkillId, string GeneratorKey, int Target, int? Seed)> ChooseNextAsync(GameSession session, List<SessionProblem> served, CancellationToken ct)
    {
        var last = served.LastOrDefault();

        // Etapa/revisão: depois de um erro vem um "problema irmão" (mesma habilidade e gerador), um pouco mais fácil.
        if (session.Mode is SessionModes.Stage or SessionModes.Review && last?.Attempt is { IsCorrect: false })
            return (last.SkillId, last.GeneratorKey, Math.Max(Rating.Min, last.Difficulty - 100), null);

        if (session.Mode == SessionModes.Daily)
        {
            // Mesmo problema para todos do nível: habilidade, gerador e semente vêm da data.
            var level = await db.Levels.SingleAsync(l => l.Id == session.LevelId, ct);
            var subject = await db.Subjects.SingleAsync(x => x.Id == session.SubjectId, ct);
            var daySeed = DailyChallenge.Seed(session.DailyDate!.Value, subject.Slug, level.Slug);
            var skillIds = await db.Skills.Where(x => session.SkillIds.Contains(x.Id)).OrderBy(x => x.Order).Select(x => x.Id).ToListAsync(ct);
            var dailySkill = skillIds[DailyChallenge.Pick(daySeed, skillIds.Count)];
            var dailyKeys = await db.ProblemTemplates.Where(t => t.SkillId == dailySkill && t.Active).OrderBy(t => t.GeneratorKey).Select(t => t.GeneratorKey).ToListAsync(ct);
            return (dailySkill, dailyKeys[DailyChallenge.Pick(daySeed / 7, dailyKeys.Count)], DailyChallenge.TargetDifficulty(LevelProgress.BySlug(level.Slug)), daySeed);
        }

        var candidates = session.SkillIds;
        int? target = null;

        if (session.Mode == SessionModes.Placement)
        {
            var estimate = session.PlacementEstimate ?? Rating.Initial;
            var maxLevel = await progress.MaxContentLevelOrderAsync(session.SubjectId, ct);
            var level = Placement.LevelToAsk(estimate, maxLevel);
            candidates = await db.Skills.Where(s => session.SkillIds.Contains(s.Id) && s.Level!.Slug == level.Slug).Select(s => s.Id).ToListAsync(ct);
            target = estimate;
        }
        else if (session.Mode == SessionModes.Guardian)
        {
            var level = await db.Levels.SingleAsync(l => l.Id == session.LevelId, ct);
            target = Guardian.TargetDifficulty(LevelProgress.BySlug(level.Slug), Random.Shared);
        }

        var skillId = ChooseSkill(candidates, served);
        var keys = await db.ProblemTemplates.Where(t => t.SkillId == skillId && t.Active).Select(t => t.GeneratorKey).ToListAsync(ct);
        var key = keys[Random.Shared.Next(keys.Count)];

        target ??= SessionTarget(await RatingOfAsync(session.ProfileId, skillId, ct));
        return (skillId, key, target.Value, null);
    }

    /// <summary>
    /// Escolhe um problema curado: prefere os que o jogador ainda não viu e, entre eles,
    /// o de dificuldade mais próxima do alvo. No desafio diário a escolha vem da semente do dia.
    /// </summary>
    private async Task<CuratedProblem> PickCuratedAsync(GameSession session, int skillId, int target, int? daySeed, CancellationToken ct)
    {
        var all = await db.CuratedProblems.Where(p => p.SkillId == skillId && p.Active).OrderBy(p => p.Id).ToListAsync(ct);
        if (daySeed is { } ds) return all[DailyChallenge.Pick(ds, all.Count)];

        var seen = await db.SessionProblems
            .Where(p => p.GeneratorKey == ProblemFactory.CuratedKey && p.SkillId == skillId && db.GameSessions.Any(s => s.Id == p.SessionId && s.ProfileId == session.ProfileId))
            .Select(p => p.Seed).ToListAsync(ct);
        var pool = all.Where(p => !seen.Contains(p.Id)).ToList();
        if (pool.Count == 0)
        {
            var inSession = await db.SessionProblems.Where(p => p.SessionId == session.Id).Select(p => p.Seed).ToListAsync(ct);
            pool = all.Where(p => !inSession.Contains(p.Id)).ToList();
            if (pool.Count == 0) pool = all;
        }
        return pool.OrderBy(p => Math.Abs(p.Difficulty - target)).ThenBy(_ => Random.Shared.Next()).First();
    }

    /// <summary>
    /// Zona de desafio: 70% perto do rating, 20% um pouco abaixo (confiança), 10% um pouco acima (desafio).
    /// </summary>
    public static int SessionTarget(int rating, Random? rng = null)
    {
        rng ??= Random.Shared;
        var roll = rng.NextDouble();
        var offset = roll switch
        {
            < 0.7 => rng.Next(-100, 101),
            < 0.9 => -rng.Next(100, 251),
            _ => rng.Next(100, 251),
        };
        return Math.Clamp(rating + offset, Rating.Min, Rating.Max);
    }

    /// <summary>Equilibra as habilidades: escolhe a menos sorteada até agora (empates ao acaso).</summary>
    private static int ChooseSkill(IReadOnlyList<int> skillIds, IReadOnlyList<SessionProblem> served)
    {
        var counts = skillIds.ToDictionary(id => id, id => served.Count(p => p.SkillId == id));
        var min = counts.Values.Min();
        var candidates = counts.Where(c => c.Value == min).Select(c => c.Key).ToList();
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    private async Task<int> RatingOfAsync(Guid profileId, int skillId, CancellationToken ct)
    {
        var rating = await db.SkillMasteries.Where(m => m.ProfileId == profileId && m.SkillId == skillId).Select(m => (int?)m.Rating).SingleOrDefaultAsync(ct);
        return rating ?? await InitialRatingAsync(skillId, ct);
    }

    private async Task<int> InitialRatingAsync(int skillId, CancellationToken ct)
    {
        var levelSlug = await db.Skills.Where(s => s.Id == skillId).Select(s => s.Level!.Slug).SingleAsync(ct);
        return Rating.InitialFor(LevelProgress.BySlug(levelSlug));
    }

    // ---------- Dicas e respostas ----------

    public async Task<HintResponse> HintAsync(GameSession session, Guid problemId, CancellationToken ct)
    {
        var problem = await FindProblemAsync(session, problemId, ct);
        if (problem.Attempt is not null)
            throw new SessionException(SessionError.AlreadyAnswered, "Este problema já foi respondido.");
        if (session.Mode == SessionModes.Placement)
            throw new SessionException(SessionError.NoHintsLeft, "No teste de nivelamento não há dicas — tudo bem não saber!");
        if (problem.HintsRevealed >= problem.Content.Hints.Count)
            throw new SessionException(SessionError.NoHintsLeft, "Não há mais dicas para este problema.");

        problem.HintsRevealed++;
        await db.SaveChangesAsync(ct);
        return new([.. problem.Content.Hints.Take(problem.HintsRevealed)], problem.Content.Hints.Count);
    }

    public async Task<AnswerResponse> AnswerAsync(GameSession session, Guid problemId, string answer, CancellationToken ct)
    {
        if (session.IsFinished)
            throw new SessionException(SessionError.Finished, "Esta sessão já terminou.");
        var problem = await FindProblemAsync(session, problemId, ct);
        if (problem.Attempt is not null)
            throw new SessionException(SessionError.AlreadyAnswered, "Este problema já foi respondido.");

        var now = clock.Now;
        var correct = AnswerChecker.IsCorrect(problem.Content, answer);

        // Domínio da habilidade
        var mastery = await db.SkillMasteries.SingleOrDefaultAsync(m => m.ProfileId == session.ProfileId && m.SkillId == problem.SkillId, ct);
        if (mastery is null)
        {
            mastery = new SkillMastery { ProfileId = session.ProfileId, SkillId = problem.SkillId, Rating = await InitialRatingAsync(problem.SkillId, ct) };
            db.SkillMasteries.Add(mastery);
        }
        var ratingBefore = mastery.Rating;
        mastery.Rating = Rating.Update(mastery.Rating, problem.Difficulty, Rating.Outcome(correct, problem.HintsRevealed), mastery.AttemptsCount);
        mastery.AttemptsCount++;
        if (correct) mastery.CorrectCount++;
        mastery.LastPracticedAt = now;

        // XP e atividade do dia
        var xp = Rewards.Xp(correct, problem.Difficulty, problem.HintsRevealed);
        if (session.Mode == SessionModes.Daily && correct) xp *= DailyChallenge.XpMultiplier;
        var subjectProgress = await GetOrCreateSubjectProgressAsync(session.ProfileId, session.SubjectId, ct);
        subjectProgress.Xp += xp;
        await AddDailyActivityAsync(session.ProfileId, correct, xp, ct);

        if (session.Mode == SessionModes.Placement)
        {
            var index = await db.SessionProblems.CountAsync(p => p.SessionId == session.Id && p.Attempt != null, ct);
            session.PlacementEstimate = Placement.Next(session.PlacementEstimate ?? Rating.Initial, correct, index);
        }

        // Adicionado explicitamente: com o Id já preenchido, o EF trataria como registro existente.
        problem.Attempt = new Attempt
        {
            SessionProblemId = problem.Id,
            Answer = answer.Trim()[..Math.Min(answer.Trim().Length, 200)],
            IsCorrect = correct,
            HintsUsed = problem.HintsRevealed,
            TimeMs = (int)Math.Clamp((now - problem.ServedAt).TotalMilliseconds, 0, 600_000),
            RatingBefore = ratingBefore,
            RatingAfter = mastery.Rating,
            XpEarned = xp,
            CreatedAt = now,
        };
        db.Attempts.Add(problem.Attempt);

        session.XpEarned += xp;
        if (correct) session.CorrectCount++;

        var answered = await db.Attempts.CountAsync(a => db.SessionProblems.Any(p => p.Id == a.SessionProblemId && p.SessionId == session.Id), ct) + 1;

        try
        {
            await db.SaveChangesAsync(ct);
            if (answered >= session.ProblemCount)
            {
                await FinishAsync(session, subjectProgress, now, ct);
                await db.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Duas respostas simultâneas para o mesmo problema: o índice único barra a segunda.
            throw new SessionException(SessionError.AlreadyAnswered, "Este problema já foi respondido.");
        }

        return new AnswerResponse(
            correct,
            problem.Content.Answer,
            problem.Content.Explanation,
            xp,
            ratingBefore,
            mastery.Rating,
            answered,
            session.CorrectCount,
            session.ProblemCount,
            session.IsFinished);
    }

    // ---------- Fim da sessão ----------

    private async Task FinishAsync(GameSession session, SubjectProgress subjectProgress, DateTimeOffset now, CancellationToken ct)
    {
        session.FinishedAt = now;
        session.Stars = Rewards.Stars(session.CorrectCount, session.ProblemCount);

        switch (session.Mode)
        {
            case SessionModes.Stage:
            {
                var sp = await db.StageProgresses.SingleOrDefaultAsync(p => p.ProfileId == session.ProfileId && p.StageId == session.StageId, ct);
                if (sp is null)
                {
                    sp = new StageProgress { ProfileId = session.ProfileId, StageId = session.StageId!.Value };
                    db.StageProgresses.Add(sp);
                }
                sp.BestStars = Math.Max(sp.BestStars, session.Stars.Value);
                sp.TimesCompleted++;
                sp.LastCompletedAt = now;
                break;
            }
            case SessionModes.Guardian when Guardian.Passed(session.CorrectCount, session.ProblemCount):
            {
                var current = await db.Levels.SingleAsync(l => l.Id == session.LevelId, ct);
                var next = await db.Levels.SingleAsync(l => l.SubjectId == session.SubjectId && l.Order == current.Order + 1, ct);
                await PromoteAsync(session, subjectProgress, next, now, ct);
                break;
            }
            case SessionModes.Placement:
            {
                var maxLevel = await progress.MaxContentLevelOrderAsync(session.SubjectId, ct);
                var placed = Placement.LevelFor(session.PlacementEstimate ?? Rating.Initial, maxLevel);
                var level = await db.Levels.SingleAsync(l => l.SubjectId == session.SubjectId && l.Slug == placed.Slug, ct);
                subjectProgress.PlacementDone = true;
                await PromoteAsync(session, subjectProgress, level, now, ct);
                break;
            }
        }

        // As medalhas olham o banco: salva o fim da sessão antes de avaliar.
        await db.SaveChangesAsync(ct);
        await progress.AwardAchievementsAsync(session.ProfileId, now, ct);
    }

    /// <summary>
    /// Leva o jogador ao nível indicado (nunca rebaixa). As habilidades dos níveis anteriores
    /// passam a contar como dominadas, e as do novo nível começam no início da faixa dele.
    /// </summary>
    private async Task PromoteAsync(GameSession session, SubjectProgress sp, Level target, DateTimeOffset now, CancellationToken ct)
    {
        var current = await db.Levels.SingleAsync(l => l.Id == sp.CurrentLevelId, ct);
        if (target.Order <= current.Order)
        {
            session.ResultLevelId = current.Id;
            return;
        }

        sp.CurrentLevelId = target.Id;
        sp.LevelReachedAt = now;
        session.ResultLevelId = target.Id;

        var lowerSkills = await db.Skills.Where(s => s.SubjectId == session.SubjectId && s.Level!.Order < target.Order)
            .Select(s => new { s.Id, LevelSlug = s.Level!.Slug }).ToListAsync(ct);
        var ids = lowerSkills.Select(s => s.Id).ToList();
        var masteries = await db.SkillMasteries.Where(m => m.ProfileId == session.ProfileId && ids.Contains(m.SkillId)).ToDictionaryAsync(m => m.SkillId, ct);
        foreach (var skill in lowerSkills)
        {
            var mastered = LevelProgress.TargetRating(LevelProgress.BySlug(skill.LevelSlug));
            if (!masteries.TryGetValue(skill.Id, out var m))
            {
                m = new SkillMastery { ProfileId = session.ProfileId, SkillId = skill.Id, Rating = mastered };
                db.SkillMasteries.Add(m);
            }
            m.Rating = Math.Max(m.Rating, mastered);
        }
    }

    private async Task<SubjectProgress> GetOrCreateSubjectProgressAsync(Guid profileId, int subjectId, CancellationToken ct)
    {
        var sp = db.SubjectProgresses.Local.SingleOrDefault(p => p.ProfileId == profileId && p.SubjectId == subjectId)
            ?? await db.SubjectProgresses.SingleOrDefaultAsync(p => p.ProfileId == profileId && p.SubjectId == subjectId, ct);
        if (sp is null)
        {
            var firstLevel = await db.Levels.Where(l => l.SubjectId == subjectId).OrderBy(l => l.Order).FirstAsync(ct);
            sp = new SubjectProgress { ProfileId = profileId, SubjectId = subjectId, CurrentLevelId = firstLevel.Id, LevelReachedAt = clock.Now };
            db.SubjectProgresses.Add(sp);
        }
        return sp;
    }

    private async Task AddDailyActivityAsync(Guid profileId, bool correct, int xp, CancellationToken ct)
    {
        var today = clock.Today;
        var day = await db.DailyActivities.SingleOrDefaultAsync(d => d.ProfileId == profileId && d.Date == today, ct);
        if (day is null)
        {
            day = new DailyActivity { ProfileId = profileId, Date = today };
            db.DailyActivities.Add(day);
        }
        day.ProblemsSolved++;
        if (correct) day.Correct++;
        day.Xp += xp;
    }

    // ---------- Resumo ----------

    public async Task<SessionSummary> SummaryAsync(GameSession session, CancellationToken ct)
    {
        var subject = await db.Subjects.SingleAsync(s => s.Id == session.SubjectId, ct);
        var bestStars = await BestStarsAsync(session.ProfileId, ct);
        var answered = await db.SessionProblems.CountAsync(p => p.SessionId == session.Id && p.Attempt != null, ct);
        var subjectProgress = await progress.ForSubjectAsync(session.ProfileId, subject, ct);

        StageRef? stageRef = null, nextRef = null;
        var nextUnlocked = false;
        if (session.StageId is { } stageId)
        {
            var ordered = await StageOrder.ForSubjectAsync(db, subject.Id, ct);
            var index = ordered.FindIndex(s => s.Id == stageId);
            stageRef = new StageRef(stageId, ordered[index].Name);
            if (index + 1 < ordered.Count)
            {
                nextRef = new StageRef(ordered[index + 1].Id, ordered[index + 1].Name);
                nextUnlocked = StageOrder.IsUnlocked(ordered, index + 1, bestStars, subjectProgress.LevelOrder);
            }
        }

        LevelDto? resultLevel = null;
        if (session.ResultLevelId is { } levelId)
        {
            var l = await db.Levels.SingleAsync(x => x.Id == levelId, ct);
            resultLevel = new LevelDto(l.Slug, l.Name, l.Icon);
        }

        var earnedSlugs = await db.ProfileAchievements
            .Where(a => a.ProfileId == session.ProfileId && a.EarnedAt >= session.StartedAt && (session.FinishedAt == null || a.EarnedAt <= session.FinishedAt))
            .Select(a => a.Slug).ToListAsync(ct);
        var achievements = (await progress.AchievementsAsync(session.ProfileId, ct)).Where(a => earnedSlugs.Contains(a.Slug)).ToList();

        // Resultado de cada problema, em ordem, para o "compartilhar" (sem revelar enunciados nem respostas):
        // "certo", "dica" (certo com dica), "erro" ou "pendente".
        var marks = (await LoadProblemsAsync(session.Id, ct)).Select(p => p.Attempt switch
        {
            null => "pendente",
            { IsCorrect: false } => "erro",
            { HintsUsed: > 0 } => "dica",
            _ => "certo",
        }).ToList();

        return new SessionSummary(
            session.Id,
            session.Mode,
            subject.Slug,
            subject.Name,
            stageRef,
            session.IsFinished,
            session.CorrectCount,
            answered,
            session.ProblemCount,
            session.Stars ?? 0,
            session.StageId is { } sid ? bestStars.GetValueOrDefault(sid) : 0,
            session.XpEarned,
            nextRef,
            nextUnlocked,
            session.Mode == SessionModes.Guardian && session.IsFinished ? Guardian.Passed(session.CorrectCount, session.ProblemCount) : null,
            resultLevel,
            achievements,
            subjectProgress,
            marks,
            session.DailyDate);
    }

    // ---------- Auxiliares ----------

    private Task<bool> IsAssignedAsync(Guid profileId, int stageId, CancellationToken ct) =>
        db.ClassroomAssignments.AnyAsync(a => a.StageId == stageId
            && db.ClassroomMembers.Any(m => m.ClassroomId == a.ClassroomId && m.ProfileId == profileId), ct);

    private Task<Dictionary<int, int>> BestStarsAsync(Guid profileId, CancellationToken ct) =>
        db.StageProgresses.Where(p => p.ProfileId == profileId).ToDictionaryAsync(p => p.StageId, p => p.BestStars, ct);

    private Task<List<SessionProblem>> LoadProblemsAsync(Guid sessionId, CancellationToken ct) =>
        db.SessionProblems.Include(p => p.Attempt).Where(p => p.SessionId == sessionId).OrderBy(p => p.Order).ToListAsync(ct);

    private async Task<SessionProblem> FindProblemAsync(GameSession session, Guid problemId, CancellationToken ct) =>
        await db.SessionProblems.Include(p => p.Attempt).SingleOrDefaultAsync(p => p.Id == problemId && p.SessionId == session.Id, ct)
        ?? throw new SessionException(SessionError.NotServed, "Problema não encontrado nesta sessão.");

    private async Task<ProblemDto> ToDtoAsync(SessionProblem p, GameSession session, CancellationToken ct)
    {
        var skill = await db.Skills.Where(s => s.Id == p.SkillId).Select(s => new SkillRef(s.Slug, s.Name)).SingleAsync(ct);
        var c = p.Content;
        var hintsTotal = session.Mode == SessionModes.Placement ? 0 : c.Hints.Count;
        return new ProblemDto(
            p.Id, p.Order + 1, session.ProblemCount, session.Mode, c.Type, c.Prompt, c.Visual, c.Choices, c.Sequence, c.Code,
            c.Grid, c.Figure, c.AcceptsNegative, c.AcceptsDecimal,
            hintsTotal, [.. c.Hints.Take(p.HintsRevealed)], skill);
    }
}
