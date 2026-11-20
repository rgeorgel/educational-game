using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using EduGame.Api.Common;
using EduGame.Api.Features.Auth;
using EduGame.Api.Features.Calibration;
using EduGame.Api.Features.Map;
using EduGame.Api.Features.Profiles;
using EduGame.Api.Features.Progress;
using EduGame.Api.Features.Sessions;
using EduGame.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GameClock>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddHostedService<CalibrationService>();
builder.Services.AddScoped<SessionService>();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// ---------- Autenticação por cookie (mesma origem que o front, via nginx) ----------
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "edugame.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;
    // API: responder 401/403 em vez de redirecionar para uma página de login.
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();

// ---------- Limite de requisições (login/cadastro) ----------
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(RateLimits.Auth, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue("RateLimits:AuthPerMinute", 20), Window = TimeSpan.FromMinutes(1) }));
});

// O nginx repassa IP e protocolo originais.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

await app.Services.InitializeDatabaseAsync(
    migrate: app.Configuration.GetValue("Database:MigrateOnStartup", true));

app.MapOpenApi("/api/openapi/{documentName}.json");

app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }),
        }, JsonSerializerOptions.Web);
    },
});

var api = app.MapGroup("/api");

api.MapGet("/subjects", async (AppDbContext db, CancellationToken ct) =>
    await db.Subjects
        .OrderBy(s => s.Order)
        .Select(s => new
        {
            s.Slug,
            s.Name,
            s.Icon,
            Levels = s.Levels.OrderBy(l => l.Order).Select(l => new { l.Slug, l.Name, l.Icon, l.MinRating }),
        })
        .ToListAsync(ct))
    .WithName("ListSubjects")
    .WithTags("Content");

api.MapAuth();
api.MapProfiles();
api.MapMap();
api.MapSessions();
api.MapProgress();

app.Run();

public partial class Program;
