using EduGame.Api.Common;
using EduGame.Api.Features.Progress;
using EduGame.Domain.Content;
using EduGame.Domain.Players;
using EduGame.Domain.Sessions;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EduGame.Problems.Lang;

namespace EduGame.Api.Features.Sessions;

public static class SessionEndpoints
{
    public sealed record StartRequest(int StageId);
    public sealed record StartResponse(Guid SessionId, int Total, string Mode);
    public sealed record AnswerRequest(string Answer);
    public sealed record ChangeLevelRequest(string? Level);

    public static void MapSessions(this RouteGroupBuilder api)
    {
        api.MapPost("/profiles/{profileId:guid}/sessions", async (
            Guid profileId, StartRequest req, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));
            return await Handle(async () => Created(await sessions.StartStageAsync(profile, req.StageId, ct)));
        }).RequireAuthorization().WithTags("Sessions");

        var subjectGroup = api.MapGroup("/profiles/{profileId:guid}/subjects/{slug}").WithTags("Sessions").RequireAuthorization();

        subjectGroup.MapPost("/guardian", (Guid profileId, string slug, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithProfileAndSubject(db, http, profileId, slug, ct, async (profile, subject) => Created(await sessions.StartGuardianAsync(profile, subject, ct))));

        subjectGroup.MapPost("/placement", (Guid profileId, string slug, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithProfileAndSubject(db, http, profileId, slug, ct, async (profile, subject) => Created(await sessions.StartPlacementAsync(profile, subject, ct))));

        subjectGroup.MapPost("/daily", (Guid profileId, string slug, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithProfileAndSubject(db, http, profileId, slug, ct, async (profile, subject) => Created(await sessions.StartDailyAsync(profile, subject, ct))));

        subjectGroup.MapPost("/review", (Guid profileId, string slug, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithProfileAndSubject(db, http, profileId, slug, ct, async (profile, subject) => Created(await sessions.StartReviewAsync(profile, subject, ct))));

        // Jogo livre: um jogo (habilidade) escolhido no menu de jogos.
        subjectGroup.MapPost("/games/{skill}", (Guid profileId, string slug, string skill, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithProfileAndSubject(db, http, profileId, slug, ct, async (profile, subject) => Created(await sessions.StartFreePlayAsync(profile, subject, skill, ct))));

        subjectGroup.MapPost("/level", (Guid profileId, string slug, ChangeLevelRequest req, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Level))
                return Task.FromResult(ApiResults.Validation("level", Tr("Escolha um nível.", "Choose a level.", "Elige un nivel.")));
            return WithProfileAndSubject(db, http, profileId, slug, ct, async (profile, subject) =>
            {
                var level = await sessions.ChangeLevelAsync(profile, subject, req.Level, ct);
                return Results.Ok(new LevelDto(level.Slug, level.Name, level.Icon));
            });
        });

        subjectGroup.MapPost("/placement/skip", (Guid profileId, string slug, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithProfileAndSubject(db, http, profileId, slug, ct, async (profile, subject) =>
            {
                await sessions.SkipPlacementAsync(profile, subject, ct);
                return Results.NoContent();
            }));

        var group = api.MapGroup("/sessions/{sessionId:guid}").WithTags("Sessions").RequireAuthorization();

        group.MapGet("/next", (Guid sessionId, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithSession(db, http, sessionId, ct, async s => Results.Ok(await sessions.NextAsync(s, ct))));

        group.MapPost("/problems/{problemId:guid}/hint", (Guid sessionId, Guid problemId, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithSession(db, http, sessionId, ct, async s => Results.Ok(await sessions.HintAsync(s, problemId, ct))));

        group.MapPost("/problems/{problemId:guid}/answer", (Guid sessionId, Guid problemId, AnswerRequest req, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Answer))
                return Task.FromResult(ApiResults.Validation("answer", Tr("Escolha ou digite uma resposta.", "Choose or type an answer.", "Elige o escribe una respuesta.")));
            if (req.Answer.Length > 400)
                return Task.FromResult(ApiResults.Validation("answer", Tr("Resposta longa demais.", "Answer is too long.", "Respuesta demasiado larga.")));
            return WithSession(db, http, sessionId, ct, async s => Results.Ok(await sessions.AnswerAsync(s, problemId, req.Answer, ct)));
        });

        group.MapGet("/summary", (Guid sessionId, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithSession(db, http, sessionId, ct, async s => Results.Ok(await sessions.SummaryAsync(s, ct))));
    }

    private static IResult Created(GameSession session) =>
        Results.Created($"/api/sessions/{session.Id}", new StartResponse(session.Id, session.ProblemCount, session.Mode));

    private static async Task<IResult> WithProfileAndSubject(AppDbContext db, HttpContext http, Guid profileId, string slug, CancellationToken ct, Func<PlayerProfile, Subject, Task<IResult>> action)
    {
        var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
        if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));
        var subject = await db.Subjects.SingleOrDefaultAsync(s => s.Slug == slug, ct);
        if (subject is null) return ApiResults.NotFound(Tr("Tópico não encontrado.", "Subject not found.", "Tema no encontrado."));
        return await Handle(() => action(profile, subject));
    }

    /// <summary>Carrega a sessão garantindo que pertence a um perfil do usuário logado.</summary>
    private static async Task<IResult> WithSession(AppDbContext db, HttpContext http, Guid sessionId, CancellationToken ct, Func<GameSession, Task<IResult>> action)
    {
        var userId = http.User.UserId();
        var session = await db.GameSessions
            .Where(s => s.Id == sessionId && db.Profiles.Any(p => p.Id == s.ProfileId && p.UserId == userId))
            .SingleOrDefaultAsync(ct);
        if (session is null) return ApiResults.NotFound(Tr("Sessão não encontrada.", "Session not found.", "Sesión no encontrada."));
        return await Handle(() => action(session));
    }

    private static async Task<IResult> Handle(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (SessionException e)
        {
            return e.Error switch
            {
                SessionError.NotFound or SessionError.NotServed => ApiResults.NotFound(e.Message),
                SessionError.Locked or SessionError.NotAvailable => ApiResults.Forbidden(e.Message),
                _ => ApiResults.Conflict(e.Message),
            };
        }
    }
}
