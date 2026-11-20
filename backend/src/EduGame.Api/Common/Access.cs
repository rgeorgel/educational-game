using System.Security.Claims;
using EduGame.Domain.Players;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Common;

public static class Access
{
    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Usuário não autenticado."));

    /// <summary>Carrega o perfil apenas se ele pertencer ao usuário logado.</summary>
    public static Task<PlayerProfile?> FindOwnedProfileAsync(this AppDbContext db, ClaimsPrincipal user, Guid profileId, CancellationToken ct) =>
        db.Profiles.SingleOrDefaultAsync(p => p.Id == profileId && p.UserId == user.UserId(), ct);
}

public static class ApiResults
{
    public static IResult NotFound(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status404NotFound, title: "Não encontrado");

    public static IResult Conflict(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: "Conflito");

    public static IResult Forbidden(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status403Forbidden, title: "Não permitido");

    public static IResult Validation(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] }, title: "Dados inválidos");
}
