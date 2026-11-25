using EduGame.Api.Common;
using EduGame.Api.Features.Progress;
using EduGame.Domain.Content;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EduGame.Problems.Lang;

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

    /// <summary>
    /// Uma etapa abre quando o nível dela já foi alcançado e a etapa anterior do mesmo nível tem pelo menos 1 estrela.
    /// A primeira etapa de cada nível alcançado está sempre aberta.
    /// </summary>
    public static bool IsUnlocked(IReadOnlyList<Stage> ordered, int index, IReadOnlyDictionary<int, int> bestStars, int currentLevelOrder)
    {
        var levelOrder = ordered[index].Region!.Level!.Order;
        if (levelOrder > currentLevelOrder) return false;
        if (index == 0 || ordered[index - 1].Region!.Level!.Order != levelOrder) return true;
        return bestStars.GetValueOrDefault(ordered[index - 1].Id) >= 1;
    }
}

public static class MapEndpoints
{
    public sealed record StageDto(int Id, string Slug, string Name, int ProblemCount, IReadOnlyList<string> Skills, int Stars, int TimesCompleted, bool Unlocked);
    public sealed record RegionDto(string Slug, string Name, string Description, string Icon, IReadOnlyList<StageDto> Stages);
    public sealed record LevelSectionDto(LevelDto Level, bool Unlocked, bool Current, IReadOnlyList<RegionDto> Regions);
    public sealed record MapResponse(SubjectProgressDto Progress, IReadOnlyList<LevelSectionDto> Levels);

    public static void MapMap(this RouteGroupBuilder api)
    {
        api.MapGet("/profiles/{profileId:guid}/subjects/{slug}/map", async (
            Guid profileId, string slug, HttpContext http, AppDbContext db, ProgressService progress, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));
            var subject = await db.Subjects.SingleOrDefaultAsync(s => s.Slug == slug, ct);
            if (subject is null) return ApiResults.NotFound(Tr("Tópico não encontrado.", "Subject not found.", "Tema no encontrado."));

            var subjectProgress = await progress.ForSubjectAsync(profileId, subject, ct);
            var currentOrder = subjectProgress.LevelOrder;

            var stages = await StageOrder.ForSubjectAsync(db, subject.Id, ct);
            var stageIds = stages.Select(s => s.Id).ToList();
            var stageProgress = await db.StageProgresses
                .Where(p => p.ProfileId == profileId && stageIds.Contains(p.StageId))
                .ToDictionaryAsync(p => p.StageId, ct);
            var bestStars = stageProgress.ToDictionary(p => p.Key, p => p.Value.BestStars);
            var skillNames = await db.Skills.Where(s => s.SubjectId == subject.Id).ToDictionaryAsync(s => s.Id, s => s.Name, ct);

            var levels = stages
                .Select((stage, index) => (stage, index))
                .GroupBy(x => x.stage.Region!.Level!)
                .Select(lg => new LevelSectionDto(
                    new LevelDto(lg.Key.Slug, lg.Key.Name, lg.Key.Icon),
                    lg.Key.Order <= currentOrder,
                    lg.Key.Order == currentOrder,
                    [.. lg.GroupBy(x => x.stage.Region!).Select(rg => new RegionDto(
                        rg.Key.Slug, rg.Key.Name, rg.Key.Description, rg.Key.Icon,
                        [.. rg.Select(x => new StageDto(
                            x.stage.Id, x.stage.Slug, x.stage.Name, x.stage.ProblemCount,
                            [.. x.stage.SkillIds.Select(id => skillNames.GetValueOrDefault(id, "?"))],
                            bestStars.GetValueOrDefault(x.stage.Id),
                            stageProgress.GetValueOrDefault(x.stage.Id)?.TimesCompleted ?? 0,
                            StageOrder.IsUnlocked(stages, x.index, bestStars, currentOrder)))]))]))
                .ToList();

            return Results.Ok(new MapResponse(subjectProgress, levels));
        }).RequireAuthorization().WithTags("Map");
    }
}
