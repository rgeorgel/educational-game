using System.Text.Json;
using EduGame.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

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
    .WithName("ListSubjects");

app.Run();

public partial class Program;
