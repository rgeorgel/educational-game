using EduGame.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EduGame.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");

        services.AddDbContext<AppDbContext>(o => o
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        return services;
    }

    /// <summary>Aplica migrations pendentes e o seed de conteúdo.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool migrate, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        if (migrate)
        {
            logger.LogInformation("Aplicando migrations…");
            await db.Database.MigrateAsync(ct);
        }

        await ContentSeeder.SeedAsync(db, ct);
        logger.LogInformation("Banco de dados pronto.");
    }
}
