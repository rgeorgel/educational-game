using EduGame.Api.Common;
using EduGame.Domain.Progression;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Calibration;

/// <summary>
/// Recalibra periodicamente a dificuldade de cada gerador (por habilidade) com as respostas dos últimos 30 dias.
/// Configuração: Calibration:Enabled (padrão true), Calibration:IntervalMinutes (padrão 60).
/// </summary>
public sealed class CalibrationService(IServiceScopeFactory scopes, IConfiguration config, ILogger<CalibrationService> logger) : BackgroundService
{
    public const int WindowDays = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Calibration:Enabled", true)) return;
        var interval = TimeSpan.FromMinutes(config.GetValue("Calibration:IntervalMinutes", 60));

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var changed = await RunOnceAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), scope.ServiceProvider.GetRequiredService<GameClock>(), stoppingToken);
                if (changed > 0) logger.LogInformation("Calibração ajustou {Count} gerador(es).", changed);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Falha na calibração de dificuldade.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Executa uma rodada de calibração. Devolve quantos geradores mudaram de deslocamento.</summary>
    public static async Task<int> RunOnceAsync(AppDbContext db, GameClock clock, CancellationToken ct)
    {
        var since = clock.Now.AddDays(-WindowDays);
        var rows = await db.Attempts
            .Where(a => a.CreatedAt >= since)
            .Join(db.SessionProblems, a => a.SessionProblemId, p => p.Id, (a, p) => new { p.SkillId, p.GeneratorKey, p.Difficulty, a.RatingBefore, a.IsCorrect, a.HintsUsed })
            .ToListAsync(ct);

        var changed = 0;
        var templates = await db.ProblemTemplates.Where(t => t.Active).ToListAsync(ct);
        foreach (var template in templates)
        {
            var samples = rows
                .Where(r => r.SkillId == template.SkillId && r.GeneratorKey == template.GeneratorKey)
                .Select(r => new Domain.Progression.Calibration.Sample(r.RatingBefore, r.Difficulty, Rating.Outcome(r.IsCorrect, r.HintsUsed)))
                .ToList();
            var offset = Domain.Progression.Calibration.NewOffset(template.DifficultyOffset, samples);
            if (offset == template.DifficultyOffset) continue;
            template.DifficultyOffset = offset;
            template.CalibratedAt = clock.Now;
            changed++;
        }
        await db.SaveChangesAsync(ct);
        return changed;
    }
}
