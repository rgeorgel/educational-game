using EduGame.Api.Common;
using EduGame.Domain.Billing;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace EduGame.Api.Features.Billing;

/// <summary>
/// Recebe os avisos do Stripe e mantém a tabela <c>subscriptions</c> em dia (Source = "stripe", ExternalId = id da assinatura).
/// Eventos tratados: checkout.session.completed e customer.subscription.created/updated/deleted.
/// É idempotente (procura pela assinatura do Stripe) e ignora eventos mais antigos que o último aplicado.
/// </summary>
public sealed class StripeWebhook(AppDbContext db, StripeSettings settings, GameClock clock, ILogger<StripeWebhook> log)
{
    public const string CheckoutCompleted = "checkout.session.completed";
    public const string SubscriptionCreated = "customer.subscription.created";
    public const string SubscriptionUpdated = "customer.subscription.updated";
    public const string SubscriptionDeleted = "customer.subscription.deleted";

    /// <summary>Status do Stripe que mantêm o plano ativo ("past_due" dá alguns dias para acertar o pagamento).</summary>
    private static readonly HashSet<string> ActiveStatuses = ["active", "trialing", "past_due"];

    public async Task HandleAsync(Event stripeEvent, CancellationToken ct)
    {
        var at = new DateTimeOffset(DateTime.SpecifyKind(stripeEvent.Created, DateTimeKind.Utc));
        switch (stripeEvent.Type)
        {
            case CheckoutCompleted when stripeEvent.Data.Object is Session session:
                await CheckoutCompletedAsync(session, at, ct);
                break;
            case SubscriptionCreated or SubscriptionUpdated or SubscriptionDeleted when stripeEvent.Data.Object is Stripe.Subscription sub:
                await SubscriptionChangedAsync(sub, stripeEvent.Type == SubscriptionDeleted, at, ct);
                break;
            default:
                log.LogInformation("Evento do Stripe ignorado: {Type}", stripeEvent.Type);
                break;
        }
    }

    private async Task CheckoutCompletedAsync(Session session, DateTimeOffset at, CancellationToken ct)
    {
        if (!Guid.TryParse(session.ClientReferenceId ?? session.Metadata?.GetValueOrDefault("userId"), out var userId)) return;
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;
        if (user.StripeCustomerId is null && session.CustomerId is not null) user.StripeCustomerId = session.CustomerId;

        // Pagamento confirmado na hora (cartão): já libera. Pagamentos que confirmam depois chegam por subscription.updated.
        var plan = session.Metadata?.GetValueOrDefault("plan");
        if (session.SubscriptionId is not null && Plans.BySlug(plan) is not null && session.PaymentStatus is "paid" or "no_payment_required")
            await UpsertAsync(user.Id, session.SubscriptionId, plan!, "active", startsAt: at, endsAt: null, at, ct);
        else
            await db.SaveChangesAsync(ct);
    }

    private async Task SubscriptionChangedAsync(Stripe.Subscription sub, bool deleted, DateTimeOffset at, CancellationToken ct)
    {
        var userId = Guid.TryParse(sub.Metadata?.GetValueOrDefault("userId"), out var id)
            ? id
            : await db.Users.Where(u => u.StripeCustomerId == sub.CustomerId).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct) ?? Guid.Empty;
        if (userId == Guid.Empty || !await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            log.LogWarning("Assinatura {Subscription} do Stripe sem usuário conhecido.", sub.Id);
            return;
        }

        var item = sub.Items?.Data?.FirstOrDefault();
        // O preço decide o plano (no portal a pessoa pode trocar de plano); a metadata do Checkout é a reserva.
        var plan = settings.PlanForPrice(item?.Price?.Id, item?.Price?.LookupKey)
            ?? (sub.Metadata?.GetValueOrDefault("plan") is { } p && Plans.BySlug(p) is not null ? p : null);
        if (plan is null)
        {
            log.LogWarning("Assinatura {Subscription} do Stripe com preço desconhecido ({Price}).", sub.Id, item?.Price?.Id);
            return;
        }

        var status = deleted ? "canceled" : sub.Status;
        DateTimeOffset? endsAt;
        if (ActiveStatuses.Contains(status))
        {
            // Cancelamento agendado: vale até o fim do período já pago.
            var cancelAt = sub.CancelAt ?? (sub.CancelAtPeriodEnd ? item?.CurrentPeriodEnd : null);
            endsAt = cancelAt is { } c ? Utc(c) : null;
        }
        else if (status == "incomplete")
        {
            return; // ainda aguardando o primeiro pagamento: não libera nada
        }
        else
        {
            endsAt = sub.EndedAt is { } ended ? Utc(ended) : at; // canceled, unpaid, incomplete_expired, paused
        }

        await UpsertAsync(userId, sub.Id, plan, status, Utc(sub.StartDate), endsAt, at, ct);
    }

    private async Task UpsertAsync(Guid userId, string externalId, string plan, string status, DateTimeOffset startsAt, DateTimeOffset? endsAt,
        DateTimeOffset eventAt, CancellationToken ct)
    {
        var row = await db.Subscriptions.SingleOrDefaultAsync(s => s.ExternalId == externalId, ct);
        if (row is not null && row.ExternalUpdatedAt > eventAt) return; // evento atrasado: já temos um mais novo
        if (row is null)
        {
            row = new Domain.Billing.Subscription { UserId = userId, Plan = plan, Source = SubscriptionSources.Stripe, ExternalId = externalId, StartsAt = startsAt };
            db.Subscriptions.Add(row);
        }
        row.Plan = plan;
        row.StartsAt = row.StartsAt <= startsAt ? row.StartsAt : startsAt;
        row.EndsAt = endsAt;
        row.ExternalStatus = status;
        row.ExternalUpdatedAt = eventAt;
        row.Note = "Stripe";

        // Assinou de verdade: encerra a escolha grátis do lançamento.
        if (endsAt is null || endsAt > clock.Now)
        {
            var now = clock.Now;
            await db.Subscriptions.Where(s => s.UserId == userId && s.Source == SubscriptionSources.Choice && (s.EndsAt == null || s.EndsAt > now))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EndsAt, now), ct);
        }
        await db.SaveChangesAsync(ct);
    }

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
