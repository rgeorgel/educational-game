using EduGame.Api.Common;
using EduGame.Domain.Billing;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EduGame.Problems.Lang;

namespace EduGame.Api.Features.Billing;

public sealed record PlanSummary(string Slug, string Name);

/// <summary>Plano em período de teste grátis (dado pelo painel admin) e até quando vale.</summary>
public sealed record PlanTrial(string Slug, DateTimeOffset EndsAt);

/// <summary>Planos da conta e o que eles liberam (para a interface mostrar e os endpoints conferirem).</summary>
public sealed record PlanInfo(
    string Slug,
    string Name,
    IReadOnlyList<PlanSummary> Plans,
    string? Chosen,
    IReadOnlyList<string> Paid,
    IReadOnlyList<PlanTrial> Trials,
    IReadOnlyList<string> TrialAvailable,
    bool CanManageBilling,
    bool Enforced,
    DateTimeOffset? EndsAt,
    int MaxProfiles,
    int MaxClassrooms,
    int MaxActiveAssignmentsPerClassroom,
    bool CanExport,
    bool Worksheets,
    bool WeeklyReport,
    bool Reinforcement);

/// <summary>Um plano na página de planos.</summary>
public sealed record PlanOffer(string Slug, string Name, string Audience, string PriceLabel, bool SelfService, PlanFeatures Features);

/// <summary>
/// Controle de planos. Enquanto <c>Plans:Enforce</c> estiver desligado (padrão), ninguém é limitado:
/// todos têm os recursos do Professor Pro e da Família. Ao ligar, cada conta tem só o que os seus planos liberam.
/// Por enquanto a pessoa escolhe o plano no site sem pagar; depois o Stripe cria as assinaturas (Source = "stripe").
/// </summary>
public sealed class PlanService(AppDbContext db, IConfiguration config, GameClock clock)
{
    public bool Enforced => config.GetValue("Plans:Enforce", false);

    /// <summary>Planos da página de planos, no idioma da requisição.</summary>
    public static IReadOnlyList<PlanOffer> Offers =>
        [.. Plans.All.Select(p => new PlanOffer(p.Slug, p.Name, p.Audience, p.PriceLabel, p.SelfService, p.Features))];

    public async Task<PlanInfo> ForUserAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.Now;
        var subs = await db.Subscriptions.AsNoTracking().Where(s => s.UserId == userId).ToListAsync(ct);
        var active = Plans.Active(subs, now);
        var features = Plans.Features(subs, now, Enforced);
        var top = active[0];
        var granting = subs.Where(s => s.Plan == top.Slug && s.IsActive(now)).ToList();
        DateTimeOffset? endsAt = granting.Count == 0 || granting.Any(s => s.EndsAt is null) ? null : granting.Max(s => s.EndsAt);
        var chosen = subs.Where(s => s.Source == SubscriptionSources.Choice && s.IsActive(now)).OrderByDescending(s => s.StartsAt).FirstOrDefault()?.Plan;
        var paid = subs.Where(s => s.Source == SubscriptionSources.Stripe && s.IsActive(now)).Select(s => s.Plan).Distinct().ToList();
        var trials = subs.Where(s => s.Source == SubscriptionSources.Trial && s.IsActive(now) && s.EndsAt is not null && !paid.Contains(s.Plan))
            .GroupBy(s => s.Plan).Select(g => new PlanTrial(g.Key, g.Max(s => s.EndsAt!.Value))).ToList();
        // Teste grátis pelo site: uma vez por plano, para quem nunca testou nem pagou aquele plano.
        var trialAvailable = Plans.All.Where(Plans.HasSelfServiceTrial)
            .Where(p => !subs.Any(s => s.Plan == p.Slug && s.Source is SubscriptionSources.Trial or SubscriptionSources.Stripe))
            .Select(p => p.Slug).ToList();
        var hasCustomer = await db.Users.AnyAsync(u => u.Id == userId && u.StripeCustomerId != null, ct);

        return new PlanInfo(top.Slug, string.Join(" + ", active.Select(p => p.Name)),
            [.. active.Select(p => new PlanSummary(p.Slug, p.Name))], chosen, paid, trials, trialAvailable, hasCustomer, Enforced, endsAt,
            features.MaxProfiles, features.MaxClassrooms, features.MaxActiveAssignmentsPerClassroom,
            features.CanExport, features.Worksheets, features.WeeklyReport, features.Reinforcement);
    }

    /// <summary>
    /// A pessoa escolhe um plano no site. Troca a escolha anterior (planos concedidos à mão continuam).
    /// "gratis" só encerra a escolha anterior.
    /// </summary>
    public async Task<PlanInfo> ChooseAsync(Guid userId, PlanDefinition plan, CancellationToken ct)
    {
        var now = clock.Now;
        var previous = await db.Subscriptions
            .Where(s => s.UserId == userId && s.Source == SubscriptionSources.Choice && (s.EndsAt == null || s.EndsAt > now))
            .ToListAsync(ct);
        foreach (var s in previous) s.EndsAt = now;
        if (plan.Slug != Plans.FreeSlug)
        {
            db.Subscriptions.Add(new Subscription
            {
                UserId = userId,
                Plan = plan.Slug,
                StartsAt = now,
                Source = SubscriptionSources.Choice,
                Note = "Escolhido no site (lançamento, sem pagamento)",
            });
        }
        await db.SaveChangesAsync(ct);
        return await ForUserAsync(userId, ct);
    }

    /// <summary>
    /// Começa o teste grátis de <see cref="Plans.TrialDays"/> dias (sem cartão). Devolve o erro para mostrar,
    /// ou null se começou. Uma vez por plano: quem já testou (pelo site ou pelo admin) ou já pagou não testa de novo.
    /// </summary>
    public async Task<string?> StartTrialAsync(Guid userId, PlanDefinition plan, CancellationToken ct)
    {
        var used = await db.Subscriptions.AnyAsync(s => s.UserId == userId && s.Plan == plan.Slug
            && (s.Source == SubscriptionSources.Trial || s.Source == SubscriptionSources.Stripe), ct);
        if (used) return Tr($"Você já usou o teste grátis do {plan.Name}. Para continuar com ele, é só assinar.", $"You've already used the {plan.Name} free trial. To keep it, just subscribe.", $"Ya usaste la prueba gratis de {plan.Name}. Para seguir con él, solo suscríbete.");

        var now = clock.Now;
        db.Subscriptions.Add(new Subscription
        {
            UserId = userId,
            Plan = plan.Slug,
            StartsAt = now,
            EndsAt = now.AddDays(Plans.TrialDays),
            Source = SubscriptionSources.Trial,
            Note = $"Teste grátis de {Plans.TrialDays} dias (site)",
        });
        await db.SaveChangesAsync(ct);
        return null;
    }

    /// <summary>Mensagem para quando o limite do plano foi atingido.</summary>
    public static string LimitMessage(PlanInfo plan, string what) =>
        plan.Enforced ? Tr($"{what} Veja os planos para ter mais.", $"{what} See the plans to get more.", $"{what} Mira los planes para tener más.") : what;

    /// <summary>403 para recurso de outro plano; o app mostra o link para a página de planos.</summary>
    public static IResult Locked(string feature, string planSlug) =>
        TypedResults.Problem(
            detail: Tr($"{feature} faz parte do plano {Plans.BySlug(planSlug)?.Name ?? planSlug}.", $"{feature} is part of the {Plans.BySlug(planSlug)?.Name ?? planSlug} plan.", $"{feature} forma parte del plan {Plans.BySlug(planSlug)?.Name ?? planSlug}."),
            statusCode: StatusCodes.Status403Forbidden,
            title: Tr("Recurso de plano", "Plan feature", "Función del plan"),
            extensions: new Dictionary<string, object?> { ["plan"] = planSlug });
}
