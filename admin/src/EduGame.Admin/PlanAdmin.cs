using Npgsql;

namespace EduGame.Admin;

/// <summary>
/// Gerência de planos pelo super admin: dar teste grátis (7, 14, 30 dias…), conceder um plano à mão,
/// estender ou encerrar. É a ÚNICA escrita do painel e só mexe na tabela <c>subscriptions</c>,
/// numa transação aberta explicitamente como leitura e escrita (o resto continua somente leitura).
///
/// Assinaturas pagas pelo Stripe não são alteradas aqui: o Stripe as atualiza pelo webhook
/// e encerrar só no banco deixaria a cobrança ativa.
/// </summary>
public static class PlanAdmin
{
    /// <summary>Planos que podem ser dados pelo painel (mesmos slugs de <c>Plans.cs</c> no jogo).</summary>
    public static readonly IReadOnlyDictionary<string, string> PlanNames = new Dictionary<string, string>
    {
        ["professor"] = "Professor Pro",
        ["familia"] = "Família",
        ["escola"] = "Escola",
    };

    public const string Trial = "teste";
    public const string Manual = "manual";
    public const string Stripe = "stripe";
    public const int MaxDays = 3650;
    public static readonly int[] TrialPresets = [7, 14, 30];

    public sealed record GrantRequest(string? Plan, string? Kind, int? Days, string? Note);
    public sealed record ExtendRequest(int? Days);

    public static void MapPlanAdmin(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.MapGet("/plans", () => Results.Ok(new
        {
            plans = PlanNames.Select(p => new { slug = p.Key, name = p.Value }),
            trialPresets = TrialPresets,
            maxDays = MaxDays,
        }));
        api.MapPost("/users/{id:guid}/plans", Grant);
        api.MapPost("/users/{id:guid}/plans/{subId:guid}/extend", Extend);
        api.MapPost("/users/{id:guid}/plans/{subId:guid}/end", End);
    }

    /// <summary>
    /// Dá um plano. "teste" sempre tem data para acabar; "manual" sem dias = sem data para acabar.
    /// Se a pessoa já tem um teste ou concessão ativa do mesmo plano, ela é trocada pela nova.
    /// </summary>
    private static async Task<IResult> Grant(Guid id, GrantRequest req, NpgsqlDataSource ds, HttpContext http, ILoggerFactory logs, CancellationToken ct)
    {
        var plan = req.Plan?.Trim();
        if (plan is null || !PlanNames.ContainsKey(plan)) return Invalid("Escolha o plano: Professor Pro, Família ou Escola.");
        var kind = req.Kind is Trial or Manual ? req.Kind : null;
        if (kind is null) return Invalid("Escolha entre teste grátis e concessão manual.");
        if (req.Days is { } d && (d < 1 || d > MaxDays)) return Invalid($"Os dias precisam ficar entre 1 e {MaxDays}.");
        if (kind == Trial && req.Days is null) return Invalid("O teste grátis precisa de uma duração (ex.: 7, 14 ou 30 dias).");
        var note = Note(req.Note) ?? (kind == Trial ? $"Teste grátis de {req.Days} dias" : "Concedido pelo painel admin");
        var admin = http.User.Identity?.Name ?? "admin";

        return await Write(ds, logs, async (conn, tx) =>
        {
            var user = await UserAsync(conn, tx, id, ct);
            if (user is null) return Results.Problem("Conta não encontrada.", statusCode: StatusCodes.Status404NotFound);
            if (user.Value.IsGuest) return Invalid("Convidados não têm plano. A pessoa precisa salvar o progresso (criar conta) antes.");

            await using (var check = Cmd(conn, tx, """
                SELECT 1 FROM subscriptions
                WHERE user_id = @id AND plan = @plan AND source = 'stripe' AND starts_at <= now() AND (ends_at IS NULL OR ends_at > now())
                LIMIT 1
                """, ("id", id), ("plan", plan)))
            {
                if (await check.ExecuteScalarAsync(ct) is not null)
                    return Results.Problem($"Esta conta já paga o {PlanNames[plan]} pelo Stripe.", statusCode: StatusCodes.Status409Conflict);
            }

            // Troca o teste/concessão anterior do mesmo plano pela nova (sem duplicar linhas ativas).
            await using (var end = Cmd(conn, tx, """
                UPDATE subscriptions SET ends_at = now()
                WHERE user_id = @id AND plan = @plan AND source IN ('teste', 'manual') AND (ends_at IS NULL OR ends_at > now())
                """, ("id", id), ("plan", plan)))
                await end.ExecuteNonQueryAsync(ct);

            var subId = Guid.CreateVersion7();
            await using (var insert = Cmd(conn, tx, """
                INSERT INTO subscriptions (id, user_id, plan, starts_at, ends_at, note, created_at, source)
                VALUES (@sid, @id, @plan, now(), CASE WHEN @days::int IS NULL THEN NULL ELSE now() + make_interval(days => @days::int) END, @note, now(), @kind)
                """, ("sid", subId), ("id", id), ("plan", plan), ("days", req.Days), ("note", $"{note} · por {admin}"), ("kind", kind)))
                await insert.ExecuteNonQueryAsync(ct);

            logs.CreateLogger("PlanAdmin").LogInformation("{Admin} deu {Kind} do plano {Plan} ({Days} dias) para {UserId}.",
                admin, kind, plan, req.Days?.ToString() ?? "sem fim", id);
            return Results.Ok(new { id = subId });
        }, ct);
    }

    /// <summary>Soma dias ao fim de um teste ou concessão (a partir de agora, se já tinha acabado).</summary>
    private static async Task<IResult> Extend(Guid id, Guid subId, ExtendRequest req, NpgsqlDataSource ds, HttpContext http, ILoggerFactory logs, CancellationToken ct)
    {
        if (req.Days is not { } days || days < 1 || days > MaxDays) return Invalid($"Os dias precisam ficar entre 1 e {MaxDays}.");
        return await Write(ds, logs, async (conn, tx) =>
        {
            var sub = await SubscriptionAsync(conn, tx, id, subId, ct);
            if (sub is null) return Results.Problem("Plano não encontrado.", statusCode: StatusCodes.Status404NotFound);
            if (sub.Value.Source == Stripe) return StripeManaged();
            if (sub.Value.EndsAt is null) return Invalid("Este plano já não tem data para acabar.");

            // Conta a partir do fim atual, ou de agora se já tinha acabado; depois encerra outro ativo do mesmo plano.
            await using (var cmd = Cmd(conn, tx, """
                UPDATE subscriptions SET ends_at = greatest(ends_at, now()) + make_interval(days => @days) WHERE id = @sid
                """, ("sid", subId), ("days", days)))
                await cmd.ExecuteNonQueryAsync(ct);
            await using (var dedupe = Cmd(conn, tx, """
                UPDATE subscriptions SET ends_at = now()
                WHERE user_id = @id AND plan = @plan AND id <> @sid AND source IN ('teste', 'manual') AND (ends_at IS NULL OR ends_at > now())
                """, ("id", id), ("plan", sub.Value.Plan), ("sid", subId)))
                await dedupe.ExecuteNonQueryAsync(ct);

            logs.CreateLogger("PlanAdmin").LogInformation("{Admin} estendeu em {Days} dias o plano {SubId} de {UserId}.",
                http.User.Identity?.Name, days, subId, id);
            return Results.NoContent();
        }, ct);
    }

    /// <summary>Encerra agora um teste, concessão ou escolha do lançamento.</summary>
    private static async Task<IResult> End(Guid id, Guid subId, NpgsqlDataSource ds, HttpContext http, ILoggerFactory logs, CancellationToken ct)
    {
        return await Write(ds, logs, async (conn, tx) =>
        {
            var sub = await SubscriptionAsync(conn, tx, id, subId, ct);
            if (sub is null) return Results.Problem("Plano não encontrado.", statusCode: StatusCodes.Status404NotFound);
            if (sub.Value.Source == Stripe) return StripeManaged();
            await using (var cmd = Cmd(conn, tx, "UPDATE subscriptions SET ends_at = now() WHERE id = @sid AND (ends_at IS NULL OR ends_at > now())", ("sid", subId)))
                await cmd.ExecuteNonQueryAsync(ct);
            logs.CreateLogger("PlanAdmin").LogInformation("{Admin} encerrou o plano {SubId} de {UserId}.", http.User.Identity?.Name, subId, id);
            return Results.NoContent();
        }, ct);
    }

    // ---------- Apoio ----------

    private static IResult Invalid(string detail) => Results.Problem(detail, statusCode: StatusCodes.Status400BadRequest);

    private static IResult StripeManaged() => Results.Problem(
        "Assinaturas pagas são controladas pelo Stripe. Para cancelar ou mudar, use o painel do Stripe (o jogo é avisado sozinho).",
        statusCode: StatusCodes.Status409Conflict);

    private static string? Note(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(150, note.Trim().Length)];

    private static NpgsqlCommand Cmd(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params (string Name, object? Value)[] args)
    {
        var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    private static async Task<(bool IsGuest, string? Email)?> UserAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid id, CancellationToken ct)
    {
        await using var cmd = Cmd(conn, tx, "SELECT is_guest, email FROM users WHERE id = @id", ("id", id));
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? (r.GetBoolean(0), r.IsDBNull(1) ? null : r.GetString(1)) : null;
    }

    private static async Task<(string Plan, string Source, DateTime? EndsAt)?> SubscriptionAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, Guid userId, Guid subId, CancellationToken ct)
    {
        await using var cmd = Cmd(conn, tx, "SELECT plan, source, ends_at FROM subscriptions WHERE id = @sid AND user_id = @id", ("sid", subId), ("id", userId));
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? (r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetDateTime(2)) : null;
    }

    /// <summary>
    /// Abre uma transação de leitura e escrita (a conexão do painel é somente leitura por padrão)
    /// e trava as alterações de planos da mesma conta, para dois cliques não criarem duas linhas.
    /// </summary>
    private static async Task<IResult> Write(NpgsqlDataSource ds, ILoggerFactory logs, Func<NpgsqlConnection, NpgsqlTransaction, Task<IResult>> work, CancellationToken ct)
    {
        try
        {
            await using var conn = await ds.OpenConnectionAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);
            await using (var rw = new NpgsqlCommand("SET TRANSACTION READ WRITE; SELECT pg_advisory_xact_lock(hashtext('edugame-admin-plans'))", conn, tx))
                await rw.ExecuteNonQueryAsync(ct);
            var result = await work(conn, tx);
            if (result is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 }) await tx.CommitAsync(ct);
            else await tx.RollbackAsync(ct);
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.InsufficientPrivilege or PostgresErrorCodes.ReadOnlySqlTransaction)
        {
            logs.CreateLogger("PlanAdmin").LogError(ex, "O usuário do banco do painel não pode alterar a tabela subscriptions.");
            return Results.Problem(
                "O usuário do banco do painel só tem permissão de leitura. Para gerenciar planos, rode no Postgres: " +
                "GRANT INSERT, UPDATE ON subscriptions TO <usuário do painel>; (veja \"Painel de super admin\" no README).",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
