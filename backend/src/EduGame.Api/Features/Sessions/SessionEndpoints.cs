using EduGame.Api.Common;
using EduGame.Domain.Sessions;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Sessions;

public static class SessionEndpoints
{
    public sealed record StartRequest(int StageId);
    public sealed record StartResponse(Guid SessionId, int Total);
    public sealed record AnswerRequest(string Answer);

    public static void MapSessions(this RouteGroupBuilder api)
    {
        api.MapPost("/profiles/{profileId:guid}/sessions", async (
            Guid profileId, StartRequest req, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            return await Handle(async () =>
            {
                var session = await sessions.StartAsync(profile, req.StageId, ct);
                return Results.Created($"/api/sessions/{session.Id}", new StartResponse(session.Id, session.ProblemCount));
            });
        }).RequireAuthorization().WithTags("Sessions");

        var group = api.MapGroup("/sessions/{sessionId:guid}").WithTags("Sessions").RequireAuthorization();

        group.MapGet("/next", (Guid sessionId, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithSession(db, http, sessionId, ct, async s => Results.Ok(await sessions.NextAsync(s, ct))));

        group.MapPost("/problems/{problemId:guid}/hint", (Guid sessionId, Guid problemId, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithSession(db, http, sessionId, ct, async s => Results.Ok(await sessions.HintAsync(s, problemId, ct))));

        group.MapPost("/problems/{problemId:guid}/answer", (Guid sessionId, Guid problemId, AnswerRequest req, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Answer))
                return Task.FromResult(ApiResults.Validation("answer", "Escolha ou digite uma resposta."));
            return WithSession(db, http, sessionId, ct, async s => Results.Ok(await sessions.AnswerAsync(s, problemId, req.Answer, ct)));
        });

        group.MapGet("/summary", (Guid sessionId, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
            WithSession(db, http, sessionId, ct, async s => Results.Ok(await sessions.SummaryAsync(s, ct))));
    }

    /// <summary>Carrega a sessão garantindo que pertence a um perfil do usuário logado.</summary>
    private static async Task<IResult> WithSession(AppDbContext db, HttpContext http, Guid sessionId, CancellationToken ct, Func<GameSession, Task<IResult>> action)
    {
        var userId = http.User.UserId();
        var session = await db.GameSessions
            .Where(s => s.Id == sessionId && db.Profiles.Any(p => p.Id == s.ProfileId && p.UserId == userId))
            .SingleOrDefaultAsync(ct);
        if (session is null) return ApiResults.NotFound("Sessão não encontrada.");
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
                SessionError.Locked => ApiResults.Forbidden(e.Message),
                _ => ApiResults.Conflict(e.Message),
            };
        }
    }
}
