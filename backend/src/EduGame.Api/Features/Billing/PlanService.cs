using EduGame.Api.Common;
using EduGame.Domain.Billing;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Billing;

/// <summary>Plano da conta e o que ele permite (para a interface mostrar e os endpoints conferirem).</summary>
public sealed record PlanInfo(
    string Slug,
    string Name,
    bool Enforced,
    DateTimeOffset? EndsAt,
    int MaxClassrooms,
    int MaxActiveAssignmentsPerClassroom,
    bool CanExport);

/// <summary>
/// Controle de planos. Enquanto <c>Plans:Enforce</c> estiver desligado (padrão), ninguém é limitado:
/// todos têm os recursos do Professor Pro. Ao ligar, quem não tem assinatura fica no Grátis.
/// </summary>
public sealed class PlanService(AppDbContext db, IConfiguration config, GameClock clock)
{
    public bool Enforced => config.GetValue("Plans:Enforce", false);

    public async Task<PlanInfo> ForUserAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.Now;
        var subs = await db.Subscriptions.AsNoTracking().Where(s => s.UserId == userId).ToListAsync(ct);
        var plan = Plans.Effective(subs, now, Enforced);
        var granting = subs.Where(s => s.Plan == plan.Slug && s.IsActive(now)).ToList();
        DateTimeOffset? endsAt = granting.Count == 0 || granting.Any(s => s.EndsAt is null) ? null : granting.Max(s => s.EndsAt);
        return new PlanInfo(plan.Slug, plan.Name, Enforced, endsAt,
            plan.MaxClassrooms, plan.MaxActiveAssignmentsPerClassroom, plan.CanExport);
    }

    /// <summary>Mensagem para quando o limite do plano foi atingido.</summary>
    public static string LimitMessage(PlanInfo plan, string what) =>
        plan.Enforced && plan.Slug == Plans.FreeSlug
            ? $"{what} No plano Grátis isso é limitado; com o Professor Pro você tem mais. Fale com a gente para liberar."
            : what;
}
