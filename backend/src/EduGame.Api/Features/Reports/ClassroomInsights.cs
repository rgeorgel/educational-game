using EduGame.Api.Common;
using EduGame.Domain.Players;
using EduGame.Domain.Progression;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Reports;

public sealed record StageRefDto(int Id, string Name, string RegionName, string LevelName, string SubjectSlug, string SubjectName, string SubjectIcon);

public sealed record AssignmentStudentDto(Guid ProfileId, string Nickname, string Avatar, string Status, int BestStars);

/// <summary>Tarefa vista pelo professor: quem já fez ("feita"), quem tentou sem atingir as estrelas ("tentou") e quem não começou ("pendente").</summary>
public sealed record AssignmentDto(
    Guid Id,
    StageRefDto Stage,
    int MinStars,
    DateOnly? DueDate,
    string? Note,
    DateTimeOffset CreatedAt,
    bool Active,
    int Done,
    int Total,
    IReadOnlyList<AssignmentStudentDto> Students);

/// <summary>Tarefa vista pelo aluno.</summary>
public sealed record StudentAssignmentDto(
    Guid Id,
    string ClassroomName,
    StageRefDto Stage,
    int MinStars,
    DateOnly? DueDate,
    string? Note,
    bool Done,
    int BestStars,
    bool Overdue);

/// <summary>Uma habilidade na turma: quantos praticaram, dominam e pedem revisão, com a BNCC.</summary>
public sealed record SkillClassDto(
    string Slug,
    string Name,
    string SubjectName,
    string SubjectIcon,
    string LevelName,
    IReadOnlyList<string> Bncc,
    int Practiced,
    int Mastered,
    int Practicing,
    int NeedsReview,
    int Attempts,
    int Correct);

/// <summary>Tarefas e relatório por habilidade (BNCC) das turmas.</summary>
public sealed class ClassroomInsights(AppDbContext db, GameClock clock)
{
    public Task<List<StageRefDto>> StageRefsAsync(IEnumerable<int> stageIds, CancellationToken ct)
    {
        var ids = stageIds.Distinct().ToList();
        return db.Stages.AsNoTracking().Where(s => ids.Contains(s.Id))
            .Select(s => new StageRefDto(s.Id, s.Name, s.Region!.Name, s.Region.Level!.Name,
                s.Region.Subject!.Slug, s.Region.Subject.Name, s.Region.Subject.Icon))
            .ToListAsync(ct);
    }

    /// <summary>Melhor resultado de cada (perfil, etapa) depois de uma data: estrelas da melhor sessão concluída.</summary>
    private async Task<Dictionary<(Guid Profile, int Stage), (int Stars, DateTimeOffset At)>> FinishedRunsAsync(
        IReadOnlyCollection<Guid> profileIds, IReadOnlyCollection<int> stageIds, DateTimeOffset since, CancellationToken ct)
    {
        var runs = await db.GameSessions.AsNoTracking()
            .Where(g => profileIds.Contains(g.ProfileId) && g.StageId != null && stageIds.Contains(g.StageId.Value)
                && g.FinishedAt != null && g.FinishedAt >= since)
            .Select(g => new { g.ProfileId, StageId = g.StageId!.Value, Stars = g.Stars ?? 0, At = g.FinishedAt!.Value })
            .ToListAsync(ct);
        return runs.GroupBy(r => (r.ProfileId, r.StageId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Stars).ThenBy(r => r.At).Select(r => (r.Stars, r.At)).First());
    }

    public async Task<List<AssignmentDto>> AssignmentsAsync(Guid classroomId, IReadOnlyList<PlayerProfile> students, CancellationToken ct)
    {
        var assignments = await db.ClassroomAssignments.AsNoTracking().Where(a => a.ClassroomId == classroomId)
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        if (assignments.Count == 0) return [];

        var stages = (await StageRefsAsync(assignments.Select(a => a.StageId), ct)).ToDictionary(s => s.Id);
        var profileIds = students.Select(s => s.Id).ToList();
        var oldest = assignments.Min(a => a.CreatedAt);
        var runs = await FinishedRunsAsync(profileIds, [.. assignments.Select(a => a.StageId)], oldest, ct);
        var today = clock.Today;

        return [.. assignments.Where(a => stages.ContainsKey(a.StageId)).Select(a =>
        {
            var rows = students.Select(p =>
            {
                var best = runs.TryGetValue((p.Id, a.StageId), out var r) && r.At >= a.CreatedAt ? r.Stars : -1;
                var status = best >= a.MinStars ? "feita" : best >= 0 ? "tentou" : "pendente";
                return new AssignmentStudentDto(p.Id, p.Nickname, p.Avatar, status, Math.Max(0, best));
            }).OrderBy(s => s.Status == "feita" ? 2 : s.Status == "tentou" ? 1 : 0).ThenBy(s => s.Nickname).ToList();
            return new AssignmentDto(a.Id, stages[a.StageId], a.MinStars, a.DueDate, a.Note, a.CreatedAt, a.IsActive(today),
                rows.Count(s => s.Status == "feita"), rows.Count, rows);
        })];
    }

    public async Task<List<StudentAssignmentDto>> ForStudentAsync(Guid profileId, CancellationToken ct)
    {
        var today = clock.Today;
        var recent = today.AddDays(-14);
        var rows = await db.ClassroomMembers.AsNoTracking().Where(m => m.ProfileId == profileId)
            .Join(db.Classrooms, m => m.ClassroomId, c => c.Id, (m, c) => c)
            .Join(db.ClassroomAssignments, c => c.Id, a => a.ClassroomId, (c, a) => new { ClassroomName = c.Name, Assignment = a })
            .Where(x => x.Assignment.DueDate == null || x.Assignment.DueDate >= recent)
            .ToListAsync(ct);
        if (rows.Count == 0) return [];

        var stages = (await StageRefsAsync(rows.Select(r => r.Assignment.StageId), ct)).ToDictionary(s => s.Id);
        var runs = await FinishedRunsAsync([profileId], [.. rows.Select(r => r.Assignment.StageId)], rows.Min(r => r.Assignment.CreatedAt), ct);

        return [.. rows.Where(r => stages.ContainsKey(r.Assignment.StageId)).Select(r =>
        {
            var a = r.Assignment;
            var best = runs.TryGetValue((profileId, a.StageId), out var run) && run.At >= a.CreatedAt ? run.Stars : 0;
            var done = best >= a.MinStars;
            return new StudentAssignmentDto(a.Id, r.ClassroomName, stages[a.StageId], a.MinStars, a.DueDate, a.Note, done, best,
                Overdue: !done && a.DueDate is { } due && due < today);
        })
        .OrderBy(a => a.Done).ThenBy(a => a.DueDate ?? DateOnly.MaxValue)];
    }

    /// <summary>Uma linha por habilidade que alguém da turma já praticou.</summary>
    public async Task<List<SkillClassDto>> SkillsAsync(IReadOnlyCollection<Guid> profileIds, CancellationToken ct)
    {
        if (profileIds.Count == 0) return [];
        var now = clock.Now;
        var masteries = await db.SkillMasteries.AsNoTracking()
            .Where(m => profileIds.Contains(m.ProfileId) && m.AttemptsCount > 0).ToListAsync(ct);
        if (masteries.Count == 0) return [];

        var skillIds = masteries.Select(m => m.SkillId).Distinct().ToList();
        var skills = await db.Skills.AsNoTracking().Where(s => skillIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id, s.Slug, s.Name, s.BnccCodes, s.Order,
                SubjectName = s.Subject!.Name, SubjectIcon = s.Subject.Icon, SubjectOrder = s.Subject.Order,
                LevelSlug = s.Level!.Slug, LevelName = s.Level.Name, LevelOrder = s.Level.Order,
            })
            .ToListAsync(ct);

        return [.. skills.OrderBy(s => s.SubjectOrder).ThenBy(s => s.LevelOrder).ThenBy(s => s.Order).Select(s =>
        {
            var level = LevelProgress.BySlug(s.LevelSlug);
            var rows = masteries.Where(m => m.SkillId == s.Id).ToList();
            var statuses = rows.Select(m => LevelProgress.StatusOf(m.Rating, m.AttemptsCount, m.LastPracticedAt, level, now)).ToList();
            return new SkillClassDto(s.Slug, s.Name, s.SubjectName, s.SubjectIcon, s.LevelName, s.BnccCodes,
                rows.Count,
                statuses.Count(x => x == SkillStatus.Mastered),
                statuses.Count(x => x == SkillStatus.Practicing),
                statuses.Count(x => x == SkillStatus.NeedsReview),
                rows.Sum(m => m.AttemptsCount),
                rows.Sum(m => m.CorrectCount));
        })];
    }
}
