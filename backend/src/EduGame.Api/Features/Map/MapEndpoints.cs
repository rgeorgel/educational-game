using EduGame.Api.Common;
using EduGame.Api.Features.Progress;
using EduGame.Domain.Content;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Map;

/// <summary>Ordem das etapas de um tópico e regra de desbloqueio.</summary>
public static class StageOrder
{
    /// <summary>Etapas do tópico na ordem do mapa (nível → região → etapa).</summary>
    public static Task<List<Stage>> ForSubjectAsync(AppDbContext db, int subjectId, CancellationToken ct) =>
        db.Stages
            .Include(s => s.Region).ThenInclude(r => r!.Level)
            .Where(s => s.Region!.SubjectId == subjectId)
            .OrderBy(s => s.Region!.Level!.Order).ThenBy(s => s.Region!.Order).ThenBy(s => s.Order)
            .ToListAsync(ct);

    /// <summary>A primeira etapa está sempre aberta; as demais abrem quando a anterior tem pelo menos 1 estrela.</summary>
    public static bool IsUnlocked(IReadOnlyList<Stage> ordered, int index, IReadOnlyDictionary<int, int> bestStars) =>
        index == 0 || bestStars.GetValueOrDefault(ordered[index - 1].Id) >= 1;
}

public static class MapEndpoints
{
    public sealed record StageDto(int Id, string Slug, string Name, int ProblemCount, IReadOnlyList<string> Skills, int Stars, int TimesCompleted, bool Unlocked);
    public sealed record RegionDto(string Slug, string Name, string Description, string Icon, LevelDto Level, IReadOnlyList<StageDto> Stages);
    public sealed record MapResponse(SubjectProgressDto Progress, IReadOnlyList<RegionDto> Regions);

    public static void MapMap(this RouteGroupBuilder api)
    {
        api.MapGet("/profiles/{profileId:guid}/subjects/{slug}/map", async (
            Guid profileId, string slug, HttpContext http, AppDbContext db, ProgressService progress, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            var subject = await db.Subjects.SingleOrDefaultAsync(s => s.Slug == slug, ct);
            if (subject is null) return ApiResults.NotFound("Tópico não encontrado.");

            var stages = await StageOrder.ForSubjectAsync(db, subject.Id, ct);
            var stageIds = stages.Select(s => s.Id).ToList();
            var stageProgress = await db.StageProgresses
                .Where(p => p.ProfileId == profileId && stageIds.Contains(p.StageId))
                .ToDictionaryAsync(p => p.StageId, ct);
            var bestStars = stageProgress.ToDictionary(p => p.Key, p => p.Value.BestStars);
            var skillNames = await db.Skills.Where(s => s.SubjectId == subject.Id).ToDictionaryAsync(s => s.Id, s => s.Name, ct);

            var regions = stages
                .Select((stage, index) => (stage, index))
                .GroupBy(x => x.stage.Region!)
                .Select(g => new RegionDto(
                    g.Key.Slug, g.Key.Name, g.Key.Description, g.Key.Icon,
                    new LevelDto(g.Key.Level!.Slug, g.Key.Level.Name, g.Key.Level.Icon),
                    [.. g.Select(x => new StageDto(
                        x.stage.Id, x.stage.Slug, x.stage.Name, x.stage.ProblemCount,
                        [.. x.stage.SkillIds.Select(id => skillNames.GetValueOrDefault(id, "?"))],
                        bestStars.GetValueOrDefault(x.stage.Id),
                        stageProgress.GetValueOrDefault(x.stage.Id)?.TimesCompleted ?? 0,
                        StageOrder.IsUnlocked(stages, x.index, bestStars)))]))
                .ToList();

            return Results.Ok(new MapResponse(await progress.ForSubjectAsync(profileId, subject, ct), regions));
        }).RequireAuthorization().WithTags("Map");
    }
}
