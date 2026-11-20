using EduGame.Api.Common;
using EduGame.Api.Features.Profiles;
using EduGame.Api.Features.Progress;
using EduGame.Domain.Progression;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Reports;

public sealed record WeekDto(int Problems, int Correct, int ActiveDays);

public sealed record SubjectReport(
    string Slug,
    string Name,
    string Icon,
    LevelDto Level,
    int Percent,
    int Xp,
    int MasteredSkills,
    int TotalSkills,
    IReadOnlyList<string> NeedsAttention,
    IReadOnlyList<AttentionSkill> AttentionSkills);

/// <summary>Habilidade que pede atenção, com os códigos da BNCC (para o professor planejar a aula).</summary>
public sealed record AttentionSkill(string Name, IReadOnlyList<string> Bncc);

public sealed record ProfileReport(
    Guid Id,
    string Nickname,
    string Avatar,
    string AccessoryIcon,
    string Color,
    DateOnly? LastActive,
    int Streak,
    WeekDto LastSevenDays,
    int TotalProblems,
    int Accuracy,
    IReadOnlyList<SubjectReport> Subjects);

/// <summary>Relatórios para o responsável e para o professor (só dados de progresso, nada pessoal).</summary>
public sealed class ReportService(AppDbContext db, ProgressService progress, GameClock clock)
{
    /// <summary>Uma habilidade pede atenção quando pede revisão ou quando o acerto está baixo com tentativas suficientes.</summary>
    public static bool NeedsAttention(SkillProgressDto s) =>
        s.Status == SkillStatus.NeedsReview || (s.Attempts >= 5 && (double)s.Correct / s.Attempts < 0.6);

    public async Task<List<ProfileReport>> BuildAsync(IReadOnlyCollection<Guid> profileIds, CancellationToken ct)
    {
        var profiles = await db.Profiles.Where(p => profileIds.Contains(p.Id)).OrderBy(p => p.Nickname).ToListAsync(ct);
        var subjects = await db.Subjects.OrderBy(s => s.Order).ToListAsync(ct);
        var today = clock.Today;
        var weekStart = today.AddDays(-6);

        var reports = new List<ProfileReport>();
        foreach (var profile in profiles)
        {
            var days = await db.DailyActivities.Where(d => d.ProfileId == profile.Id && d.ProblemsSolved > 0).ToListAsync(ct);
            var week = days.Where(d => d.Date >= weekStart).ToList();
            var masteries = await db.SkillMasteries.Where(m => m.ProfileId == profile.Id).ToListAsync(ct);
            var attempts = masteries.Sum(m => m.AttemptsCount);
            var correct = masteries.Sum(m => m.CorrectCount);

            var subjectReports = new List<SubjectReport>();
            foreach (var subject in subjects)
            {
                var s = await progress.ForSubjectAsync(profile.Id, subject, ct);
                var attention = s.Skills.Where(NeedsAttention).ToList();
                subjectReports.Add(new SubjectReport(s.Slug, s.Name, s.Icon, s.Level, s.Percent, s.Xp, s.MasteredSkills, s.TotalSkills,
                    [.. attention.Select(k => k.Name)],
                    [.. attention.Select(k => new AttentionSkill(k.Name, [.. k.Bncc.Select(b => b.Code)]))]));
            }

            var dto = ProfileEndpoints.ToDto(profile);
            reports.Add(new ProfileReport(
                profile.Id, profile.Nickname, profile.Avatar, dto.AccessoryIcon, profile.Color,
                days.Count == 0 ? null : days.Max(d => d.Date),
                Streaks.Current(days.Select(d => d.Date), today),
                new WeekDto(week.Sum(d => d.ProblemsSolved), week.Sum(d => d.Correct), week.Count),
                attempts,
                attempts == 0 ? 0 : (int)Math.Round(100.0 * correct / attempts),
                subjectReports));
        }
        return reports;
    }
}
