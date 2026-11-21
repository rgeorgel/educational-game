using EduGame.Domain.Billing;
using Stripe;
using Stripe.Checkout;

namespace EduGame.Api.Features.Billing;

/// <summary>
/// Configuração do Stripe (seção "Stripe"; no Docker: STRIPE_SECRET_KEY e STRIPE_WEBHOOK_SECRET).
/// Sem as duas, o pagamento fica desligado e a página de planos continua no modo lançamento (escolha grátis).
/// Os preços são achados pelo nome fixo (lookup key) criado por "stripe configurar"; STRIPE_PRICE_* é opcional
/// e só serve para forçar um preço específico.
/// </summary>
public sealed class StripeSettings(IConfiguration config)
{
    public string? SecretKey => Value("Stripe:SecretKey");
    public string? WebhookSecret => Value("Stripe:WebhookSecret");

    /// <summary>Endereço público do site (ex.: https://educa-game.exemplo), para os links de volta do Checkout.</summary>
    public string? PublicBaseUrl => Value("App:PublicBaseUrl")?.TrimEnd('/');

    /// <summary>Preço forçado por configuração (opcional).</summary>
    public string? PriceFor(string plan) => Value($"Stripe:Prices:{plan}");

    /// <summary>Nome fixo do preço mensal de um plano no Stripe (ex.: "educagame_familia_mensal").</summary>
    public static string LookupKey(string plan) => $"educagame_{plan}_mensal";

    /// <summary>Id fixo do produto de um plano no Stripe (ex.: "educagame_familia").</summary>
    public static string ProductId(string plan) => $"educagame_{plan}";

    /// <summary>Plano de um preço do Stripe (para assinaturas sem metadata, ex.: criadas no painel).</summary>
    public string? PlanForPrice(string? priceId, string? lookupKey = null) =>
        PaidPlans.FirstOrDefault(p => (priceId is not null && PriceFor(p) == priceId) || (lookupKey is not null && LookupKey(p) == lookupKey));

    public bool Enabled => SecretKey is not null && WebhookSecret is not null;

    /// <summary>Planos que podem ser assinados pelo Stripe.</summary>
    public static readonly string[] PaidPlans = [Plans.TeacherSlug, Plans.FamilySlug];

    private string? Value(string key) => config[key] is { Length: > 0 } v ? v.Trim() : null;
}

/// <summary>As chamadas à API do Stripe (atrás de uma interface para os testes não dependerem da rede).</summary>
public interface IStripeGateway
{
    /// <summary>Id do preço ativo com este nome fixo (lookup key), ou null.</summary>
    Task<string?> FindPriceIdAsync(string lookupKey, CancellationToken ct);

    Task<string> CreateCustomerAsync(string email, Guid userId, CancellationToken ct);
    Task<string> CreateCheckoutAsync(string customerId, Guid userId, string plan, string priceId, string successUrl, string cancelUrl, CancellationToken ct);
    Task<string> CreatePortalAsync(string customerId, string returnUrl, CancellationToken ct);
}

public sealed class StripeGateway(StripeSettings settings) : IStripeGateway
{
    private StripeClient Client => new(settings.SecretKey ?? throw new InvalidOperationException("Stripe não configurado."));

    // Preços e configuração do portal mudam raramente: guarda por alguns minutos.
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string? Value, DateTimeOffset At)> _cache = new();

    private async Task<string?> CachedAsync(string key, Func<Task<string?>> load)
    {
        if (_cache.TryGetValue(key, out var hit) && hit.Value is not null && DateTimeOffset.UtcNow - hit.At < CacheFor) return hit.Value;
        var value = await load();
        _cache[key] = (value, DateTimeOffset.UtcNow);
        return value;
    }

    public Task<string?> FindPriceIdAsync(string lookupKey, CancellationToken ct) =>
        CachedAsync($"price:{lookupKey}", async () =>
            (await new PriceService(Client).ListAsync(new PriceListOptions { LookupKeys = [lookupKey], Active = true, Limit = 1 }, cancellationToken: ct))
                .Data.FirstOrDefault()?.Id);

    /// <summary>Configuração do portal criada por "stripe configurar" (metadata app = educagame); sem ela, usa a padrão da conta.</summary>
    private Task<string?> PortalConfigurationAsync(CancellationToken ct) =>
        CachedAsync("portal", async () =>
            (await new Stripe.BillingPortal.ConfigurationService(Client).ListAsync(new Stripe.BillingPortal.ConfigurationListOptions { Active = true, Limit = 100 }, cancellationToken: ct))
                .Data.FirstOrDefault(c => c.Metadata?.GetValueOrDefault("app") == StripeSetup.AppTag)?.Id);

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
            Configuration = await PortalConfigurationAsync(ct),
        }, cancellationToken: ct);
        return session.Url;
    }
}
