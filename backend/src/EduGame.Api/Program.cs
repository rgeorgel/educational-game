using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using EduGame.Api.Common;
using EduGame.Api.Features.Auth;
using EduGame.Api.Features.Calibration;
using EduGame.Api.Features.Map;
using EduGame.Api.Features.Profiles;
using EduGame.Api.Features.Progress;
using EduGame.Api.Features.Reports;
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
builder.Services.AddHostedService<GuestCleanupService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ClassroomInsights>();
builder.Services.AddScoped<EduGame.Api.Features.Billing.PlanService>();
builder.Services.AddSingleton<EduGame.Api.Features.Billing.StripeSettings>();
builder.Services.AddSingleton<EduGame.Api.Features.Billing.IStripeGateway, EduGame.Api.Features.Billing.StripeGateway>();
builder.Services.AddScoped<EduGame.Api.Features.Billing.StripeWebhook>();

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
    o.AddPolicy(RateLimits.Guest, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue("RateLimits:GuestPerMinute", 120), Window = TimeSpan.FromMinutes(1) }));
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

// Identifica quem respondeu: ajuda a diagnosticar proxies apontando para o serviço errado.
app.Use((ctx, next) =>
{
    ctx.Response.Headers["X-Served-By"] = "edugame-api";
    return next();
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Comando de terminal (ex.: "dotnet EduGame.Api.dll plano listar"): roda e sai, sem subir o servidor.
if (EduGame.Api.Features.Billing.PlanCommand.IsInvoked(args))
{
    Environment.ExitCode = await EduGame.Api.Features.Billing.PlanCommand.RunAsync(app.Services, args, Console.Out);
    return;
}
if (EduGame.Api.Features.Billing.StripeSetup.IsInvoked(args))
{
    Environment.ExitCode = await EduGame.Api.Features.Billing.StripeSetup.RunAsync(app.Services, args, Console.Out);
    return;
}

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

// Currículo público: todas as habilidades do jogo, por tópico e nível, com o alinhamento à BNCC.
// Serve a página "Currículo e BNCC" (pais e professores) e pode ser lido sem conta.
api.MapGet("/curriculum", async (AppDbContext db, HttpContext http, CancellationToken ct) =>
{
    var subjects = await db.Subjects.AsNoTracking().OrderBy(s => s.Order).Include(s => s.Levels).ToListAsync(ct);
    var skills = await db.Skills.AsNoTracking().Where(s => s.Templates.Any(t => t.Active)).OrderBy(s => s.Order).ToListAsync(ct);
    http.Response.Headers.CacheControl = "public, max-age=3600";
    return Results.Ok(subjects.Select(subject => new
    {
        subject.Slug,
        subject.Name,
        subject.Icon,
        Levels = subject.Levels.OrderBy(l => l.Order).Select(level => new
        {
            level.Slug,
            level.Name,
            level.Icon,
            Skills = skills.Where(k => k.SubjectId == subject.Id && k.LevelId == level.Id).Select(k => new
            {
                k.Slug,
                k.Name,
                k.Description,
                Bncc = EduGame.Api.Features.Progress.BnccRefDto.For(k.BnccCodes),
            }),
        }).Where(l => l.Skills.Any()),
    }));
})
    .WithName("Curriculum")
    .WithTags("Content");

// Catálogo de etapas (para o professor escolher a etapa de uma tarefa).
api.MapGet("/catalog/stages", async (AppDbContext db, CancellationToken ct) =>
{
    var stages = await db.Stages.AsNoTracking()
        .Select(s => new
        {
            s.Id, s.Name, s.Order, s.ProblemCount,
            RegionName = s.Region!.Name, RegionIcon = s.Region.Icon, RegionOrder = s.Region.Order,
            LevelSlug = s.Region.Level!.Slug, LevelName = s.Region.Level.Name, LevelIcon = s.Region.Level.Icon, LevelOrder = s.Region.Level.Order,
            SubjectSlug = s.Region.Subject!.Slug, SubjectName = s.Region.Subject.Name, SubjectIcon = s.Region.Subject.Icon, SubjectOrder = s.Region.Subject.Order,
        })
        .ToListAsync(ct);
    return Results.Ok(stages.OrderBy(s => s.SubjectOrder).ThenBy(s => s.LevelOrder).ThenBy(s => s.RegionOrder).ThenBy(s => s.Order)
        .Select(s => new { s.Id, s.Name, s.ProblemCount, s.RegionName, s.RegionIcon, s.LevelSlug, s.LevelName, s.LevelIcon, s.SubjectSlug, s.SubjectName, s.SubjectIcon }));
})
    .WithName("StageCatalog")
    .WithTags("Content");

api.MapAuth();
api.MapProfiles();
api.MapMap();
api.MapSessions();
api.MapProgress();
api.MapReports();
EduGame.Api.Features.Family.FamilyEndpoints.MapFamily(api);
EduGame.Api.Features.Billing.BillingEndpoints.MapBilling(api);

var stripeSettings = app.Services.GetRequiredService<EduGame.Api.Features.Billing.StripeSettings>();
app.Logger.LogInformation(stripeSettings.Enabled
    ? "Pagamentos pelo Stripe ligados (API {Version})."
    : "Pagamentos pelo Stripe desligados: planos no modo lançamento (API {Version}).", Stripe.StripeConfiguration.ApiVersion);

app.Run();

public partial class Program;
