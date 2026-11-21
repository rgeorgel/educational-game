using EduGame.Domain.Billing;
using Stripe;
using Stripe.Checkout;

namespace EduGame.Api.Features.Billing;

/// <summary>
/// Configuração do Stripe (seção "Stripe"; no Docker: STRIPE_SECRET_KEY, STRIPE_WEBHOOK_SECRET,
/// STRIPE_PRICE_PROFESSOR, STRIPE_PRICE_FAMILIA). Sem tudo preenchido, o pagamento fica desligado
/// e a página de planos continua no modo lançamento (escolha grátis).
/// </summary>
public sealed class StripeSettings(IConfiguration config)
{
    public string? SecretKey => Value("Stripe:SecretKey");
    public string? WebhookSecret => Value("Stripe:WebhookSecret");

    /// <summary>Endereço público do site (ex.: https://educa-game.exemplo), para os links de volta do Checkout.</summary>
    public string? PublicBaseUrl => Value("App:PublicBaseUrl")?.TrimEnd('/');

    public string? PriceFor(string plan) => Value($"Stripe:Prices:{plan}");

    /// <summary>Plano de um preço do Stripe (para assinaturas criadas fora do Checkout).</summary>
    public string? PlanForPrice(string? priceId) =>
        priceId is null ? null : new[] { Plans.TeacherSlug, Plans.FamilySlug }.FirstOrDefault(p => PriceFor(p) == priceId);

    public bool Enabled =>
        SecretKey is not null && WebhookSecret is not null && PriceFor(Plans.TeacherSlug) is not null && PriceFor(Plans.FamilySlug) is not null;

    /// <summary>Planos que podem ser assinados pelo Stripe.</summary>
    public static readonly string[] PaidPlans = [Plans.TeacherSlug, Plans.FamilySlug];

    private string? Value(string key) => config[key] is { Length: > 0 } v ? v.Trim() : null;
}

/// <summary>As chamadas à API do Stripe (atrás de uma interface para os testes não dependerem da rede).</summary>
public interface IStripeGateway
{
    Task<string> CreateCustomerAsync(string email, Guid userId, CancellationToken ct);
    Task<string> CreateCheckoutAsync(string customerId, Guid userId, string plan, string priceId, string successUrl, string cancelUrl, CancellationToken ct);
    Task<string> CreatePortalAsync(string customerId, string returnUrl, CancellationToken ct);
}

public sealed class StripeGateway(StripeSettings settings) : IStripeGateway
{
    private StripeClient Client => new(settings.SecretKey ?? throw new InvalidOperationException("Stripe não configurado."));

    public async Task<string> CreateCustomerAsync(string email, Guid userId, CancellationToken ct)
    {
        var customer = await new CustomerService(Client).CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            PreferredLocales = ["pt-BR"],
            Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() },
        }, cancellationToken: ct);
        return customer.Id;
    }

    public async Task<string> CreateCheckoutAsync(string customerId, Guid userId, string plan, string priceId, string successUrl, string cancelUrl, CancellationToken ct)
    {
        var metadata = new Dictionary<string, string> { ["userId"] = userId.ToString(), ["plan"] = plan };
        var session = await new SessionService(Client).CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            ClientReferenceId = userId.ToString(),
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata },
            Metadata = metadata,
            AllowPromotionCodes = true,
            Locale = "pt-BR",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        }, new RequestOptions { IdempotencyKey = $"checkout-{userId}-{plan}-{DateTime.UtcNow:yyyyMMddHHmm}" }, ct);
        return session.Url;
    }

    public async Task<string> CreatePortalAsync(string customerId, string returnUrl, CancellationToken ct)
    {
        var session = await new Stripe.BillingPortal.SessionService(Client).CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl,
            Locale = "pt-BR",
        }, cancellationToken: ct);
        return session.Url;
    }
}
