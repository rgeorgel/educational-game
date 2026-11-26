using EduGame.Api.Common;
using EduGame.Api.Features.Progress;
using EduGame.Api.Features.Sessions;
using EduGame.Domain.Progression;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EduGame.Problems.Lang;

namespace EduGame.Api.Features.Games;

/// <summary>
/// Menu de jogos: cada habilidade do currículo vira um jogo que o jogador escolhe e joga quando quiser
/// (ex.: "hoje só quero Sudoku"), fora da trilha do mapa.
/// </summary>
public static class GameEndpoints
{
    public sealed record GameSubjectDto(string Slug, string Name, string Icon, LevelDto CurrentLevel);

    /// <param name="Percent">Quanto o jogador já andou rumo ao domínio deste jogo (0 a 100).</param>
    /// <param name="AboveLevel">O jogo é de um nível acima do nível atual do jogador no tópico (vai ser puxado).</param>
    public sealed record GameDto(
        string Slug,
        string Name,
        string Description,
        string SubjectSlug,
        LevelDto Level,
        int LevelOrder,
        bool AboveLevel,
        bool CurrentLevel,
        SkillStatus Status,
        int Percent,
        int Attempts,
        int Correct,
        DateTimeOffset? LastPlayedAt);

    public sealed record GamesResponse(IReadOnlyList<GameSubjectDto> Subjects, IReadOnlyList<GameDto> Games, int ProblemsPerGame);

    public static void MapGames(this RouteGroupBuilder api)
    {
        api.MapGet("/profiles/{profileId:guid}/games", async (Guid profileId, HttpContext http, AppDbContext db, ProgressService progress, GameClock clock, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));

            var subjects = await db.Subjects.AsNoTracking().OrderBy(s => s.Order).ToListAsync(ct);
            var currentLevels = new Dictionary<int, (string Slug, string Name, string Icon, int Order)>();
            foreach (var subject in subjects)
            {
                var level = await progress.CurrentLevelAsync(profileId, subject.Id, ct);
                currentLevels[subject.Id] = (level.Slug, level.Name, level.Icon, level.Order);
            }

            var skills = await db.Skills.AsNoTracking()
                .Where(s => s.Templates.Any(t => t.Active))
                .Select(s => new { s.Id, s.Slug, s.Name, s.Description, s.SubjectId, s.Order, LevelSlug = s.Level!.Slug, LevelName = s.Level.Name, LevelIcon = s.Level.Icon, LevelOrder = s.Level.Order })
                .ToListAsync(ct);
            var masteries = await db.SkillMasteries.AsNoTracking().Where(m => m.ProfileId == profileId).ToDictionaryAsync(m => m.SkillId, ct);

            var now = clock.Now;
            var subjectOrder = subjects.ToDictionary(s => s.Id, s => s.Order);
            var subjectSlug = subjects.ToDictionary(s => s.Id, s => s.Slug);
            var games = skills
                .OrderBy(s => subjectOrder[s.SubjectId]).ThenBy(s => s.LevelOrder).ThenBy(s => s.Order)
                .Select(s =>
                {
                    masteries.TryGetValue(s.Id, out var m);
                    var def = LevelProgress.BySlug(s.LevelSlug);
                    // Domínio "dado" pelo nivelamento (sem respostas) não conta como jogado.
                    var played = m is { AttemptsCount: > 0 };
                    var current = currentLevels[s.SubjectId].Order;
                    return new GameDto(
                        s.Slug, s.Name, s.Description, subjectSlug[s.SubjectId],
                        new LevelDto(s.LevelSlug, s.LevelName, s.LevelIcon), s.LevelOrder,
                        AboveLevel: s.LevelOrder > current,
                        CurrentLevel: s.LevelOrder == current,
                        LevelProgress.StatusOf(m?.Rating, m?.AttemptsCount ?? 0, m?.LastPracticedAt, def, now),
                        Percent: (int)Math.Floor(LevelProgress.SkillProgress(def, m?.Rating) * 100),
                        m?.AttemptsCount ?? 0,
                        m?.CorrectCount ?? 0,
                        played ? m!.LastPracticedAt : null);
                })
                .ToList();

            var subjectDtos = subjects.Select(s =>
            {
                var l = currentLevels[s.Id];
                return new GameSubjectDto(s.Slug, s.Name, s.Icon, new LevelDto(l.Slug, l.Name, l.Icon));
            }).ToList();

            return Results.Ok(new GamesResponse(subjectDtos, games, SessionService.FreePlayProblemCount));
        }).RequireAuthorization().WithTags("Games");
    }
}
