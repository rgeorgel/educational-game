namespace EduGame.Domain.Billing;

/// <summary>O que um plano libera. Jogar é sempre grátis; os planos são para adultos (responsáveis e professores).</summary>
public sealed record PlanFeatures(
    int MaxProfiles,
    int MaxClassrooms,
    int MaxActiveAssignmentsPerClassroom,
    bool CanExport,
    bool Worksheets,
    bool WeeklyReport,
    bool Reinforcement)
{
    /// <summary>Quem tem mais de um plano (ex.: professor que também é mãe) fica com o melhor de cada recurso.</summary>
    public PlanFeatures Union(PlanFeatures other) => new(
        Math.Max(MaxProfiles, other.MaxProfiles),
        Math.Max(MaxClassrooms, other.MaxClassrooms),
        Math.Max(MaxActiveAssignmentsPerClassroom, other.MaxActiveAssignmentsPerClassroom),
        CanExport || other.CanExport,
        Worksheets || other.Worksheets,
        WeeklyReport || other.WeeklyReport,
        Reinforcement || other.Reinforcement);
}

/// <param name="SelfService">Pode ser escolhido pela própria pessoa na página de planos (Escola é por contrato).</param>
/// <param name="PriceLabel">Preço previsto, exibido na página de planos.</param>
public sealed record PlanDefinition(string Slug, string Name, string Audience, string PriceLabel, bool SelfService, int Rank, PlanFeatures Features);

public static class Plans
{
    public const string FreeSlug = "gratis";
    public const string TeacherSlug = "professor";
    public const string FamilySlug = "familia";
    public const string SchoolSlug = "escola";

    public static readonly PlanDefinition Free = new(FreeSlug, "Grátis", "Para começar", "R$ 0", true, 0,
        new(MaxProfiles: 3, MaxClassrooms: 1, MaxActiveAssignmentsPerClassroom: 2, CanExport: false, Worksheets: false, WeeklyReport: false, Reinforcement: false));

    public static readonly PlanDefinition Teacher = new(TeacherSlug, "Professor Pro", "Para professores", "R$ 19,90/mês", true, 1,
        new(MaxProfiles: 3, MaxClassrooms: 20, MaxActiveAssignmentsPerClassroom: 50, CanExport: true, Worksheets: true, WeeklyReport: false, Reinforcement: false));

    public static readonly PlanDefinition Family = new(FamilySlug, "Família", "Para pais e responsáveis", "R$ 9,90/mês", true, 1,
        new(MaxProfiles: 8, MaxClassrooms: 1, MaxActiveAssignmentsPerClassroom: 2, CanExport: false, Worksheets: true, WeeklyReport: true, Reinforcement: true));

    public static readonly PlanDefinition School = new(SchoolSlug, "Escola", "Para escolas e redes", "Sob consulta", false, 2,
        new(MaxProfiles: 8, MaxClassrooms: 300, MaxActiveAssignmentsPerClassroom: 50, CanExport: true, Worksheets: true, WeeklyReport: true, Reinforcement: true));

    public static IReadOnlyList<PlanDefinition> All { get; } = [Free, Teacher, Family, School];

    public static PlanDefinition? BySlug(string? slug) => All.SingleOrDefault(p => p.Slug == slug);

    /// <summary>Enquanto a cobrança está desligada, todos têm os recursos do Professor Pro e da Família juntos.</summary>
    public static PlanFeatures Unrestricted { get; } = Teacher.Features.Union(Family.Features);

    /// <summary>Planos ativos da conta (sem assinatura = só o Grátis), do maior para o menor.</summary>
    public static IReadOnlyList<PlanDefinition> Active(IEnumerable<Subscription> subscriptions, DateTimeOffset now)
    {
        var active = subscriptions.Where(s => s.IsActive(now)).Select(s => BySlug(s.Plan)).OfType<PlanDefinition>()
            .Where(p => p.Slug != FreeSlug).Distinct().OrderByDescending(p => p.Rank).ToList();
        return active.Count == 0 ? [Free] : active;
    }

    /// <summary>Recursos liberados: a soma dos planos ativos; sem cobrança ligada, tudo do Professor Pro e da Família.</summary>
    public static PlanFeatures Features(IEnumerable<Subscription> subscriptions, DateTimeOffset now, bool enforced)
    {
        var features = Active(subscriptions, now).Select(p => p.Features).Aggregate((a, b) => a.Union(b));
        return enforced ? features : features.Union(Unrestricted);
    }
}

public static class SubscriptionSources
{
    /// <summary>Escolhido pela própria pessoa na página de planos (por enquanto sem pagamento).</summary>
    public const string Choice = "escolha";

    /// <summary>Concedido à mão com o comando "plano".</summary>
    public const string Manual = "manual";

    /// <summary>Pago pelo Stripe (integração futura).</summary>
    public const string Stripe = "stripe";
}

/// <summary>Assinatura de um plano: escolhida na página de planos, concedida à mão ou (no futuro) paga pelo Stripe.</summary>
public class Subscription
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public required string Plan { get; set; }
    public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Vazio = sem data para acabar.</summary>
    public DateTimeOffset? EndsAt { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>De onde veio (ver <see cref="SubscriptionSources"/>).</summary>
    public string Source { get; set; } = SubscriptionSources.Manual;

    /// <summary>Identificador no provedor de pagamento (ex.: assinatura do Stripe), quando houver.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Status no provedor (ex.: "active", "past_due", "canceled"), para o painel e o suporte.</summary>
    public string? ExternalStatus { get; set; }

    /// <summary>Quando o provedor mandou a última atualização (eventos fora de ordem mais antigos são ignorados).</summary>
    public DateTimeOffset? ExternalUpdatedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => StartsAt <= now && (EndsAt is null || EndsAt > now);
}
