using EduGame.Api.Common;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Progress;

public static class ProgressEndpoints
{
    public sealed record TotalsDto(int Xp, int ProblemsSolved, int Correct, int Stars, int Streak, int Achievements);
    public sealed record ProgressResponse(string Nickname, string Avatar, TotalsDto Totals, IReadOnlyList<SubjectProgressDto> Subjects);

    public static void MapProgress(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/profiles/{profileId:guid}").WithTags("Progress").RequireAuthorization();

        group.MapGet("/progress", async (Guid profileId, HttpContext http, AppDbContext db, ProgressService progress, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");

            var subjects = await db.Subjects.OrderBy(s => s.Order).ToListAsync(ct);
            var list = new List<SubjectProgressDto>();
            foreach (var s in subjects) list.Add(await progress.ForSubjectAsync(profileId, s, ct));

            var masteries = db.SkillMasteries.Where(m => m.ProfileId == profileId);
            var history = await progress.HistoryAsync(profileId, 1, ct);
            var totals = new TotalsDto(
                Xp: await db.SubjectProgresses.Where(p => p.ProfileId == profileId).SumAsync(p => p.Xp, ct),
                ProblemsSolved: await masteries.SumAsync(m => m.AttemptsCount, ct),
                Correct: await masteries.SumAsync(m => m.CorrectCount, ct),
                Stars: await db.StageProgresses.Where(p => p.ProfileId == profileId).SumAsync(p => p.BestStars, ct),
                Streak: history.Streak,
                Achievements: await db.ProfileAchievements.CountAsync(a => a.ProfileId == profileId, ct));

            return Results.Ok(new ProgressResponse(profile.Nickname, profile.Avatar, totals, list));
        });

        group.MapGet("/history", async (Guid profileId, int? days, HttpContext http, AppDbContext db, ProgressService progress, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            return Results.Ok(await progress.HistoryAsync(profileId, Math.Clamp(days ?? 14, 1, 90), ct));
        });

        group.MapGet("/achievements", async (Guid profileId, HttpContext http, AppDbContext db, ProgressService progress, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            return Results.Ok(await progress.AchievementsAsync(profileId, ct));
        });
    }
}
