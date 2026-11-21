using System.Threading.RateLimiting;
using EduGame.Admin;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(Database.CreateDataSource(builder.Configuration));
builder.Services.AddSingleton<AdminCredentials>();
builder.Services.AddProblemDetails();

// Chaves que assinam o cookie de login. Num volume (Admin:KeysPath), o login sobrevive a deploys.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("edugame-admin");
if (builder.Configuration["Admin:KeysPath"] is { Length: > 0 } keysPath)
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

var sessionHours = builder.Configuration.GetValue("Admin:SessionHours", 8);
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "edugame.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(sessionHours);
        options.SlidingExpiration = false;
        // API: responde 401/403 em vez de redirecionar para uma página de login.
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(null) // arquivos estáticos e /api/session são públicos; os dados exigem login
    .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireRole("super-admin").Build());

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "?",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
});

// Atrás de um proxy com HTTPS (ex.: Coolify), usa o protocolo original para marcar o cookie como Secure.
// Só o protocolo é lido: o IP do cliente não é confiado a cabeçalhos (o limite de login usa o IP da conexão).
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; " +
        "font-src 'self'; object-src 'none'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";
    h["X-Content-Type-Options"] = "nosniff";
    h["Referrer-Policy"] = "no-referrer";
    h["X-Frame-Options"] = "DENY";
    h["X-Robots-Tag"] = "noindex, nofollow";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (ctx.Request.Path.StartsWithSegments("/api")) h.CacheControl = "no-store";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Saúde: confirma que o banco responde e que a conexão está mesmo em modo somente leitura.
app.MapGet("/health", async (Npgsql.NpgsqlDataSource ds, CancellationToken ct) =>
{
    try
    {
        await using var cmd = ds.CreateCommand("SHOW transaction_read_only");
        var readOnly = (string?)await cmd.ExecuteScalarAsync(ct) == "on";
        return readOnly
            ? Results.Ok(new { status = "ok", readOnly })
            : Results.Json(new { status = "conexão não está em modo somente leitura", readOnly }, statusCode: 503);
    }
    catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException)
    {
        return Results.Json(new { status = "banco indisponível" }, statusCode: 503);
    }
});
app.MapAdminAuth();
app.MapMetrics();
app.MapPlanAdmin();

var creds = app.Services.GetRequiredService<AdminCredentials>();
if (!creds.IsConfigured) app.Logger.LogWarning("Painel admin sem login ativo: {Problem}", creds.ConfigurationProblem);

app.Run();

public partial class Program;
