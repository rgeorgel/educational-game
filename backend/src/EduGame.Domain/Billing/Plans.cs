namespace EduGame.Domain.Billing;

/// <summary>O que cada plano permite. Jogar é sempre grátis; os planos são para adultos (professores e escolas).</summary>
public sealed record PlanDefinition(string Slug, string Name, int Rank, int MaxClassrooms, int MaxActiveAssignmentsPerClassroom, bool CanExport);

public static class Plans
{
    public const string FreeSlug = "gratis";
    public const string TeacherSlug = "professor";
    public const string SchoolSlug = "escola";

    public static readonly PlanDefinition Free = new(FreeSlug, "Grátis", 0, MaxClassrooms: 1, MaxActiveAssignmentsPerClassroom: 2, CanExport: false);
    public static readonly PlanDefinition Teacher = new(TeacherSlug, "Professor Pro", 1, MaxClassrooms: 20, MaxActiveAssignmentsPerClassroom: 50, CanExport: true);
    public static readonly PlanDefinition School = new(SchoolSlug, "Escola", 2, MaxClassrooms: 300, MaxActiveAssignmentsPerClassroom: 50, CanExport: true);

    public static IReadOnlyList<PlanDefinition> All { get; } = [Free, Teacher, School];

    public static PlanDefinition? BySlug(string? slug) => All.SingleOrDefault(p => p.Slug == slug);

    /// <summary>
    /// Plano efetivo: o maior plano ativo da conta. Enquanto a cobrança não estiver ligada
    /// (<paramref name="enforced"/> = false), todo mundo tem os recursos do Professor Pro.
    /// </summary>
    public static PlanDefinition Effective(IEnumerable<Subscription> subscriptions, DateTimeOffset now, bool enforced)
    {
        var best = subscriptions.Where(s => s.IsActive(now)).Select(s => BySlug(s.Plan)).OfType<PlanDefinition>()
            .OrderByDescending(p => p.Rank).FirstOrDefault() ?? Free;
        return enforced || best.Rank >= Teacher.Rank ? best : Teacher;
    }
}

/// <summary>Assinatura de um plano, concedida manualmente (comando "plano") ou, no futuro, por pagamento.</summary>
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

    public bool IsActive(DateTimeOffset now) => StartsAt <= now && (EndsAt is null || EndsAt > now);
}
