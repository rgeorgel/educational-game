using System.ComponentModel.DataAnnotations;
using EduGame.Api.Common;
using EduGame.Api.Features.Profiles;
using EduGame.Infrastructure;
using EduGame.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Auth;

public static class AuthEndpoints
{
    public sealed record RegisterRequest(string Email, string Password, bool AcceptTerms);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record MeResponse(string Email, IReadOnlyList<ProfileEndpoints.ProfileDto> Profiles);

    public static void MapAuth(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth").RequireRateLimiting(RateLimits.Auth);

        auth.MapPost("/register", async (RegisterRequest req, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
        {
            if (!req.AcceptTerms)
                return ApiResults.Validation("acceptTerms", "É preciso aceitar os termos e autorizar o uso dos dados para criar a conta.");
            var email = req.Email?.Trim() ?? "";
            if (!new EmailAddressAttribute().IsValid(email) || email.Length > 254)
                return ApiResults.Validation("email", "Informe um e-mail válido.");

            var user = new AppUser { UserName = email, Email = email, ConsentAcceptedAt = DateTimeOffset.UtcNow };
            var result = await users.CreateAsync(user, req.Password ?? "");
            if (!result.Succeeded)
            {
                var duplicate = result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");
                return duplicate
                    ? ApiResults.Validation("email", "Já existe uma conta com este e-mail.")
                    : ApiResults.Validation("password", "A senha precisa ter pelo menos 8 caracteres.");
            }

            await signIn.SignInAsync(user, isPersistent: true);
            return Results.Ok(new MeResponse(email, []));
        });

        auth.MapPost("/login", async (LoginRequest req, UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db, CancellationToken ct) =>
        {
            var user = await users.FindByEmailAsync(req.Email?.Trim() ?? "");
            if (user is null)
                return Results.Problem(title: "Login inválido", detail: "E-mail ou senha incorretos.", statusCode: StatusCodes.Status401Unauthorized);

            var result = await signIn.PasswordSignInAsync(user, req.Password ?? "", isPersistent: true, lockoutOnFailure: true);
            if (result.IsLockedOut)
                return Results.Problem(title: "Conta bloqueada", detail: "Muitas tentativas. Tente de novo em alguns minutos.", statusCode: StatusCodes.Status429TooManyRequests);
            if (!result.Succeeded)
                return Results.Problem(title: "Login inválido", detail: "E-mail ou senha incorretos.", statusCode: StatusCodes.Status401Unauthorized);

            var profiles = await ProfileEndpoints.ListAsync(db, user.Id, ct);
            return Results.Ok(new MeResponse(user.Email!, profiles));
        });

        auth.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        });

        api.MapGet("/me", async (HttpContext http, UserManager<AppUser> users, AppDbContext db, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(http.User);
            if (user is null) return Results.Unauthorized();
            var profiles = await ProfileEndpoints.ListAsync(db, user.Id, ct);
            return Results.Ok(new MeResponse(user.Email!, profiles));
        }).RequireAuthorization().WithTags("Auth");

        // LGPD: o responsável pode apagar a conta e todos os dados dos perfis.
        api.MapDelete("/me", async (HttpContext http, UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(http.User);
            if (user is null) return Results.Unauthorized();
            await db.Profiles.Where(p => p.UserId == user.Id).ExecuteDeleteAsync(ct);
            await users.DeleteAsync(user);
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization().WithTags("Auth");
    }
}
