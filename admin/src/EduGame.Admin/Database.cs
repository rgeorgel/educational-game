using Npgsql;

namespace EduGame.Admin;

/// <summary>
/// Conexão com o mesmo Postgres do jogo, montada dos campos da seção "Database"
/// (no Docker: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD e DB_SSL_MODE).
///
/// Toda conexão abre em modo SOMENTE LEITURA e com tempo máximo por consulta. A única exceção são as
/// ações de plano (PlanAdmin), que abrem de propósito uma transação de leitura e escrita na tabela subscriptions.
/// Fora delas, mesmo que algum SQL tentasse alterar dados, o Postgres recusaria.
/// Para uma camada extra, use um usuário do banco que só tenha permissão de leitura (ADMIN_DB_USER).
/// </summary>
public static class Database
{
    public static NpgsqlDataSource CreateDataSource(IConfiguration config) =>
        NpgsqlDataSource.Create(BuildConnectionString(config));

    /// <summary>
    /// Parâmetros de início de sessão (não são desfeitos quando a conexão volta ao pool):
    /// toda transação é somente leitura e nenhuma consulta passa de 20 segundos.
    /// </summary>
    internal const string SessionOptions = "-c default_transaction_read_only=on -c statement_timeout=20000";

    public static string BuildConnectionString(IConfiguration config)
    {
        var db = config.GetSection("Database");
        var host = db["Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            var fallback = config.GetConnectionString("Default");
            if (!string.IsNullOrWhiteSpace(fallback))
                return new NpgsqlConnectionStringBuilder(fallback) { Options = SessionOptions, ApplicationName = "edugame-admin" }.ConnectionString;
            throw new InvalidOperationException(
                "Banco de dados não configurado. Defina DB_HOST, DB_PORT, DB_NAME, DB_USER e DB_PASSWORD.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host.Trim(),
            Port = int.TryParse(db["Port"], out var port) && port is > 0 and < 65536 ? port : 5432,
            Database = Required(db, "Name", "DB_NAME"),
            Username = Required(db, "Username", "DB_USER"),
            Password = Required(db, "Password", "DB_PASSWORD"),
            MaxPoolSize = 10,
        };
        builder.Options = SessionOptions;
        builder.ApplicationName = "edugame-admin";
        if (!string.IsNullOrWhiteSpace(db["SslMode"]))
        {
            builder.SslMode = Enum.TryParse<SslMode>(db["SslMode"], ignoreCase: true, out var ssl)
                ? ssl
                : throw new InvalidOperationException($"DB_SSL_MODE inválido: \"{db["SslMode"]}\".");
        }
        return builder.ConnectionString;
    }

    private static string Required(IConfigurationSection db, string key, string envName) =>
        string.IsNullOrWhiteSpace(db[key])
            ? throw new InvalidOperationException($"Database:{key} não configurado (no Docker: {envName}).")
            : db[key]!;
}

/// <summary>Atalhos para consultas com parâmetros (nunca concatenar valores no SQL).</summary>
public static class Sql
{
    public static async Task<List<T>> ListAsync<T>(
        this NpgsqlDataSource ds, string sql, Func<NpgsqlDataReader, T> map, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using var cmd = ds.CreateCommand(sql);
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<T>();
        while (await reader.ReadAsync(ct)) list.Add(map(reader));
        return list;
    }

    public static async Task<T?> SingleAsync<T>(
        this NpgsqlDataSource ds, string sql, Func<NpgsqlDataReader, T> map, CancellationToken ct, params (string Name, object? Value)[] args) =>
        (await ds.ListAsync(sql, map, ct, args)).FirstOrDefault();

    public static int Int(this NpgsqlDataReader r, string col) => r[col] is DBNull ? 0 : Convert.ToInt32(r[col]);
    public static long Long(this NpgsqlDataReader r, string col) => r[col] is DBNull ? 0 : Convert.ToInt64(r[col]);
    public static double? Dbl(this NpgsqlDataReader r, string col) => r[col] is DBNull ? null : Math.Round(Convert.ToDouble(r[col]), 1);
    public static string? Str(this NpgsqlDataReader r, string col) => r[col] as string;
    public static DateTimeOffset? Time(this NpgsqlDataReader r, string col) =>
        r[col] is DateTime dt ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)) : null;
    public static DateOnly Date(this NpgsqlDataReader r, string col) => r[col] switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        var other => throw new InvalidCastException($"Coluna {col}: esperado date, veio {other?.GetType().Name}."),
    };
}
