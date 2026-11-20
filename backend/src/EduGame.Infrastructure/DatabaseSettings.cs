using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EduGame.Infrastructure;

/// <summary>
/// Monta a conexão com o Postgres a partir de campos separados (seção "Database"):
/// Host, Port, Name, Username, Password e SslMode (opcional).
/// Em Docker eles vêm de DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD e DB_SSL_MODE.
///
/// Se Database:Host não estiver definido, usa ConnectionStrings:Default (útil em testes).
/// A string é montada com NpgsqlConnectionStringBuilder, então senhas com ";" ou "=" funcionam.
/// </summary>
public static class DatabaseSettings
{
    public static string BuildConnectionString(IConfiguration config)
    {
        var db = config.GetSection("Database");
        var host = db["Host"];

        if (string.IsNullOrWhiteSpace(host))
        {
            var fallback = config.GetConnectionString("Default");
            if (!string.IsNullOrWhiteSpace(fallback)) return fallback;
            throw new InvalidOperationException(
                "Banco de dados não configurado. Defina Database:Host, Database:Port, Database:Name, Database:Username e Database:Password " +
                "(no Docker: DB_HOST, DB_PORT, DB_NAME, DB_USER e DB_PASSWORD).");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host.Trim(),
            Port = ParsePort(db["Port"]),
            Database = Required(db, "Name", "DB_NAME"),
            Username = Required(db, "Username", "DB_USER"),
            Password = Required(db, "Password", "DB_PASSWORD"),
        };

        if (!string.IsNullOrWhiteSpace(db["SslMode"]))
        {
            builder.SslMode = Enum.TryParse<SslMode>(db["SslMode"], ignoreCase: true, out var ssl)
                ? ssl
                : throw new InvalidOperationException($"Database:SslMode inválido: \"{db["SslMode"]}\". Use Disable, Allow, Prefer, Require, VerifyCA ou VerifyFull.");
        }

        return builder.ConnectionString;
    }

    private static int ParsePort(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 5432;
        return int.TryParse(value, out var port) && port is > 0 and < 65536
            ? port
            : throw new InvalidOperationException($"Database:Port inválida: \"{value}\".");
    }

    private static string Required(IConfigurationSection db, string key, string envName) =>
        string.IsNullOrWhiteSpace(db[key])
            ? throw new InvalidOperationException($"Database:{key} não configurado (no Docker: {envName}).")
            : db[key]!;
}
