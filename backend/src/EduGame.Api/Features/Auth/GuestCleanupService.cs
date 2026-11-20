using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Auth;

/// <summary>
/// Apaga contas de convidado abandonadas (sem acesso há mais de Guests:RetentionDays, padrão 180 dias),
/// junto com perfis e progresso. Contas salvas nunca são apagadas por aqui.
/// </summary>
public sealed class GuestCleanupService(IServiceScopeFactory scopes, IConfiguration config, TimeProvider time, ILogger<GuestCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Guests:CleanupEnabled", true)) return;
        var retention = TimeSpan.FromDays(config.GetValue("Guests:RetentionDays", 180));

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var removed = await RunOnceAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), time.GetUtcNow(), retention, stoppingToken);
                if (removed > 0) logger.LogInformation("Removidas {Count} conta(s) de convidado sem uso.", removed);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Falha ao limpar convidados.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public static async Task<int> RunOnceAsync(AppDbContext db, DateTimeOffset now, TimeSpan retention, CancellationToken ct)
    {
        var limit = now - retention;
        var ids = await db.Users
            .Where(u => u.IsGuest && (u.LastSeenAt ?? u.CreatedAt) < limit)
            .Select(u => u.Id)
            .ToListAsync(ct);
        if (ids.Count == 0) return 0;

        // Perfis primeiro (o progresso cai em cascata), depois as contas.
        await db.Profiles.Where(p => ids.Contains(p.UserId)).ExecuteDeleteAsync(ct);
        return await db.Users.Where(u => ids.Contains(u.Id)).ExecuteDeleteAsync(ct);
    }
}
