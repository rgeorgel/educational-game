using Npgsql;

namespace EduGame.Admin;

/// <summary>
/// Endpoints de leitura do painel. Todo o SQL é próprio do admin (não usa código do jogo)
/// e os dias são contados no fuso configurado (Admin:TimeZone, padrão America/Sao_Paulo).
/// </summary>
public static class Metrics
{
    public static void MapMetrics(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.MapGet("/overview", Overview);
        api.MapGet("/series", Series);
        api.MapGet("/users", Users);
        api.MapGet("/users/{id:guid}", UserDetail);
        api.MapGet("/ranking", Ranking);
        api.MapGet("/insights", Insights);
        api.MapGet("/origins", Origins);
    }

    private static string Tz(IConfiguration c) => c["Admin:TimeZone"] is { Length: > 0 } tz ? tz : "America/Sao_Paulo";

    // Limites do dia local como timestamptz (aproveitam o índice de created_at).
    private const string Bounds = """
        WITH t AS (SELECT (now() AT TIME ZONE @tz)::date AS today),
        b AS (
          SELECT today,
                 today::timestamp AT TIME ZONE @tz              AS d1,
                 (today - 6)::timestamp AT TIME ZONE @tz        AS d7,
                 (today - 13)::timestamp AT TIME ZONE @tz       AS d14,
                 (today - 29)::timestamp AT TIME ZONE @tz       AS d30
          FROM t)
        """;

    private static async Task<IResult> Overview(NpgsqlDataSource ds, IConfiguration config, CancellationToken ct)
    {
        var sql = Bounds + """
            SELECT
              b.today,
              (SELECT count(*) FROM users WHERE NOT is_guest)                                         AS accounts,
              (SELECT count(*) FROM users WHERE is_guest)                                             AS guests,
              (SELECT count(*) FROM player_profiles)                                                  AS profiles,
              (SELECT count(*) FROM users WHERE NOT is_guest AND consent_accepted_at > created_at + interval '1 minute') AS converted,
              (SELECT count(*) FROM users WHERE created_at >= b.d1)                                    AS new_today,
              (SELECT count(*) FROM users WHERE created_at >= b.d7 AND NOT is_guest)                   AS new_accounts_7d,
              (SELECT count(*) FROM users WHERE created_at >= b.d7 AND is_guest)                       AS new_guests_7d,
              (SELECT count(DISTINCT user_id) FROM daily_visits WHERE date = b.today)                  AS active_today,
              (SELECT count(DISTINCT user_id) FROM daily_visits WHERE date > b.today - 7)              AS active_7d,
              (SELECT count(DISTINCT user_id) FROM daily_visits WHERE date > b.today - 14 AND date <= b.today - 7) AS active_prev_7d,
              (SELECT count(DISTINCT user_id) FROM daily_visits WHERE date > b.today - 30)             AS active_30d,
              (SELECT coalesce(sum(visits), 0) FROM daily_visits WHERE date = b.today)                 AS visits_today,
              (SELECT coalesce(sum(visits), 0) FROM daily_visits WHERE date > b.today - 7)             AS visits_7d,
              (SELECT count(*) FROM attempts WHERE created_at >= b.d1)                                 AS answers_today,
              (SELECT count(*) FROM attempts WHERE created_at >= b.d7)                                 AS answers_7d,
              (SELECT count(*) FROM attempts WHERE created_at >= b.d14 AND created_at < b.d7)          AS answers_prev_7d,
              (SELECT count(*) FROM attempts WHERE created_at >= b.d30)                                AS answers_30d,
              (SELECT count(*) FROM attempts)                                                          AS answers_total,
              (SELECT count(*) FILTER (WHERE is_correct) FROM attempts WHERE created_at >= b.d7)        AS correct_7d,
              (SELECT avg(time_ms) FILTER (WHERE time_ms BETWEEN 0 AND 600000) FROM attempts WHERE created_at >= b.d7) AS avg_ms_7d,
              (SELECT count(*) FILTER (WHERE hints_used > 0) FROM attempts WHERE created_at >= b.d7)   AS with_hints_7d,
              (SELECT count(DISTINCT profile_id) FROM daily_activity WHERE problems_solved > 0 AND date = b.today)    AS players_today,
              (SELECT count(DISTINCT profile_id) FROM daily_activity WHERE problems_solved > 0 AND date > b.today - 7) AS players_7d,
              (SELECT count(*) FROM game_sessions WHERE started_at >= b.d7)                            AS sessions_7d,
              (SELECT count(*) FROM game_sessions WHERE started_at >= b.d7 AND finished_at IS NOT NULL) AS finished_7d,
              (SELECT count(*) FROM classrooms)                                                        AS classrooms,
              (SELECT count(*) FROM classroom_members)                                                 AS classroom_members,
              (SELECT count(*) FROM classroom_assignments)                                             AS assignments,
              (SELECT count(DISTINCT user_id) FROM subscriptions
                WHERE starts_at <= now() AND (ends_at IS NULL OR ends_at > now()))                     AS paying,
              (SELECT min(date) FROM daily_visits)                                                     AS visits_since
            FROM b
            """;
        var row = await ds.SingleAsync(sql, r => new
        {
            today = r.Date("today"),
            accounts = r.Long("accounts"),
            guests = r.Long("guests"),
            profiles = r.Long("profiles"),
            converted = r.Long("converted"),
            newToday = r.Long("new_today"),
            newAccounts7d = r.Long("new_accounts_7d"),
            newGuests7d = r.Long("new_guests_7d"),
            activeToday = r.Long("active_today"),
            active7d = r.Long("active_7d"),
            activePrev7d = r.Long("active_prev_7d"),
            active30d = r.Long("active_30d"),
            visitsToday = r.Long("visits_today"),
            visits7d = r.Long("visits_7d"),
            answersToday = r.Long("answers_today"),
            answers7d = r.Long("answers_7d"),
            answersPrev7d = r.Long("answers_prev_7d"),
            answers30d = r.Long("answers_30d"),
            answersTotal = r.Long("answers_total"),
            correct7d = r.Long("correct_7d"),
            avgSeconds7d = r["avg_ms_7d"] is DBNull ? (double?)null : Math.Round(Convert.ToDouble(r["avg_ms_7d"]) / 1000, 1),
            withHints7d = r.Long("with_hints_7d"),
            playersToday = r.Long("players_today"),
            players7d = r.Long("players_7d"),
            sessions7d = r.Long("sessions_7d"),
            finished7d = r.Long("finished_7d"),
            classrooms = r.Long("classrooms"),
            classroomMembers = r.Long("classroom_members"),
            assignments = r.Long("assignments"),
            paying = r.Long("paying"),
            visitsSince = r["visits_since"] is DBNull ? (DateOnly?)null : r.Date("visits_since"),
        }, ct, ("tz", Tz(config)));

        // Planos ativos por plano e origem (escolha no site, manual ou Stripe).
        var plans = await ds.ListAsync("""
            SELECT plan, source, count(DISTINCT user_id) AS users
            FROM subscriptions WHERE starts_at <= now() AND (ends_at IS NULL OR ends_at > now())
            GROUP BY plan, source ORDER BY users DESC, plan
            """, r => new { plan = r.Str("plan"), source = r.Str("source"), users = r.Long("users") }, ct);

        return Results.Ok(new { overview = row, plans });
    }

    private static async Task<IResult> Series(NpgsqlDataSource ds, IConfiguration config, CancellationToken ct, int days = 30, int weeks = 12)
    {
        days = Math.Clamp(days, 7, 180);
        weeks = Math.Clamp(weeks, 4, 52);
        var tz = Tz(config);

        const string daily = """
            WITH t AS (SELECT (now() AT TIME ZONE @tz)::date AS today),
            r AS (SELECT today - (@days - 1) AS first, ((today - (@days - 1))::timestamp AT TIME ZONE @tz) AS since FROM t),
            days AS (SELECT generate_series(r.first, t.today, interval '1 day')::date AS day FROM t, r),
            ans AS (SELECT (created_at AT TIME ZONE @tz)::date AS day, count(*) AS n, count(*) FILTER (WHERE is_correct) AS c
                    FROM attempts WHERE created_at >= (SELECT since FROM r) GROUP BY 1),
            vis AS (SELECT date AS day, count(DISTINCT user_id) AS u, sum(visits) AS v
                    FROM daily_visits WHERE date >= (SELECT first FROM r) GROUP BY 1),
            ply AS (SELECT date AS day, count(DISTINCT profile_id) AS p
                    FROM daily_activity WHERE problems_solved > 0 AND date >= (SELECT first FROM r) GROUP BY 1),
            sig AS (SELECT (created_at AT TIME ZONE @tz)::date AS day,
                           count(*) FILTER (WHERE NOT is_guest) AS a, count(*) FILTER (WHERE is_guest) AS g
                    FROM users WHERE created_at >= (SELECT since FROM r) GROUP BY 1)
            SELECT d.day, coalesce(ans.n, 0) AS answers, coalesce(ans.c, 0) AS correct,
                   coalesce(vis.u, 0) AS active_users, coalesce(vis.v, 0) AS visits, coalesce(ply.p, 0) AS players,
                   coalesce(sig.a, 0) AS new_accounts, coalesce(sig.g, 0) AS new_guests
            FROM days d
            LEFT JOIN ans USING (day) LEFT JOIN vis USING (day) LEFT JOIN ply USING (day) LEFT JOIN sig USING (day)
            ORDER BY d.day
            """;
        var dailyRows = await ds.ListAsync(daily, r => new
        {
            date = r.Date("day"),
            answers = r.Long("answers"),
            correct = r.Long("correct"),
            activeUsers = r.Long("active_users"),
            visits = r.Long("visits"),
            players = r.Long("players"),
            newAccounts = r.Long("new_accounts"),
            newGuests = r.Long("new_guests"),
        }, ct, ("tz", tz), ("days", days));

        // Semanas começam na segunda-feira.
        const string weekly = """
            WITH t AS (SELECT date_trunc('week', (now() AT TIME ZONE @tz))::date AS wk),
            r AS (SELECT wk - 7 * (@weeks - 1) AS first, ((wk - 7 * (@weeks - 1))::timestamp AT TIME ZONE @tz) AS since FROM t),
            weeks AS (SELECT generate_series(r.first, t.wk, interval '7 day')::date AS week FROM t, r),
            ans AS (SELECT date_trunc('week', created_at AT TIME ZONE @tz)::date AS week, count(*) AS n, count(*) FILTER (WHERE is_correct) AS c
                    FROM attempts WHERE created_at >= (SELECT since FROM r) GROUP BY 1),
            vis AS (SELECT date_trunc('week', date::timestamp)::date AS week, count(DISTINCT user_id) AS u, sum(visits) AS v
                    FROM daily_visits WHERE date >= (SELECT first FROM r) GROUP BY 1),
            ply AS (SELECT date_trunc('week', date::timestamp)::date AS week, count(DISTINCT profile_id) AS p
                    FROM daily_activity WHERE problems_solved > 0 AND date >= (SELECT first FROM r) GROUP BY 1)
            SELECT w.week, coalesce(ans.n, 0) AS answers, coalesce(ans.c, 0) AS correct,
                   coalesce(vis.u, 0) AS active_users, coalesce(vis.v, 0) AS visits, coalesce(ply.p, 0) AS players
            FROM weeks w LEFT JOIN ans USING (week) LEFT JOIN vis USING (week) LEFT JOIN ply USING (week)
            ORDER BY w.week
            """;
        var weeklyRows = await ds.ListAsync(weekly, r => new
        {
            weekStart = r.Date("week"),
            answers = r.Long("answers"),
            correct = r.Long("correct"),
            activeUsers = r.Long("active_users"),
            visits = r.Long("visits"),
            players = r.Long("players"),
        }, ct, ("tz", tz), ("weeks", weeks));

        // Em que horário as pessoas respondem (últimos 30 dias, hora local).
        const string hours = """
            SELECT h AS hour, coalesce(n, 0) AS answers
            FROM generate_series(0, 23) AS h
            LEFT JOIN (SELECT extract(hour FROM created_at AT TIME ZONE @tz)::int AS hr, count(*) AS n
                       FROM attempts WHERE created_at >= now() - interval '30 days' GROUP BY 1) x ON x.hr = h
            ORDER BY h
            """;
        var hourRows = await ds.ListAsync(hours, r => new { hour = r.Int("hour"), answers = r.Long("answers") }, ct, ("tz", tz));

        return Results.Ok(new { daily = dailyRows, weekly = weeklyRows, hours = hourRows });
    }

    private static readonly Dictionary<string, string> UserSorts = new()
    {
        ["recentes"] = "u.created_at DESC",
        ["ultimo-acesso"] = "u.last_seen_at DESC NULLS LAST",
        ["respostas"] = "solved DESC, u.created_at DESC",
        ["xp"] = "xp DESC, u.created_at DESC",
    };

    private static async Task<IResult> Users(NpgsqlDataSource ds, IConfiguration config, CancellationToken ct,
        string? q = null, string? type = null, string? sort = null, int page = 1)
    {
        const int pageSize = 25;
        page = Math.Max(1, page);
        type = type is "convidado" or "conta" ? type : "todos";
        var order = UserSorts.GetValueOrDefault(sort ?? "", UserSorts["recentes"]);
        var search = (q ?? "").Trim();
        var like = "%" + search.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";

        var sql = $"""
            SELECT u.id, u.email, u.is_guest, u.created_at, u.last_seen_at, u.signup_source,
                   (NOT u.is_guest AND u.consent_accepted_at > u.created_at + interval '1 minute') AS converted,
                   coalesce(p.names, '') AS nicknames, coalesce(p.avatars, '') AS avatars, coalesce(p.n, 0) AS profiles,
                   coalesce(a.solved, 0) AS solved, coalesce(a.correct, 0) AS correct, coalesce(a.xp, 0) AS xp, a.last_day,
                   coalesce(v.visits, 0) AS visits_30d, coalesce(v.days, 0) AS days_30d,
                   count(*) OVER () AS total
            FROM users u
            LEFT JOIN LATERAL (SELECT string_agg(nickname, ', ' ORDER BY created_at) AS names,
                                      string_agg(avatar, ' ' ORDER BY created_at) AS avatars, count(*) AS n
                               FROM player_profiles WHERE user_id = u.id) p ON true
            LEFT JOIN LATERAL (SELECT sum(da.problems_solved) AS solved, sum(da.correct) AS correct, sum(da.xp) AS xp, max(da.date) AS last_day
                               FROM daily_activity da JOIN player_profiles pp ON pp.id = da.profile_id
                               WHERE pp.user_id = u.id) a ON true
            LEFT JOIN LATERAL (SELECT sum(visits) AS visits, count(*) AS days FROM daily_visits
                               WHERE user_id = u.id AND date > (now() AT TIME ZONE @tz)::date - 30) v ON true
            WHERE (@type = 'todos' OR (@type = 'convidado' AND u.is_guest) OR (@type = 'conta' AND NOT u.is_guest))
              AND (@q = '' OR u.email ILIKE @like
                   OR EXISTS (SELECT 1 FROM player_profiles pp WHERE pp.user_id = u.id AND pp.nickname ILIKE @like))
            ORDER BY {order}
            LIMIT @limit OFFSET @offset
            """;
        long total = 0;
        var rows = await ds.ListAsync(sql, r =>
        {
            total = r.Long("total");
            return new
            {
                id = r.GetGuid(r.GetOrdinal("id")),
                email = r.Str("email"),
                isGuest = r.GetBoolean(r.GetOrdinal("is_guest")),
                converted = r.GetBoolean(r.GetOrdinal("converted")),
                createdAt = r.Time("created_at"),
                lastSeenAt = r.Time("last_seen_at"),
                source = r.Str("signup_source"),
                nicknames = r.Str("nicknames"),
                avatars = r.Str("avatars"),
                profiles = r.Long("profiles"),
                solved = r.Long("solved"),
                correct = r.Long("correct"),
                xp = r.Long("xp"),
                lastPlayed = r["last_day"] is DBNull ? (DateOnly?)null : r.Date("last_day"),
                visits30d = r.Long("visits_30d"),
                days30d = r.Long("days_30d"),
            };
        }, ct, ("tz", Tz(config)), ("type", type), ("q", search), ("like", like), ("limit", pageSize), ("offset", (page - 1) * pageSize));

        return Results.Ok(new { page, pageSize, total, items = rows });
    }

    private static async Task<IResult> UserDetail(Guid id, NpgsqlDataSource ds, IConfiguration config, CancellationToken ct)
    {
        var tz = Tz(config);
        var user = await ds.SingleAsync("""
            SELECT u.id, u.email, u.is_guest, u.created_at, u.last_seen_at, u.consent_accepted_at,
                   u.signup_source, u.signup_medium, u.signup_campaign, u.signup_referrer,
                   (NOT u.is_guest AND u.consent_accepted_at > u.created_at + interval '1 minute') AS converted,
                   (SELECT coalesce(sum(visits), 0) FROM daily_visits WHERE user_id = u.id) AS visits_total,
                   (SELECT count(*) FROM daily_visits WHERE user_id = u.id AND date > (now() AT TIME ZONE @tz)::date - 30) AS days_30d,
                   (SELECT count(*) FROM classrooms WHERE owner_user_id = u.id) AS classrooms_owned
            FROM users u WHERE u.id = @id
            """, r => new
        {
            id = r.GetGuid(r.GetOrdinal("id")),
            email = r.Str("email"),
            isGuest = r.GetBoolean(r.GetOrdinal("is_guest")),
            converted = r.GetBoolean(r.GetOrdinal("converted")),
            createdAt = r.Time("created_at"),
            lastSeenAt = r.Time("last_seen_at"),
            consentAcceptedAt = r.Time("consent_accepted_at"),
            source = r.Str("signup_source"),
            medium = r.Str("signup_medium"),
            campaign = r.Str("signup_campaign"),
            referrer = r.Str("signup_referrer"),
            visitsTotal = r.Long("visits_total"),
            days30d = r.Long("days_30d"),
            classroomsOwned = r.Long("classrooms_owned"),
        }, ct, ("id", id), ("tz", tz));
        if (user is null) return Results.NotFound();

        var profiles = await ds.ListAsync("""
            SELECT p.id, p.nickname, p.avatar, p.school_grade, p.birth_year, p.created_at,
                   coalesce(a.solved, 0) AS solved, coalesce(a.correct, 0) AS correct, coalesce(a.xp, 0) AS xp,
                   coalesce(a.days, 0) AS active_days, a.last_day,
                   (SELECT count(*) FROM profile_achievements pa WHERE pa.profile_id = p.id) AS medals,
                   (SELECT count(*) FROM classroom_members cm WHERE cm.profile_id = p.id) AS classrooms,
                   (SELECT string_agg(s.icon || ' ' || s.name || ': ' || l.icon || ' ' || l.name || ' (' || sp.xp || ' XP)', ' · ' ORDER BY s."order")
                      FROM subject_progress sp JOIN subjects s ON s.id = sp.subject_id JOIN levels l ON l.id = sp.current_level_id
                     WHERE sp.profile_id = p.id) AS levels
            FROM player_profiles p
            LEFT JOIN LATERAL (SELECT sum(problems_solved) AS solved, sum(correct) AS correct, sum(xp) AS xp,
                                      count(*) FILTER (WHERE problems_solved > 0) AS days, max(date) AS last_day
                               FROM daily_activity WHERE profile_id = p.id) a ON true
            WHERE p.user_id = @id
            ORDER BY p.created_at
            """, r => new
        {
            id = r.GetGuid(r.GetOrdinal("id")),
            nickname = r.Str("nickname"),
            avatar = r.Str("avatar"),
            schoolGrade = r["school_grade"] is DBNull ? (int?)null : r.Int("school_grade"),
            birthYear = r["birth_year"] is DBNull ? (int?)null : r.Int("birth_year"),
            createdAt = r.Time("created_at"),
            solved = r.Long("solved"),
            correct = r.Long("correct"),
            xp = r.Long("xp"),
            activeDays = r.Long("active_days"),
            lastPlayed = r["last_day"] is DBNull ? (DateOnly?)null : r.Date("last_day"),
            medals = r.Long("medals"),
            classrooms = r.Long("classrooms"),
            levels = r.Str("levels"),
        }, ct, ("id", id));

        var activity = await ds.ListAsync("""
            WITH t AS (SELECT (now() AT TIME ZONE @tz)::date AS today)
            SELECT d::date AS day, coalesce(x.solved, 0) AS solved, coalesce(x.correct, 0) AS correct, coalesce(v.visits, 0) AS visits
            FROM t, generate_series(t.today - 29, t.today, interval '1 day') AS d
            LEFT JOIN (SELECT da.date, sum(da.problems_solved) AS solved, sum(da.correct) AS correct
                       FROM daily_activity da JOIN player_profiles p ON p.id = da.profile_id
                       WHERE p.user_id = @id GROUP BY da.date) x ON x.date = d::date
            LEFT JOIN daily_visits v ON v.user_id = @id AND v.date = d::date
            ORDER BY 1
            """, r => new { date = r.Date("day"), solved = r.Long("solved"), correct = r.Long("correct"), visits = r.Long("visits") },
            ct, ("id", id), ("tz", tz));

        var sessions = await ds.ListAsync("""
            SELECT gs.started_at, gs.finished_at, gs.mode, gs.problem_count, gs.correct_count, gs.xp_earned, gs.stars,
                   p.nickname, s.icon AS subject_icon, s.name AS subject, st.name AS stage
            FROM game_sessions gs
            JOIN player_profiles p ON p.id = gs.profile_id
            JOIN subjects s ON s.id = gs.subject_id
            LEFT JOIN stages st ON st.id = gs.stage_id
            WHERE p.user_id = @id
            ORDER BY gs.started_at DESC
            LIMIT 15
            """, r => new
        {
            startedAt = r.Time("started_at"),
            finished = r["finished_at"] is not DBNull,
            mode = r.Str("mode"),
            problems = r.Int("problem_count"),
            correct = r.Int("correct_count"),
            xp = r.Int("xp_earned"),
            stars = r["stars"] is DBNull ? (int?)null : r.Int("stars"),
            nickname = r.Str("nickname"),
            subject = $"{r.Str("subject_icon")} {r.Str("subject")}",
            stage = r.Str("stage"),
        }, ct, ("id", id));

        var subscriptions = await ds.ListAsync("""
            SELECT plan, starts_at, ends_at, note, (starts_at <= now() AND (ends_at IS NULL OR ends_at > now())) AS active
            FROM subscriptions WHERE user_id = @id ORDER BY starts_at DESC
            """, r => new
        {
            plan = r.Str("plan"),
            startsAt = r.Time("starts_at"),
            endsAt = r.Time("ends_at"),
            note = r.Str("note"),
            active = r.GetBoolean(r.GetOrdinal("active")),
        }, ct, ("id", id));

        return Results.Ok(new { user, profiles, activity, sessions, subscriptions });
    }

    /// <summary>Canais de divulgação: quem chegou por cada origem e quantos jogaram e voltaram.</summary>
    private static async Task<IResult> Origins(NpgsqlDataSource ds, IConfiguration config, CancellationToken ct, int days = 30)
    {
        days = Math.Clamp(days, 1, 365);
        const string cte = """
            WITH t AS (SELECT (((now() AT TIME ZONE @tz)::date - (@days - 1))::timestamp AT TIME ZONE @tz) AS since),
            u AS (
              SELECT u.id, u.is_guest, coalesce(u.signup_source, '(sem registro)') AS source,
                     u.signup_medium AS medium, u.signup_campaign AS campaign,
                     EXISTS (SELECT 1 FROM daily_activity da JOIN player_profiles p ON p.id = da.profile_id
                             WHERE p.user_id = u.id AND da.problems_solved > 0) AS played,
                     (SELECT count(*) FROM daily_visits v WHERE v.user_id = u.id) >= 2 AS returned
              FROM users u, t WHERE u.created_at >= t.since)
            """;
        var sources = await ds.ListAsync(cte + """
            SELECT source, count(*) AS users, count(*) FILTER (WHERE NOT is_guest) AS accounts, count(*) FILTER (WHERE is_guest) AS guests,
                   count(*) FILTER (WHERE played) AS played, count(*) FILTER (WHERE returned) AS returned
            FROM u GROUP BY source ORDER BY users DESC, source
            """, r => new
        {
            source = r.Str("source"),
            users = r.Long("users"),
            accounts = r.Long("accounts"),
            guests = r.Long("guests"),
            played = r.Long("played"),
            returned = r.Long("returned"),
        }, ct, ("tz", Tz(config)), ("days", days));

        var campaigns = await ds.ListAsync(cte + """
            SELECT source, coalesce(medium, '') AS medium, coalesce(campaign, '') AS campaign, count(*) AS users,
                   count(*) FILTER (WHERE played) AS played, count(*) FILTER (WHERE returned) AS returned
            FROM u WHERE medium IS NOT NULL OR campaign IS NOT NULL
            GROUP BY source, medium, campaign ORDER BY users DESC LIMIT 20
            """, r => new
        {
            source = r.Str("source"),
            medium = r.Str("medium"),
            campaign = r.Str("campaign"),
            users = r.Long("users"),
            played = r.Long("played"),
            returned = r.Long("returned"),
        }, ct, ("tz", Tz(config)), ("days", days));

        return Results.Ok(new { days, sources, campaigns });
    }

    private static readonly Dictionary<string, string> RankingOrders = new()
    {
        ["xp"] = "agg.xp DESC",
        ["respostas"] = "agg.solved DESC",
        ["acertos"] = "(agg.correct::float / agg.solved) DESC",
        ["dias"] = "agg.active_days DESC",
    };

    private static async Task<IResult> Ranking(NpgsqlDataSource ds, IConfiguration config, CancellationToken ct,
        string? by = null, int days = 0, bool guests = true)
    {
        by = by is not null && RankingOrders.ContainsKey(by) ? by : "xp";
        days = days is 7 or 30 or 90 ? days : 0;
        const int minForAccuracy = 20;

        var sql = $"""
            WITH agg AS (
              SELECT profile_id, sum(problems_solved) AS solved, sum(correct) AS correct, sum(xp) AS xp,
                     count(*) FILTER (WHERE problems_solved > 0) AS active_days, max(date) AS last_day
              FROM daily_activity
              WHERE @days = 0 OR date > (now() AT TIME ZONE @tz)::date - @days
              GROUP BY profile_id)
            SELECT p.id, p.nickname, p.avatar, u.id AS user_id, u.is_guest, u.email,
                   agg.solved, agg.correct, agg.xp, agg.active_days, agg.last_day,
                   (SELECT count(*) FROM profile_achievements pa WHERE pa.profile_id = p.id) AS medals,
                   (SELECT string_agg(s.icon || ' ' || l.name, ' · ' ORDER BY s."order")
                      FROM subject_progress sp JOIN subjects s ON s.id = sp.subject_id JOIN levels l ON l.id = sp.current_level_id
                     WHERE sp.profile_id = p.id) AS levels
            FROM agg
            JOIN player_profiles p ON p.id = agg.profile_id
            JOIN users u ON u.id = p.user_id
            WHERE agg.solved > 0
              AND (@guests OR NOT u.is_guest)
              AND (@by <> 'acertos' OR agg.solved >= @min)
            ORDER BY {RankingOrders[by]}, agg.solved DESC, p.nickname
            LIMIT 50
            """;
        var rows = await ds.ListAsync(sql, r => new
        {
            profileId = r.GetGuid(r.GetOrdinal("id")),
            userId = r.GetGuid(r.GetOrdinal("user_id")),
            nickname = r.Str("nickname"),
            avatar = r.Str("avatar"),
            isGuest = r.GetBoolean(r.GetOrdinal("is_guest")),
            email = r.Str("email"),
            solved = r.Long("solved"),
            correct = r.Long("correct"),
            xp = r.Long("xp"),
            activeDays = r.Long("active_days"),
            lastPlayed = r.Date("last_day"),
            medals = r.Long("medals"),
            levels = r.Str("levels"),
        }, ct, ("tz", Tz(config)), ("days", days), ("guests", guests), ("by", by), ("min", minForAccuracy));

        return Results.Ok(new { by, days, minForAccuracy, items = rows });
    }

    private static async Task<IResult> Insights(NpgsqlDataSource ds, CancellationToken ct, int days = 30)
    {
        days = days is 7 or 30 or 90 or 365 ? days : 30;

        var skills = await ds.ListAsync("""
            SELECT s.id, s.name, sub.icon || ' ' || sub.name AS subject, l.name AS level, s.bncc_codes,
                   count(*) AS n, count(*) FILTER (WHERE a.is_correct) AS c,
                   avg(a.hints_used) AS hints,
                   percentile_cont(0.5) WITHIN GROUP (ORDER BY a.time_ms) / 1000.0 AS median_s
            FROM attempts a
            JOIN session_problems sp ON sp.id = a.session_problem_id
            JOIN skills s ON s.id = sp.skill_id
            JOIN subjects sub ON sub.id = s.subject_id
            JOIN levels l ON l.id = s.level_id
            WHERE a.created_at >= now() - make_interval(days => @days)
            GROUP BY s.id, s.name, s.bncc_codes, sub.icon, sub.name, l.name, sub."order", l."order", s."order"
            ORDER BY sub."order", l."order", s."order"
            """, r => new
        {
            id = r.Int("id"),
            name = r.Str("name"),
            subject = r.Str("subject"),
            level = r.Str("level"),
            bncc = r["bncc_codes"] as string[] ?? [],
            answers = r.Long("n"),
            correct = r.Long("c"),
            avgHints = r.Dbl("hints"),
            medianSeconds = r.Dbl("median_s"),
        }, ct, ("days", days));

        var stages = await ds.ListAsync("""
            SELECT st.id, st.name, rg.icon || ' ' || rg.name AS region, sub.icon AS subject_icon, l.name AS level,
                   count(*) AS sessions, count(*) FILTER (WHERE gs.finished_at IS NOT NULL) AS finished,
                   avg(gs.stars) FILTER (WHERE gs.finished_at IS NOT NULL) AS avg_stars,
                   count(DISTINCT gs.profile_id) AS players
            FROM game_sessions gs
            JOIN stages st ON st.id = gs.stage_id
            JOIN regions rg ON rg.id = st.region_id
            JOIN subjects sub ON sub.id = rg.subject_id
            JOIN levels l ON l.id = rg.level_id
            WHERE gs.started_at >= now() - make_interval(days => @days)
            GROUP BY st.id, st.name, rg.icon, rg.name, sub.icon, l.name
            ORDER BY sessions DESC
            LIMIT 15
            """, r => new
        {
            id = r.Int("id"),
            name = r.Str("name"),
            region = r.Str("region"),
            subjectIcon = r.Str("subject_icon"),
            level = r.Str("level"),
            sessions = r.Long("sessions"),
            finished = r.Long("finished"),
            avgStars = r.Dbl("avg_stars"),
            players = r.Long("players"),
        }, ct, ("days", days));

        var levels = await ds.ListAsync("""
            SELECT sub.icon || ' ' || sub.name AS subject, l.name AS level, l."order" AS ord, count(sp.profile_id) AS profiles
            FROM levels l
            JOIN subjects sub ON sub.id = l.subject_id
            LEFT JOIN subject_progress sp ON sp.current_level_id = l.id
            GROUP BY sub.icon, sub.name, sub."order", l.name, l."order"
            ORDER BY sub."order", l."order"
            """, r => new { subject = r.Str("subject"), level = r.Str("level"), order = r.Int("ord"), profiles = r.Long("profiles") }, ct);

        var modes = await ds.ListAsync("""
            SELECT mode, count(*) AS sessions, count(*) FILTER (WHERE finished_at IS NOT NULL) AS finished,
                   coalesce(sum(correct_count) FILTER (WHERE finished_at IS NOT NULL), 0) AS correct,
                   coalesce(sum(problem_count) FILTER (WHERE finished_at IS NOT NULL), 0) AS problems,
                   count(DISTINCT profile_id) AS players
            FROM game_sessions
            WHERE started_at >= now() - make_interval(days => @days)
            GROUP BY mode
            ORDER BY sessions DESC
            """, r => new
        {
            mode = r.Str("mode"),
            sessions = r.Long("sessions"),
            finished = r.Long("finished"),
            correct = r.Long("correct"),
            problems = r.Long("problems"),
            players = r.Long("players"),
        }, ct, ("days", days));

        var medals = await ds.ListAsync("""
            SELECT slug, count(*) AS n, max(earned_at) AS last_at FROM profile_achievements GROUP BY slug ORDER BY n DESC, slug
            """, r => new { slug = r.Str("slug"), profiles = r.Long("n"), lastAt = r.Time("last_at") }, ct);

        return Results.Ok(new { days, skills, stages, levels, modes, medals });
    }
}
