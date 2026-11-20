using System.ComponentModel.DataAnnotations;
using EduGame.Api.Common;
using EduGame.Api.Features.Profiles;
using EduGame.Domain.Players;
using EduGame.Infrastructure;
using EduGame.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Auth;

public static class AuthEndpoints
{
    /// <summary>Por quanto tempo o aparelho "lembra" do convidado (renovado a cada acesso).</summary>
    public static readonly TimeSpan GuestCookieLifetime = TimeSpan.FromDays(365);

    public sealed record RegisterRequest(string Email, string Password, bool AcceptTerms, OriginRequest? Origin = null);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record GuestRequest(string Nickname, string Avatar, OriginRequest? Origin = null);
    public sealed record MeResponse(string? Email, bool IsGuest, IReadOnlyList<ProfileEndpoints.ProfileDto> Profiles);

    public static void MapAuth(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth").RequireRateLimiting(RateLimits.Auth);

        auth.MapPost("/register", async (RegisterRequest req, HttpContext http, UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db, GameClock clock, CancellationToken ct) =>
        {
            if (ValidateAccountData(req.Email, req.AcceptTerms, out var email) is { } invalid) return invalid;

            var user = new AppUser { UserName = email, Email = email, ConsentAcceptedAt = DateTimeOffset.UtcNow, LastSeenAt = DateTimeOffset.UtcNow };
            SignupOrigin.Apply(user, req.Origin, http.Request.Host.Host);
            var result = await users.CreateAsync(user, req.Password ?? "");
            if (!result.Succeeded) return IdentityError(result);

            await signIn.SignInAsync(user, isPersistent: true);
            await db.RecordVisitAsync(user.Id, clock.Today, ct);
            return Results.Ok(new MeResponse(email, false, []));
        });

        // Jogar sem cadastro: cria uma conta de convidado (só apelido) presa a este aparelho.
        api.MapPost("/auth/guest", async (GuestRequest req, HttpContext http, UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db, GameClock clock, CancellationToken ct) =>
        {
            var nickname = (req.Nickname ?? "").Trim();
            if (ProfileEndpoints.ValidateNicknameAndAvatar(nickname, req.Avatar) is { } invalid) return invalid;

            var user = new AppUser { UserName = $"convidado-{Guid.NewGuid():N}", IsGuest = true, LastSeenAt = DateTimeOffset.UtcNow };
            SignupOrigin.Apply(user, req.Origin, http.Request.Host.Host);
            var result = await users.CreateAsync(user);
            if (!result.Succeeded) return IdentityError(result);

            var profile = new PlayerProfile { UserId = user.Id, Nickname = nickname, Avatar = req.Avatar };
            db.Profiles.Add(profile);
            await db.SaveChangesAsync(ct);
            await db.RecordVisitAsync(user.Id, clock.Today, ct);

            // Cookie longo: quem volta neste aparelho continua de onde parou.
            await signIn.SignInAsync(user, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(GuestCookieLifetime),
            });
            return Results.Ok(new MeResponse(null, true, [ProfileEndpoints.ToDto(profile)]));
        }).RequireRateLimiting(RateLimits.Guest).WithTags("Auth");

        auth.MapPost("/login", async (LoginRequest req, UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db, GameClock clock, CancellationToken ct) =>
        {
            var user = await users.FindByEmailAsync(req.Email?.Trim() ?? "");
            if (user is null)
                return Results.Problem(title: "Login inválido", detail: "E-mail ou senha incorretos.", statusCode: StatusCodes.Status401Unauthorized);

            var result = await signIn.PasswordSignInAsync(user, req.Password ?? "", isPersistent: true, lockoutOnFailure: true);
            if (result.IsLockedOut)
                return Results.Problem(title: "Conta bloqueada", detail: "Muitas tentativas. Tente de novo em alguns minutos.", statusCode: StatusCodes.Status429TooManyRequests);
            if (!result.Succeeded)
                return Results.Problem(title: "Login inválido", detail: "E-mail ou senha incorretos.", statusCode: StatusCodes.Status401Unauthorized);

            user.LastSeenAt = DateTimeOffset.UtcNow;
            await users.UpdateAsync(user);
            await db.RecordVisitAsync(user.Id, clock.Today, ct);
            var profiles = await ProfileEndpoints.ListAsync(db, user.Id, ct);
            return Results.Ok(new MeResponse(user.Email!, false, profiles));
        });

        auth.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        });

        // "Salvar progresso": transforma o convidado em conta normal, mantendo tudo (mesmo id, perfis e progresso).
        api.MapPost("/auth/upgrade", async (RegisterRequest req, HttpContext http, UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(http.User);
            if (user is null) return Results.Unauthorized();
            if (!user.IsGuest) return ApiResults.Conflict("Esta conta já está salva.");
            if (ValidateAccountData(req.Email, req.AcceptTerms, out var email) is { } invalid) return invalid;
            if (await users.FindByEmailAsync(email) is not null)
                return ApiResults.Validation("email", "Já existe uma conta com este e-mail. Use outro e-mail ou entre nessa conta (o progresso deste aparelho não é juntado).");

            var password = req.Password ?? "";
            foreach (var validator in users.PasswordValidators)
                if (!(await validator.ValidateAsync(users, user, password)).Succeeded)
                    return ApiResults.Validation("password", "A senha precisa ter pelo menos 8 caracteres.");

            user.IsGuest = false;
            user.Email = email;
            user.UserName = email;
            user.ConsentAcceptedAt = DateTimeOffset.UtcNow;
            var result = await users.AddPasswordAsync(user, password); // também salva e-mail/nome normalizados
            if (!result.Succeeded)
            {
                user.IsGuest = true;
                return IdentityError(result);
            }

            // A troca de senha muda o carimbo de segurança: renova o login (cookie normal de conta).
            await signIn.SignInAsync(user, isPersistent: true);
            var profiles = await ProfileEndpoints.ListAsync(db, user.Id, ct);
            return Results.Ok(new MeResponse(email, false, profiles));
        }).RequireAuthorization().RequireRateLimiting(RateLimits.Auth).WithTags("Auth");

        api.MapGet("/me", async (HttpContext http, UserManager<AppUser> users, AppDbContext db, GameClock clock, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(http.User);
            if (user is null) return Results.Unauthorized();

            // Registra o último acesso (no máximo de hora em hora, para não escrever a cada chamada).
            if (user.LastSeenAt is null || DateTimeOffset.UtcNow - user.LastSeenAt > TimeSpan.FromHours(1))
            {
                user.LastSeenAt = DateTimeOffset.UtcNow;
                await users.UpdateAsync(user);
            }

            // O app chama /me uma vez ao abrir: conta como um acesso do dia.
            await db.RecordVisitAsync(user.Id, clock.Today, ct);

            var profiles = await ProfileEndpoints.ListAsync(db, user.Id, ct);
            return Results.Ok(new MeResponse(user.Email, user.IsGuest, profiles));
        }).RequireAuthorization().WithTags("Auth");

        // LGPD: o responsável pode apagar a conta e todos os dados dos perfis (convidados também).
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

    private static IResult? ValidateAccountData(string? rawEmail, bool acceptTerms, out string email)
    {
        email = rawEmail?.Trim() ?? "";
        if (!acceptTerms)
            return ApiResults.Validation("acceptTerms", "É preciso aceitar os termos e autorizar o uso dos dados para criar a conta.");
        if (!new EmailAddressAttribute().IsValid(email) || email.Length > 254)
            return ApiResults.Validation("email", "Informe um e-mail válido.");
        return null;
    }

    private static IResult IdentityError(IdentityResult result)
    {
        var duplicate = result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");
        return duplicate
            ? ApiResults.Validation("email", "Já existe uma conta com este e-mail.")
            : ApiResults.Validation("password", "A senha precisa ter pelo menos 8 caracteres.");
    }
}

public static class GuestAccess
{
    /// <summary>Convidados não podem usar recursos que envolvem outras pessoas (turmas).</summary>
    public static Task<bool> IsGuestAsync(this AppDbContext db, Guid userId, CancellationToken ct) =>
        db.Users.Where(u => u.Id == userId).Select(u => u.IsGuest).SingleOrDefaultAsync(ct);
}
