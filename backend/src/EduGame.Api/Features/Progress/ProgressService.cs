using EduGame.Api.Common;
using EduGame.Domain.Content;
using EduGame.Domain.Players;
using EduGame.Domain.Progression;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Progress;

public sealed record LevelDto(string Slug, string Name, string Icon);

/// <summary>Habilidade da BNCC ligada a uma habilidade do jogo.</summary>
public sealed record BnccRefDto(string Code, string Grade, string Component, string Summary)
{
    private static readonly Dictionary<string, EduGame.Infrastructure.Seeding.BnccCatalog.Habilidade> All =
        EduGame.Infrastructure.Seeding.BnccCatalog.Habilidades.ToDictionary(h => h.Code);

    /// <summary>Monta a cada chamada: os textos saem no idioma da requisição.</summary>
    public static IReadOnlyList<BnccRefDto> For(IEnumerable<string> codes) =>
        [.. codes.Select(c => All.GetValueOrDefault(c)).OfType<EduGame.Infrastructure.Seeding.BnccCatalog.Habilidade>()
            .Select(h => new BnccRefDto(h.Code, h.Grade, h.Component, h.Summary))];
}

public sealed record SkillProgressDto(
    string Slug,
    string Name,
    string Description,
    int? Rating,
    int StartRating,
    int TargetRating,
    SkillStatus Status,
    int Attempts,
    int Correct,
    IReadOnlyList<BnccRefDto> Bncc);

public sealed record SubjectProgressDto(
    string Slug,
    string Name,
    string Icon,
    LevelDto Level,
    int LevelOrder,
    LevelDto? NextLevel,
    int Percent,
    int GuardianAt,
    bool ReadyForNextLevel,
    bool GuardianAvailable,
    bool PlacementAvailable,
    string? SuggestedLevel,
    bool SuggestEasierLevel,
    bool ReviewAvailable,
    int NeedsReviewSkills,
    int MasteredSkills,
    int TotalSkills,
    int Xp,
    IReadOnlyList<SkillProgressDto> Skills);

public sealed record HistoryDay(DateOnly Date, int Problems, int Correct, int Xp);
public sealed record HistoryResponse(int Streak, int BestStreak, IReadOnlyList<HistoryDay> Days);
public sealed record AchievementDto(string Slug, string Name, string Description, string Icon, DateTimeOffset? EarnedAt);

public sealed class ProgressService(AppDbContext db, GameClock clock)
{
    /// <summary>Nível atual do jogador no tópico (Iniciante se ainda não jogou).</summary>
    public async Task<Level> CurrentLevelAsync(Guid profileId, int subjectId, CancellationToken ct)
    {
        var progress = await db.SubjectProgresses.AsNoTracking().SingleOrDefaultAsync(p => p.ProfileId == profileId && p.SubjectId == subjectId, ct);
        var levels = await db.Levels.Where(l => l.SubjectId == subjectId).OrderBy(l => l.Order).ToListAsync(ct);
        return levels.SingleOrDefault(l => l.Id == progress?.CurrentLevelId) ?? levels[0];
    }

    /// <summary>Maior nível do tópico que já tem habilidades (conteúdo jogável).</summary>
    public async Task<int> MaxContentLevelOrderAsync(int subjectId, CancellationToken ct) =>
        await db.Skills.Where(s => s.SubjectId == subjectId && s.Templates.Any(t => t.Active)).MaxAsync(s => (int?)s.Level!.Order, ct) ?? 1;

    public async Task<SubjectProgressDto> ForSubjectAsync(Guid profileId, Subject subject, CancellationToken ct)
    {
        var progress = await db.SubjectProgresses.AsNoTracking().SingleOrDefaultAsync(p => p.ProfileId == profileId && p.SubjectId == subject.Id, ct);
        var level = await CurrentLevelAsync(profileId, subject.Id, ct);
        var levelDef = LevelProgress.BySlug(level.Slug);

        var skills = await db.Skills.Where(s => s.SubjectId == subject.Id && s.LevelId == level.Id).OrderBy(s => s.Order).ToListAsync(ct);
        var skillIds = skills.Select(s => s.Id).ToList();
        var masteries = await db.SkillMasteries
            .Where(m => m.ProfileId == profileId && skillIds.Contains(m.SkillId))
            .ToDictionaryAsync(m => m.SkillId, ct);

        var now = clock.Now;
        var start = Rating.InitialFor(levelDef);
        var target = LevelProgress.TargetRating(levelDef);
        var skillDtos = skills.Select(s =>
        {
            masteries.TryGetValue(s.Id, out var m);
            return new SkillProgressDto(
                s.Slug, s.Name, s.Description,
                m?.Rating, start, target,
                LevelProgress.StatusOf(m?.Rating, m?.AttemptsCount ?? 0, m?.LastPracticedAt, levelDef, now),
                m?.AttemptsCount ?? 0,
                m?.CorrectCount ?? 0,
                BnccRefDto.For(s.BnccCodes));
        }).ToList();

        var result = LevelProgress.Compute(levelDef, [.. skillDtos.Select(s => s.Rating)]);
        var maxContent = await MaxContentLevelOrderAsync(subject.Id, ct);
        var next = result.Next is { } n ? new LevelDto(n.Slug, n.Name, n.Icon) : null;
        var hasAttempts = await db.SkillMasteries.AnyAsync(m => m.ProfileId == profileId && m.AttemptsCount > 0 && db.Skills.Any(s => s.Id == m.SkillId && s.SubjectId == subject.Id), ct);
        var placementAvailable = !(progress?.PlacementDone ?? false) && !hasAttempts;

        // Quem pode escolher o nível vê uma sugestão pela série escolar (o mesmo ponto de partida do nivelamento).
        string? suggested = null;
        if (placementAvailable)
        {
            var grade = await db.Profiles.Where(p => p.Id == profileId).Select(p => p.SchoolGrade).SingleOrDefaultAsync(ct);
            suggested = Placement.LevelFor(Placement.StartRating(grade), maxContent).Slug;
        }

        return new SubjectProgressDto(
            subject.Slug, subject.Name, subject.Icon,
            new LevelDto(level.Slug, level.Name, level.Icon),
            level.Order,
            next,
            result.Percent,
            LevelProgress.GuardianThreshold,
            result.ReadyForNextLevel,
            GuardianAvailable: result.ReadyForNextLevel && result.Next is { } nx && nx.Order <= maxContent,
            PlacementAvailable: placementAvailable,
            SuggestedLevel: suggested,
            SuggestEasierLevel: LevelProgress.TooHard(levelDef, skillDtos.Sum(s => s.Attempts), skillDtos.Sum(s => s.Correct)),
            ReviewAvailable: hasAttempts,
            NeedsReviewSkills: skillDtos.Count(s => s.Status == SkillStatus.NeedsReview),
            result.MasteredSkills,
            result.TotalSkills,
            progress?.Xp ?? 0,
            skillDtos);
    }

    public async Task<HistoryResponse> HistoryAsync(Guid profileId, int days, CancellationToken ct)
    {
        var today = clock.Today;
        var all = await db.DailyActivities.Where(d => d.ProfileId == profileId).OrderBy(d => d.Date).ToListAsync(ct);
        var from = today.AddDays(-(days - 1));
        var byDate = all.Where(d => d.Date >= from).ToDictionary(d => d.Date);
        var series = Enumerable.Range(0, days)
            .Select(i => from.AddDays(i))
            .Select(d => byDate.TryGetValue(d, out var a) ? new HistoryDay(d, a.ProblemsSolved, a.Correct, a.Xp) : new HistoryDay(d, 0, 0, 0))
            .ToList();
        var active = all.Where(d => d.ProblemsSolved > 0).Select(d => d.Date).ToList();
        return new HistoryResponse(Streaks.Current(active, today), Streaks.Best(active), series);
    }

    public async Task<List<AchievementDto>> AchievementsAsync(Guid profileId, CancellationToken ct)
    {
        var earned = await db.ProfileAchievements.Where(a => a.ProfileId == profileId).ToDictionaryAsync(a => a.Slug, a => a.EarnedAt, ct);
        return [.. Achievements.All.Select(a => new AchievementDto(a.Slug, a.Name, a.Description, a.Icon, earned.TryGetValue(a.Slug, out var at) ? at : null))];
    }

    /// <summary>Recalcula as estatísticas do jogador e grava as medalhas novas. Devolve os slugs conquistados agora.</summary>
    public async Task<List<string>> AwardAchievementsAsync(Guid profileId, DateTimeOffset at, CancellationToken ct)
    {
        var masteries = await db.SkillMasteries.Where(m => m.ProfileId == profileId)
            .Join(db.Skills, m => m.SkillId, s => s.Id, (m, s) => new { m.Rating, m.AttemptsCount, m.CorrectCount, s.SubjectId, LevelOrder = s.Level!.Order })
            .ToListAsync(ct);
        var finished = db.GameSessions.Where(s => s.ProfileId == profileId && s.FinishedAt != null);

        var perfectNoHints = await finished
            .Where(s => s.Mode == Domain.Sessions.SessionModes.Stage && s.CorrectCount == s.ProblemCount)
            .CountAsync(s => !db.SessionProblems.Any(p => p.SessionId == s.Id && p.HintsRevealed > 0), ct);
        var active = await db.DailyActivities.Where(d => d.ProfileId == profileId && d.ProblemsSolved > 0).Select(d => d.Date).ToListAsync(ct);
        // Medalha de nível só para nível conquistado (Guardião ou nivelamento): escolher o nível no começo não conta.
        var highestLevel = await db.Levels
            .Where(l => finished.Any(s => s.ResultLevelId == l.Id && (s.Mode == Domain.Sessions.SessionModes.Guardian || s.Mode == Domain.Sessions.SessionModes.Placement)))
            .MaxAsync(l => (int?)l.Order, ct) ?? 1;

        var stats = new PlayerStats(
            ProblemsSolved: masteries.Sum(m => m.AttemptsCount),
            Correct: masteries.Sum(m => m.CorrectCount),
            SessionsCompleted: await finished.CountAsync(s => s.Mode == Domain.Sessions.SessionModes.Stage, ct),
            ThreeStarStages: await db.StageProgresses.CountAsync(p => p.ProfileId == profileId && p.BestStars == 3, ct),
            PerfectSessionsWithoutHints: perfectNoHints,
            Streak: Streaks.Best(active),
            MasteredSkills: masteries.Count(m => m.Rating >= LevelProgress.TargetRating(LevelDefinitions.All.Single(l => l.Order == m.LevelOrder))),
            SubjectsPlayed: masteries.Select(m => m.SubjectId).Distinct().Count(),
            HighestLevelOrder: highestLevel,
            DailyChallenges: await finished.CountAsync(s => s.Mode == Domain.Sessions.SessionModes.Daily, ct),
            ReviewsCompleted: await finished.CountAsync(s => s.Mode == Domain.Sessions.SessionModes.Review, ct));

        var already = await db.ProfileAchievements.Where(a => a.ProfileId == profileId).Select(a => a.Slug).ToListAsync(ct);
        var fresh = Achievements.EarnedBy(stats).Where(a => !already.Contains(a.Slug)).Select(a => a.Slug).ToList();
        foreach (var slug in fresh) db.ProfileAchievements.Add(new ProfileAchievement { ProfileId = profileId, Slug = slug, EarnedAt = at });
        return fresh;
    }
}
