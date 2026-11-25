using EduGame.Api.Common;
using EduGame.Api.Features.Billing;
using EduGame.Api.Features.Progress;
using EduGame.Api.Features.Sessions;
using EduGame.Domain.Players;
using EduGame.Domain.Progression;
using EduGame.Infrastructure;
using EduGame.Problems;
using Microsoft.EntityFrameworkCore;
using static EduGame.Problems.Lang;

namespace EduGame.Api.Features.Family;

/// <summary>
/// Recursos para responsáveis e professores:
/// - relatório da semana (plano Família);
/// - treino de reforço: o responsável escolhe uma habilidade e ela aparece para a criança (plano Família);
/// - fichas para imprimir com gabarito (planos Família e Professor Pro).
/// </summary>
public static class FamilyEndpoints
{
    public sealed record WeekStats(int Problems, int Correct, int Xp, int ActiveDays, int Minutes);
    public sealed record DayStats(DateOnly Date, int Problems, int Correct, int Xp);
    public sealed record SkillRef(int Id, string Slug, string Name, string SubjectSlug, string SubjectName, string SubjectIcon, string LevelName, IReadOnlyList<BnccRefDto> Bncc);
    public sealed record WeekSkill(SkillRef Skill, int Attempts, int Correct, int RatingChange, string Status);
    public sealed record WeekMedal(string Name, string Icon, DateTimeOffset EarnedAt);
    public sealed record GoalDto(Guid Id, SkillRef Skill, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, int Attempts);
    public sealed record WeeklyReport(
        Guid ProfileId, string Nickname, string Avatar, DateOnly From, DateOnly To,
        WeekStats ThisWeek, WeekStats LastWeek, IReadOnlyList<DayStats> Days,
        IReadOnlyList<WeekSkill> Skills, IReadOnlyList<SkillRef> Attention,
        IReadOnlyList<WeekMedal> Medals, IReadOnlyList<GoalDto> Goals, IReadOnlyList<string> Tips);
    public sealed record CreateGoalRequest(string SkillSlug, string? SubjectSlug = null);

    public sealed record WorksheetProblem(int Number, string Type, string Prompt, string? Visual, IReadOnlyList<string>? Choices,
        IReadOnlyList<string?>? Sequence, GridPuzzle? Grid, string? Code);
    public sealed record WorksheetAnswer(int Number, string Answer, string Explanation);
    public sealed record Worksheet(SkillRef Skill, string Description, string Difficulty, int Seed,
        IReadOnlyList<WorksheetProblem> Problems, IReadOnlyList<WorksheetAnswer> Answers);

    /// <summary>Tipos que funcionam no papel (pintar frações fica de fora).</summary>
    private static readonly HashSet<string> PrintableTypes =
        [ProblemTypes.Numero, ProblemTypes.MultiplaEscolha, ProblemTypes.CompletarSequencia, ProblemTypes.VerdadeiroFalso, ProblemTypes.Ordenar, ProblemTypes.Grade];

    public static void MapFamily(this RouteGroupBuilder api)
    {
        var profiles = api.MapGroup("/profiles/{profileId:guid}").WithTags("Family").RequireAuthorization();

        // ---------- Relatório da semana ----------
        profiles.MapGet("/weekly-report", async (Guid profileId, HttpContext http, AppDbContext db, PlanService plans, GameClock clock, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));
            var plan = await plans.ForUserAsync(http.User.UserId(), ct);
            if (!plan.WeeklyReport) return PlanService.Locked(Tr("O relatório da semana", "The weekly report", "El informe de la semana"), Domain.Billing.Plans.FamilySlug);
            return Results.Ok(await BuildWeeklyAsync(db, profile, clock, ct));
        });

        // ---------- Treino de reforço ----------
        profiles.MapGet("/goals", async (Guid profileId, HttpContext http, AppDbContext db, GameClock clock, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));
            return Results.Ok(await GoalsAsync(db, profileId, clock.Now.AddDays(-7), ct));
        });

        profiles.MapPost("/goals", async (Guid profileId, CreateGoalRequest req, HttpContext http, AppDbContext db, PlanService plans, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));
            var plan = await plans.ForUserAsync(http.User.UserId(), ct);
            if (!plan.Reinforcement) return PlanService.Locked(Tr("O treino de reforço", "Extra practice", "El refuerzo"), Domain.Billing.Plans.FamilySlug);

            var skill = await db.Skills.AsNoTracking()
                .Where(s => s.Slug == req.SkillSlug && (req.SubjectSlug == null || s.Subject!.Slug == req.SubjectSlug) && s.Templates.Any(t => t.Active))
                .FirstOrDefaultAsync(ct);
            if (skill is null) return ApiResults.Validation("skillSlug", Tr("Habilidade não encontrada.", "Skill not found.", "Habilidad no encontrada."));

            var active = await db.PracticeGoals.Where(g => g.ProfileId == profileId && g.CompletedAt == null).ToListAsync(ct);
            if (active.FirstOrDefault(g => g.SkillId == skill.Id) is { } existing) return Results.Ok(new { existing.Id });
            if (active.Count >= PracticeGoal.MaxActivePerProfile)
                return ApiResults.Conflict(Tr($"Cada perfil pode ter até {PracticeGoal.MaxActivePerProfile} treinos de reforço em aberto. Apague um ou espere concluir.", $"Each profile can have up to {PracticeGoal.MaxActivePerProfile} open extra practices. Delete one or wait for one to finish.", $"Cada perfil puede tener hasta {PracticeGoal.MaxActivePerProfile} refuerzos abiertos. Borra uno o espera a que termine."));

            var goal = new PracticeGoal { ProfileId = profileId, SkillId = skill.Id };
            db.PracticeGoals.Add(goal);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/profiles/{profileId}/goals/{goal.Id}", new { goal.Id });
        });

        profiles.MapDelete("/goals/{goalId:guid}", async (Guid profileId, Guid goalId, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));
            await db.PracticeGoals.Where(g => g.Id == goalId && g.ProfileId == profileId).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });

        // A criança joga o treino (vale mesmo que a habilidade seja de outro nível).
        profiles.MapPost("/goals/{goalId:guid}/start", async (Guid profileId, Guid goalId, HttpContext http, AppDbContext db, SessionService sessions, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound(Tr("Perfil não encontrado.", "Profile not found.", "Perfil no encontrado."));
            var goal = await db.PracticeGoals.SingleOrDefaultAsync(g => g.Id == goalId && g.ProfileId == profileId, ct);
            if (goal is null) return ApiResults.NotFound(Tr("Treino não encontrado.", "Practice not found.", "Refuerzo no encontrado."));
            var session = await sessions.StartReinforcementAsync(profile, goal, ct);
            return Results.Created($"/api/sessions/{session.Id}", new { sessionId = session.Id });
        });

        // ---------- Fichas para imprimir ----------
        api.MapGet("/worksheets", async (string skill, string? subject, int? count, string? difficulty, int? seed,
            HttpContext http, AppDbContext db, PlanService plans, CancellationToken ct) =>
        {
            var plan = await plans.ForUserAsync(http.User.UserId(), ct);
            if (!plan.Worksheets) return PlanService.Locked(Tr("As fichas para imprimir", "Printable worksheets", "Las fichas para imprimir"), Domain.Billing.Plans.FamilySlug);

            var row = await db.Skills.AsNoTracking()
                .Where(s => s.Slug == skill && (subject == null || s.Subject!.Slug == subject))
                .Select(s => new { s.Id, s.Slug, s.Name, s.Description, s.BnccCodes, SubjectSlug = s.Subject!.Slug, SubjectName = s.Subject.Name, SubjectIcon = s.Subject.Icon, LevelSlug = s.Level!.Slug, LevelName = s.Level.Name })
                .FirstOrDefaultAsync(ct);
            if (row is null) return ApiResults.Validation("skill", Tr("Habilidade não encontrada.", "Skill not found.", "Habilidad no encontrada."));

            var generators = ProblemFactory.Default.ForSkill(row.Slug).ToList();
            if (generators.Count == 0) return ApiResults.Validation("skill", Tr("Esta habilidade ainda não tem fichas para imprimir.", "This skill doesn't have printable worksheets yet.", "Esta habilidad todavía no tiene fichas para imprimir."));

            var level = LevelProgress.BySlug(row.LevelSlug);
            var (difficultyName, target) = difficulty switch
            {
                "facil" => ("facil", Rating.InitialFor(level)),
                "dificil" => ("dificil", LevelProgress.TargetRating(level)),
                _ => ("medio", (Rating.InitialFor(level) + LevelProgress.TargetRating(level)) / 2),
            };
            var total = Math.Clamp(count ?? 10, 4, 30);
            var baseSeed = seed ?? Random.Shared.Next(1, 1_000_000);

            var problems = new List<WorksheetProblem>();
            var answers = new List<WorksheetAnswer>();
            var seen = new HashSet<string>();
            for (var attempt = 0; problems.Count < total && attempt < total * 6; attempt++)
            {
                var generator = generators[attempt % generators.Count];
                var generated = ProblemFactory.Default.Generate(generator.Key, target, baseSeed + attempt * 7919, seen);
                var c = generated.Content;
                if (!PrintableTypes.Contains(c.Type) || !seen.Add(ProblemFactory.Fingerprint(c))) continue;
                var number = problems.Count + 1;
                problems.Add(new WorksheetProblem(number, c.Type, c.Prompt, c.Visual, c.Choices, c.Sequence, c.Grid, c.Code));
                answers.Add(new WorksheetAnswer(number, c.Answer, c.Explanation));
            }
            if (problems.Count == 0) return ApiResults.Validation("skill", Tr("Esta habilidade é só para jogar na tela (não tem versão para imprimir).", "This skill is screen-only (there's no printable version).", "Esta habilidad es solo para jugar en pantalla (no tiene versión para imprimir)."));

            var skillRef = new SkillRef(row.Id, row.Slug, row.Name, row.SubjectSlug, row.SubjectName, row.SubjectIcon, row.LevelName, BnccRefDto.For(row.BnccCodes));
            return Results.Ok(new Worksheet(skillRef, row.Description, difficultyName, baseSeed, problems, answers));
        }).RequireAuthorization().WithTags("Family");
    }

    private static async Task<List<GoalDto>> GoalsAsync(AppDbContext db, Guid profileId, DateTimeOffset doneSince, CancellationToken ct)
    {
        var goals = await db.PracticeGoals.AsNoTracking()
            .Where(g => g.ProfileId == profileId && (g.CompletedAt == null || g.CompletedAt >= doneSince))
            .OrderBy(g => g.CompletedAt != null).ThenByDescending(g => g.CreatedAt).ToListAsync(ct);
        var skills = await SkillRefsAsync(db, goals.Select(g => g.SkillId), ct);
        return [.. goals.Where(g => skills.ContainsKey(g.SkillId)).Select(g => new GoalDto(g.Id, skills[g.SkillId], g.CreatedAt, g.CompletedAt, g.Attempts))];
    }

    private static async Task<Dictionary<int, SkillRef>> SkillRefsAsync(AppDbContext db, IEnumerable<int> skillIds, CancellationToken ct)
    {
        var ids = skillIds.Distinct().ToList();
        var rows = await db.Skills.AsNoTracking().Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Id, s.Slug, s.Name, s.BnccCodes, SubjectSlug = s.Subject!.Slug, SubjectName = s.Subject.Name, SubjectIcon = s.Subject.Icon, LevelName = s.Level!.Name })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => new SkillRef(r.Id, r.Slug, r.Name, r.SubjectSlug, r.SubjectName, r.SubjectIcon, r.LevelName, BnccRefDto.For(r.BnccCodes)));
    }

    private static async Task<WeeklyReport> BuildWeeklyAsync(AppDbContext db, PlayerProfile profile, GameClock clock, CancellationToken ct)
    {
        var today = clock.Today;
        var from = today.AddDays(-6);
        var prevFrom = from.AddDays(-7);
        var now = clock.Now;
        var weekStart = now.AddDays(-7);
        var prevStart = now.AddDays(-14);

        var days = await db.DailyActivities.AsNoTracking()
            .Where(d => d.ProfileId == profile.Id && d.Date >= prevFrom && d.Date <= today).ToListAsync(ct);

        // Tentativas das duas últimas semanas, com a habilidade e o rating antes/depois.
        var attempts = await db.SessionProblems.AsNoTracking()
            .Where(p => p.Attempt != null && p.Attempt.CreatedAt >= prevStart && db.GameSessions.Any(g => g.Id == p.SessionId && g.ProfileId == profile.Id))
            .Select(p => new { p.SkillId, p.Attempt!.IsCorrect, p.Attempt.TimeMs, p.Attempt.RatingBefore, p.Attempt.RatingAfter, p.Attempt.CreatedAt })
            .ToListAsync(ct);

        WeekStats Stats(DateOnly start, DateOnly end, DateTimeOffset tStart, DateTimeOffset tEnd)
        {
            var d = days.Where(x => x.Date >= start && x.Date <= end).ToList();
            var ms = attempts.Where(a => a.CreatedAt >= tStart && a.CreatedAt < tEnd).Sum(a => (long)Math.Clamp(a.TimeMs, 0, 300_000));
            return new WeekStats(d.Sum(x => x.ProblemsSolved), d.Sum(x => x.Correct), d.Sum(x => x.Xp), d.Count(x => x.ProblemsSolved > 0), (int)Math.Round(ms / 60000.0));
        }
        var thisWeek = Stats(from, today, weekStart, now.AddMinutes(1));
        var lastWeek = Stats(prevFrom, from.AddDays(-1), prevStart, weekStart);

        var dayList = Enumerable.Range(0, 7).Select(i => from.AddDays(i))
            .Select(d => days.FirstOrDefault(x => x.Date == d) is { } x ? new DayStats(d, x.ProblemsSolved, x.Correct, x.Xp) : new DayStats(d, 0, 0, 0)).ToList();

        var masteries = await db.SkillMasteries.AsNoTracking().Where(m => m.ProfileId == profile.Id && m.AttemptsCount > 0).ToListAsync(ct);
        var weekAttempts = attempts.Where(a => a.CreatedAt >= weekStart).ToList();
        var skillIds = weekAttempts.Select(a => a.SkillId).Concat(masteries.Select(m => m.SkillId));
        var refs = await SkillRefsAsync(db, skillIds, ct);
        var levelSlugs = await db.Skills.AsNoTracking().Where(s => refs.Keys.Contains(s.Id)).Select(s => new { s.Id, s.Level!.Slug }).ToDictionaryAsync(s => s.Id, s => s.Slug, ct);

        SkillStatus StatusOf(SkillMastery m) => LevelProgress.StatusOf(m.Rating, m.AttemptsCount, m.LastPracticedAt, LevelProgress.BySlug(levelSlugs[m.SkillId]), now);

        var skills = weekAttempts.GroupBy(a => a.SkillId).Where(g => refs.ContainsKey(g.Key)).Select(g =>
        {
            var ordered = g.OrderBy(a => a.CreatedAt).ToList();
            var mastery = masteries.FirstOrDefault(m => m.SkillId == g.Key);
            var status = mastery is null ? SkillStatus.Practicing : StatusOf(mastery);
            return new WeekSkill(refs[g.Key], ordered.Count, ordered.Count(a => a.IsCorrect), ordered[^1].RatingAfter - ordered[0].RatingBefore, status.ToString());
        }).OrderByDescending(s => s.Attempts).ToList();

        var attention = masteries.Where(m => refs.ContainsKey(m.SkillId)
                && (StatusOf(m) == SkillStatus.NeedsReview || (m.AttemptsCount >= 5 && (double)m.CorrectCount / m.AttemptsCount < 0.6)))
            .OrderBy(m => (double)m.CorrectCount / m.AttemptsCount).Take(5).Select(m => refs[m.SkillId]).ToList();

        var medals = await db.ProfileAchievements.AsNoTracking().Where(a => a.ProfileId == profile.Id && a.EarnedAt >= weekStart).ToListAsync(ct);
        var medalList = medals.Select(a => Achievements.All.FirstOrDefault(d => d.Slug == a.Slug) is { } def ? new WeekMedal(def.Name, def.Icon, a.EarnedAt) : null)
            .OfType<WeekMedal>().OrderBy(m => m.EarnedAt).ToList();

        var goals = await GoalsAsync(db, profile.Id, weekStart, ct);

        var tips = new List<string>();
        if (thisWeek.ActiveDays < 3) tips.Add(Tr("Pouca prática nesta semana. Sessões curtas em 3 ou mais dias ajudam mais do que uma sessão longa.", "Not much practice this week. Short sessions on 3 or more days help more than one long session.", "Poca práctica esta semana. Sesiones cortas en 3 o más días ayudan más que una sesión larga."));
        else if (thisWeek.ActiveDays >= 5) tips.Add(Tr($"Ótima constância: {thisWeek.ActiveDays} dias de prática na semana!", $"Great consistency: {thisWeek.ActiveDays} days of practice this week!", $"¡Excelente constancia: {thisWeek.ActiveDays} días de práctica en la semana!"));
        if (attention.Count > 0) tips.Add(Tr($"Vale criar um treino de reforço de \"{attention[0].Name}\": é onde houve mais dificuldade.", $"Consider creating extra practice for \"{attention[0].Name}\": that's where it was hardest.", $"Conviene crear un refuerzo de \"{attention[0].Name}\": es donde hubo más dificultad."));
        var improved = skills.Where(s => s.RatingChange >= 40).OrderByDescending(s => s.RatingChange).FirstOrDefault();
        if (improved is not null) tips.Add(Tr($"Evoluiu bem em \"{improved.Skill.Name}\". Vale elogiar!", $"Made great progress in \"{improved.Skill.Name}\". Worth some praise!", $"Avanzó mucho en \"{improved.Skill.Name}\". ¡Merece un elogio!"));
        if (thisWeek.Problems == 0) tips.Add(Tr("Nenhum problema nesta semana. Que tal o desafio do dia? Leva só 1 minuto.", "No problems this week. How about the daily challenge? It only takes 1 minute.", "Ningún problema esta semana. ¿Qué tal el desafío del día? Solo lleva 1 minuto."));

        return new WeeklyReport(profile.Id, profile.Nickname, profile.Avatar, from, today, thisWeek, lastWeek, dayList, skills, attention, medalList, goals, tips);
    }
}
