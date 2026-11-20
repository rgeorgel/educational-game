using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EduGame.Admin;

/// <summary>
/// Login do super admin: um único usuário definido por ADMIN_USERNAME e ADMIN_PASSWORD.
/// Sem essas variáveis (ou com senha curta) o login fica desativado e o painel avisa como configurar.
/// </summary>
public sealed class AdminCredentials(IConfiguration config)
{
    public const int MinPasswordLength = 12;

    private readonly string? _username = config["Admin:Username"]?.Trim();
    private readonly string? _password = config["Admin:Password"];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_username) && _password is { Length: >= MinPasswordLength };

    public string? ConfigurationProblem =>
        string.IsNullOrWhiteSpace(_username) || string.IsNullOrEmpty(_password)
            ? "Defina ADMIN_USERNAME e ADMIN_PASSWORD no .env para ativar o painel."
            : _password.Length < MinPasswordLength
                ? $"ADMIN_PASSWORD precisa ter pelo menos {MinPasswordLength} caracteres."
                : null;

    /// <summary>Compara em tempo constante (não revela quantos caracteres acertou).</summary>
    public bool Matches(string? username, string? password)
    {
        if (!IsConfigured) return false;
        var userOk = FixedEquals(username?.Trim() ?? "", _username!);
        var passOk = FixedEquals(password ?? "", _password!);
        return userOk & passOk;
    }

    public string Username => _username ?? "";

    private static bool FixedEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(a)), SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}

public static class AdminAuthEndpoints
{
    public sealed record LoginRequest(string? Username, string? Password);

    public static void MapAdminAuth(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/session", (HttpContext http, AdminCredentials creds) => Results.Ok(new
        {
            configured = creds.IsConfigured,
            problem = creds.ConfigurationProblem,
            authenticated = http.User.Identity?.IsAuthenticated == true,
            username = http.User.Identity?.Name,
        }));

        api.MapPost("/login", async (LoginRequest req, HttpContext http, AdminCredentials creds, ILoggerFactory logs) =>
        {
            if (!creds.IsConfigured)
                return Results.Problem(creds.ConfigurationProblem, statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!creds.Matches(req.Username, req.Password))
            {
                logs.CreateLogger("AdminLogin").LogWarning("Tentativa de login do admin recusada (IP {Ip}).", http.Connection.RemoteIpAddress);
                await Task.Delay(Random.Shared.Next(300, 700)); // atrasa tentativas automáticas
                return Results.Problem("Usuário ou senha incorretos.", statusCode: StatusCodes.Status401Unauthorized);
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, creds.Username), new Claim(ClaimTypes.Role, "super-admin")],
                CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.Ok(new { username = creds.Username });
        }).RequireRateLimiting("login");

        api.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });
    }
}
