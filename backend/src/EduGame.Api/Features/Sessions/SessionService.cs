using EduGame.Api.Features.Map;
using EduGame.Api.Features.Progress;
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
    string Type,
    string Prompt,
    string? Visual,
    IReadOnlyList<string>? Choices,
    IReadOnlyList<string?>? Sequence,
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
    string SubjectSlug,
    StageRef Stage,
    bool Finished,
    int Correct,
    int Answered,
    int Total,
    int Stars,
    int BestStars,
    int XpEarned,
    StageRef? NextStage,
    bool NextStageUnlocked,
    SubjectProgressDto Progress);

public enum SessionError { NotFound, Locked, Finished, AlreadyAnswered, NotServed, NoHintsLeft }

public sealed class SessionException(SessionError error, string message) : Exception(message)
{
    public SessionError Error { get; } = error;
}

/// <summary>
/// Regras de uma sessão de jogo: escolhe o próximo problema perto do nível do jogador,
/// revela dicas, valida respostas, atualiza rating/XP e fecha a sessão com estrelas.
/// </summary>
public sealed class SessionService(AppDbContext db, ProgressService progress, TimeProvider time)
{
    private static readonly ProblemFactory Factory = ProblemFactory.Default;

    public async Task<GameSession> StartAsync(PlayerProfile profile, int stageId, CancellationToken ct)
    {
        var stage = await db.Stages.Include(s => s.Region).SingleOrDefaultAsync(s => s.Id == stageId, ct)
            ?? throw new SessionException(SessionError.NotFound, "Etapa não encontrada.");

        var ordered = await StageOrder.ForSubjectAsync(db, stage.Region!.SubjectId, ct);
        var bestStars = await BestStarsAsync(profile.Id, ct);
        var index = ordered.FindIndex(s => s.Id == stage.Id);
        if (!StageOrder.IsUnlocked(ordered, index, bestStars))
            throw new SessionException(SessionError.Locked, "Esta etapa ainda está bloqueada. Conclua a anterior primeiro!");

        var session = new GameSession
        {
            ProfileId = profile.Id,
            StageId = stage.Id,
            ProblemCount = stage.ProblemCount,
            StartedAt = time.GetUtcNow(),
        };
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<NextProblemResponse> NextAsync(GameSession session, CancellationToken ct)
    {
        if (session.IsFinished) return new(true, null);

        var problems = await LoadProblemsAsync(session.Id, ct);
        var pending = problems.LastOrDefault(p => p.Attempt is null);
        if (pending is not null) return new(false, await ToDtoAsync(pending, session, ct));
        if (problems.Count >= session.ProblemCount) return new(true, null);

        var stage = await db.Stages.SingleAsync(s => s.Id == session.StageId, ct);
        var last = problems.LastOrDefault();

        int skillId, target;
        string generatorKey;
        if (last?.Attempt is { IsCorrect: false })
        {
            // Errou: vem um "problema irmão" (mesma habilidade e tipo), um pouco mais fácil.
            skillId = last.SkillId;
            generatorKey = last.GeneratorKey;
            target = Math.Max(Rating.Min, last.Difficulty - 100);
        }
        else
        {
            skillId = ChooseSkill(stage.SkillIds, problems);
            var keys = await db.ProblemTemplates.Where(t => t.SkillId == skillId && t.Active).Select(t => t.GeneratorKey).ToListAsync(ct);
            generatorKey = keys[Random.Shared.Next(keys.Count)];
            var rating = await db.SkillMasteries
                .Where(m => m.ProfileId == session.ProfileId && m.SkillId == skillId)
                .Select(m => (int?)m.Rating)
                .SingleOrDefaultAsync(ct) ?? Rating.Initial;
            target = TargetDifficulty(rating, Random.Shared);
        }

        var seed = Random.Shared.Next();
        var avoid = problems.Select(p => ProblemFactory.Fingerprint(p.Content)).ToHashSet();
        var generated = Factory.Generate(generatorKey, target, seed, avoid);

        var problem = new SessionProblem
        {
            SessionId = session.Id,
            Order = problems.Count,
            SkillId = skillId,
            GeneratorKey = generatorKey,
            Seed = seed,
            Difficulty = generated.Difficulty,
            Content = generated.Content,
            ServedAt = time.GetUtcNow(),
        };
        db.SessionProblems.Add(problem);
        await db.SaveChangesAsync(ct);
        return new(false, await ToDtoAsync(problem, session, ct));
    }

    public async Task<HintResponse> HintAsync(GameSession session, Guid problemId, CancellationToken ct)
    {
        var problem = await FindProblemAsync(session, problemId, ct);
        if (problem.Attempt is not null)
            throw new SessionException(SessionError.AlreadyAnswered, "Este problema já foi respondido.");
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

        var now = time.GetUtcNow();
        var correct = AnswerChecker.IsCorrect(problem.Content, answer);

        // Domínio da habilidade
        var mastery = await db.SkillMasteries.SingleOrDefaultAsync(m => m.ProfileId == session.ProfileId && m.SkillId == problem.SkillId, ct);
        if (mastery is null)
        {
            mastery = new SkillMastery { ProfileId = session.ProfileId, SkillId = problem.SkillId, Rating = Rating.Initial };
            db.SkillMasteries.Add(mastery);
        }
        var ratingBefore = mastery.Rating;
        mastery.Rating = Rating.Update(mastery.Rating, problem.Difficulty, Rating.Outcome(correct, problem.HintsRevealed), mastery.AttemptsCount);
        mastery.AttemptsCount++;
        if (correct) mastery.CorrectCount++;
        mastery.LastPracticedAt = now;

        // XP no tópico
        var xp = Rewards.Xp(correct, problem.Difficulty, problem.HintsRevealed);
        var subjectId = await db.Skills.Where(s => s.Id == problem.SkillId).Select(s => s.SubjectId).SingleAsync(ct);
        var subjectProgress = await db.SubjectProgresses.SingleOrDefaultAsync(p => p.ProfileId == session.ProfileId && p.SubjectId == subjectId, ct);
        if (subjectProgress is null)
        {
            var firstLevel = await db.Levels.Where(l => l.SubjectId == subjectId).OrderBy(l => l.Order).FirstAsync(ct);
            subjectProgress = new SubjectProgress { ProfileId = session.ProfileId, SubjectId = subjectId, CurrentLevelId = firstLevel.Id, LevelReachedAt = now };
            db.SubjectProgresses.Add(subjectProgress);
        }
        subjectProgress.Xp += xp;

        // Adicionado explicitamente: com o Id já preenchido, o EF trataria como registro existente.
        problem.Attempt = new Attempt
        {
            SessionProblemId = problem.Id,
            Answer = answer.Trim()[..Math.Min(answer.Trim().Length, 64)],
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
        if (answered >= session.ProblemCount) await FinishAsync(session, now, ct);

        try
        {
            await db.SaveChangesAsync(ct);
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

    public async Task<SessionSummary> SummaryAsync(GameSession session, CancellationToken ct)
    {
        var stage = await db.Stages.Include(s => s.Region).ThenInclude(r => r!.Subject).SingleAsync(s => s.Id == session.StageId, ct);
        var subject = stage.Region!.Subject!;
        var ordered = await StageOrder.ForSubjectAsync(db, subject.Id, ct);
        var bestStars = await BestStarsAsync(session.ProfileId, ct);
        var index = ordered.FindIndex(s => s.Id == stage.Id);
        var next = index + 1 < ordered.Count ? ordered[index + 1] : null;
        var answered = await db.SessionProblems.CountAsync(p => p.SessionId == session.Id && p.Attempt != null, ct);

        return new SessionSummary(
            session.Id,
            subject.Slug,
            new StageRef(stage.Id, stage.Name),
            session.IsFinished,
            session.CorrectCount,
            answered,
            session.ProblemCount,
            session.Stars ?? 0,
            bestStars.GetValueOrDefault(stage.Id),
            session.XpEarned,
            next is null ? null : new StageRef(next.Id, next.Name),
            next is not null && StageOrder.IsUnlocked(ordered, index + 1, bestStars),
            await progress.ForSubjectAsync(session.ProfileId, subject, ct));
    }

    /// <summary>
    /// Zona de desafio: 70% perto do rating, 20% um pouco abaixo (confiança), 10% um pouco acima (desafio).
    /// </summary>
    public static int TargetDifficulty(int rating, Random rng)
    {
        var roll = rng.NextDouble();
        var offset = roll switch
        {
            < 0.7 => rng.Next(-100, 101),
            < 0.9 => -rng.Next(100, 251),
            _ => rng.Next(100, 251),
        };
        return Math.Clamp(rating + offset, Rating.Min, Rating.Max);
    }

    /// <summary>Equilibra as habilidades da etapa: escolhe a menos sorteada até agora (empates ao acaso).</summary>
    private static int ChooseSkill(IReadOnlyList<int> skillIds, IReadOnlyList<SessionProblem> served)
    {
        var counts = skillIds.ToDictionary(id => id, id => served.Count(p => p.SkillId == id));
        var min = counts.Values.Min();
        var candidates = counts.Where(c => c.Value == min).Select(c => c.Key).ToList();
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    private async Task FinishAsync(GameSession session, DateTimeOffset now, CancellationToken ct)
    {
        session.FinishedAt = now;
        session.Stars = Rewards.Stars(session.CorrectCount, session.ProblemCount);

        var sp = await db.StageProgresses.SingleOrDefaultAsync(p => p.ProfileId == session.ProfileId && p.StageId == session.StageId, ct);
        if (sp is null)
        {
            sp = new StageProgress { ProfileId = session.ProfileId, StageId = session.StageId };
            db.StageProgresses.Add(sp);
        }
        sp.BestStars = Math.Max(sp.BestStars, session.Stars.Value);
        sp.TimesCompleted++;
        sp.LastCompletedAt = now;
    }

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
        return new ProblemDto(
            p.Id, p.Order + 1, session.ProblemCount, c.Type, c.Prompt, c.Visual, c.Choices, c.Sequence,
            c.Hints.Count, [.. c.Hints.Take(p.HintsRevealed)], skill);
    }
}
