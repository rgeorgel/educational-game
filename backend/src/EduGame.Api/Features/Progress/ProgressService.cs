using EduGame.Domain.Content;
using EduGame.Domain.Progression;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Progress;

public sealed record LevelDto(string Slug, string Name, string Icon);

public sealed record SkillProgressDto(
    string Slug,
    string Name,
    string Description,
    int? Rating,
    SkillStatus Status,
    int Attempts,
    int Correct);

public sealed record SubjectProgressDto(
    string Slug,
    string Name,
    string Icon,
    LevelDto Level,
    LevelDto? NextLevel,
    int Percent,
    bool ReadyForNextLevel,
    int MasteredSkills,
    int TotalSkills,
    int Xp,
    IReadOnlyList<SkillProgressDto> Skills);

public sealed class ProgressService(AppDbContext db, TimeProvider time)
{
    public async Task<SubjectProgressDto> ForSubjectAsync(Guid profileId, Subject subject, CancellationToken ct)
    {
        var levels = await db.Levels.Where(l => l.SubjectId == subject.Id).OrderBy(l => l.Order).ToListAsync(ct);
        var progress = await db.SubjectProgresses.SingleOrDefaultAsync(p => p.ProfileId == profileId && p.SubjectId == subject.Id, ct);
        var level = levels.SingleOrDefault(l => l.Id == progress?.CurrentLevelId) ?? levels[0];
        var levelDef = LevelDefinitions.All.Single(d => d.Slug == level.Slug);

        var skills = await db.Skills.Where(s => s.SubjectId == subject.Id && s.LevelId == level.Id).OrderBy(s => s.Order).ToListAsync(ct);
        var skillIds = skills.Select(s => s.Id).ToList();
        var masteries = await db.SkillMasteries
            .Where(m => m.ProfileId == profileId && skillIds.Contains(m.SkillId))
            .ToDictionaryAsync(m => m.SkillId, ct);

        var now = time.GetUtcNow();
        var skillDtos = skills.Select(s =>
        {
            masteries.TryGetValue(s.Id, out var m);
            return new SkillProgressDto(
                s.Slug, s.Name, s.Description,
                m?.Rating,
                LevelProgress.StatusOf(m?.Rating, m?.AttemptsCount ?? 0, m?.LastPracticedAt, levelDef, now),
                m?.AttemptsCount ?? 0,
                m?.CorrectCount ?? 0);
        }).ToList();

        var result = LevelProgress.Compute(levelDef, [.. skillDtos.Select(s => s.Rating)]);
        var next = result.Next is { } n ? new LevelDto(n.Slug, n.Name, n.Icon) : null;

        return new SubjectProgressDto(
            subject.Slug, subject.Name, subject.Icon,
            new LevelDto(level.Slug, level.Name, level.Icon),
            next,
            result.Percent,
            result.ReadyForNextLevel,
            result.MasteredSkills,
            result.TotalSkills,
            progress?.Xp ?? 0,
            skillDtos);
    }
}
