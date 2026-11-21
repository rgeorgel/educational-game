using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EduGame.Api.Features.Billing;
using EduGame.Domain.Billing;
using EduGame.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EduGame.Api.Tests;

/// <summary>Stripe falso: grava o que foi pedido e devolve endereços fixos (os testes não usam a rede).</summary>
public sealed class FakeStripeGateway : IStripeGateway
{
    public List<(Guid UserId, string Plan, string PriceId, string SuccessUrl)> Checkouts { get; } = [];

    /// <summary>Preços "criados" por stripe configurar, achados pelo nome fixo.</summary>
    public Task<string?> FindPriceIdAsync(string lookupKey, CancellationToken ct) =>
        Task.FromResult<string?>(lookupKey.StartsWith("educagame_") ? $"price_{lookupKey}" : null);

    public Task<string> CreateCustomerAsync(string email, Guid userId, CancellationToken ct) => Task.FromResult($"cus_{userId:N}");

    public Task<string> CreateCheckoutAsync(string customerId, Guid userId, string plan, string priceId, string successUrl, string cancelUrl, CancellationToken ct)
    {
        Checkouts.Add((userId, plan, priceId, successUrl));
        return Task.FromResult("https://checkout.stripe.test/c/pay/cs_test_123");
    }

    public Task<string> CreatePortalAsync(string customerId, string returnUrl, CancellationToken ct) =>
        Task.FromResult($"https://billing.stripe.test/p/session/{customerId}");
}

[Collection(ApiCollection.Name)]
public class StripeBillingTests(ApiFactory factory)
{
    private const string WebhookSecret = "whsec_teste_local";
    private readonly FakeStripeGateway _gateway = new();

    private WebApplicationFactory<Program> Stripe(bool fixedPrices = true) => factory.WithWebHostBuilder(b =>
    {
        b.UseSetting("Stripe:SecretKey", "sk_test_falso");
        b.UseSetting("Stripe:WebhookSecret", WebhookSecret);
        if (fixedPrices)
        {
            b.UseSetting("Stripe:Prices:professor", "price_pro");
            b.UseSetting("Stripe:Prices:familia", "price_fam");
        }
        b.UseSetting("App:PublicBaseUrl", "https://educa.exemplo");
        b.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<IStripeGateway>(_gateway)));
    });

    private static async Task<(HttpClient Client, Guid UserId)> AccountAsync(WebApplicationFactory<Program> app)
    {
        var client = app.CreateClient();
        var email = $"s-{Guid.NewGuid():N}@exemplo.com";
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha-segura-123", acceptTerms = true })).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (client, await db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync());
    }

    /// <summary>Assina o corpo como o Stripe: HMAC-SHA256 de "timestamp.corpo" com o segredo do webhook.</summary>
    private static HttpRequestMessage Signed(string json, string secret = WebhookSecret)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{ts}.{json}")));
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhook") { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        req.Headers.Add("Stripe-Signature", $"t={ts},v1={signature}");
        return req;
    }

    private static long Unix(DateTimeOffset t) => t.ToUnixTimeSeconds();

    private static string EventJson(string type, object dataObject, DateTimeOffset created) => JsonSerializer.Serialize(new Dictionary<string, object?>
    {
        ["id"] = $"evt_{Guid.NewGuid():N}",
        ["object"] = "event",
        ["api_version"] = global::Stripe.StripeConfiguration.ApiVersion,
        ["created"] = Unix(created),
        ["livemode"] = false,
        ["pending_webhooks"] = 1,
        ["type"] = type,
        ["data"] = new Dictionary<string, object?> { ["object"] = dataObject },
    });

    private static Dictionary<string, object?> SubscriptionObject(Guid userId, string id, string status, DateTimeOffset? cancelAt = null, DateTimeOffset? endedAt = null,
        string priceId = "price_fam", string? lookupKey = null, string metadataPlan = "familia") => new()
    {
        ["id"] = id,
        ["object"] = "subscription",
        ["customer"] = $"cus_{userId:N}",
        ["status"] = status,
        ["start_date"] = Unix(DateTimeOffset.UtcNow.AddMinutes(-5)),
        ["cancel_at"] = cancelAt is null ? null : Unix(cancelAt.Value),
        ["cancel_at_period_end"] = cancelAt is not null,
        ["ended_at"] = endedAt is null ? null : Unix(endedAt.Value),
        ["metadata"] = new Dictionary<string, string> { ["userId"] = userId.ToString(), ["plan"] = metadataPlan },
        ["items"] = new Dictionary<string, object?>
        {
            ["object"] = "list",
            ["data"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = "si_1", ["object"] = "subscription_item",
                    ["current_period_end"] = Unix(DateTimeOffset.UtcNow.AddDays(30)),
                    ["price"] = new Dictionary<string, object?> { ["id"] = priceId, ["object"] = "price", ["lookup_key"] = lookupKey },
                },
            },
        },
    };

    [Fact]
    public async Task Without_keys_payments_are_off_and_plans_stay_in_launch_mode()
    {
        var client = factory.CreateClient();
        Assert.Equal("lancamento", (await client.GetFromJsonAsync<JsonElement>("/api/billing/status")).GetProperty("mode").GetString());
        var (account, _) = await AccountAsync(factory);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await account.PostAsJsonAsync("/api/billing/checkout", new { plan = "familia" })).StatusCode);
    }

    [Fact]
    public async Task With_stripe_paid_plans_go_through_checkout()
    {
        using var app = Stripe();
        var status = await app.CreateClient().GetFromJsonAsync<JsonElement>("/api/billing/status");
        Assert.Equal("stripe", status.GetProperty("mode").GetString());

        var (client, userId) = await AccountAsync(app);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/me/plan", new { plan = "familia" })).StatusCode); // grátis não vale mais
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/me/plan", new { plan = "gratis" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/billing/checkout", new { plan = "escola" })).StatusCode);

        var res = await client.PostAsJsonAsync("/api/billing/checkout", new { plan = "familia" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.StartsWith("https://checkout.stripe.test/", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString());
        var checkout = Assert.Single(_gateway.Checkouts, c => c.UserId == userId);
        Assert.Equal("price_fam", checkout.PriceId);
        Assert.Equal("https://educa.exemplo/jogar?pagamento=sucesso#/planos", checkout.SuccessUrl);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal($"cus_{userId:N}", await db.Users.Where(u => u.Id == userId).Select(u => u.StripeCustomerId).SingleAsync());

        var guest = app.CreateClient();
        await guest.PostAsJsonAsync("/api/auth/guest", new { nickname = "Conv", avatar = "🐼" });
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PostAsJsonAsync("/api/billing/checkout", new { plan = "familia" })).StatusCode);
    }

    [Fact]
    public async Task Webhooks_activate_schedule_cancel_and_end_the_subscription()
    {
        using var app = Stripe();
        var webhook = app.CreateClient();
        var (client, userId) = await AccountAsync(app);
        var subId = $"sub_{Guid.NewGuid():N}";

        // Assinatura falsa (segredo errado) é recusada e não muda nada.
        var forged = EventJson("customer.subscription.updated", SubscriptionObject(userId, subId, "active"), DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.BadRequest, (await webhook.SendAsync(Signed(forged, "whsec_errado"))).StatusCode);

        // Checkout pago: o plano entra na hora.
        var session = new Dictionary<string, object?>
        {
            ["id"] = "cs_test_1", ["object"] = "checkout.session", ["client_reference_id"] = userId.ToString(),
            ["customer"] = $"cus_{userId:N}", ["subscription"] = subId, ["payment_status"] = "paid", ["mode"] = "subscription",
            ["metadata"] = new Dictionary<string, string> { ["userId"] = userId.ToString(), ["plan"] = "familia" },
        };
        var t0 = DateTimeOffset.UtcNow.AddSeconds(-30);
        Assert.Equal(HttpStatusCode.OK, (await webhook.SendAsync(Signed(EventJson("checkout.session.completed", session, t0)))).StatusCode);
        var plan = await client.GetFromJsonAsync<JsonElement>("/api/me/plan");
        Assert.Equal(["familia"], plan.GetProperty("paid").EnumerateArray().Select(p => p.GetString()));
        Assert.True(plan.GetProperty("canManageBilling").GetBoolean());

        // Repetir o mesmo aviso não duplica.
        await webhook.SendAsync(Signed(EventJson("checkout.session.completed", session, t0)));

        // Cancelamento agendado: continua valendo até a data.
        var cancelAt = DateTimeOffset.UtcNow.AddDays(20);
        await webhook.SendAsync(Signed(EventJson("customer.subscription.updated", SubscriptionObject(userId, subId, "active", cancelAt), t0.AddSeconds(10))));
        plan = await client.GetFromJsonAsync<JsonElement>("/api/me/plan");
        Assert.Contains("familia", plan.GetProperty("paid").EnumerateArray().Select(p => p.GetString()));

        // Um aviso atrasado (mais antigo) não desfaz o mais novo.
        await webhook.SendAsync(Signed(EventJson("customer.subscription.updated", SubscriptionObject(userId, subId, "active"), t0.AddSeconds(5))));

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.Subscriptions.SingleAsync(s => s.ExternalId == subId);
            Assert.Equal(SubscriptionSources.Stripe, row.Source);
            Assert.Equal("familia", row.Plan);
            Assert.NotNull(row.EndsAt);
            Assert.True(Math.Abs((row.EndsAt!.Value - cancelAt).TotalSeconds) < 2);
        }

        // Assinatura encerrada: o plano sai.
        await webhook.SendAsync(Signed(EventJson("customer.subscription.deleted", SubscriptionObject(userId, subId, "canceled", endedAt: DateTimeOffset.UtcNow.AddSeconds(-1)), t0.AddSeconds(20))));
        plan = await client.GetFromJsonAsync<JsonElement>("/api/me/plan");
        Assert.Empty(plan.GetProperty("paid").EnumerateArray());
        Assert.Equal("gratis", plan.GetProperty("slug").GetString());

        // O portal só existe para quem já é cliente.
        var portal = await client.PostAsJsonAsync("/api/billing/portal", new { });
        Assert.Equal(HttpStatusCode.OK, portal.StatusCode);
        var (stranger, _) = await AccountAsync(app);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync("/api/billing/portal", new { })).StatusCode);
    }

    [Fact]
    public async Task Paying_ends_the_free_launch_choice()
    {
        var (client, userId) = await AccountAsync(factory);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/me/plan", new { plan = "familia" })).StatusCode); // escolha do lançamento

        using var app = Stripe();
        var subId = $"sub_{Guid.NewGuid():N}";
        await app.CreateClient().SendAsync(Signed(EventJson("customer.subscription.created", SubscriptionObject(userId, subId, "active"), DateTimeOffset.UtcNow)));

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var subs = await db.Subscriptions.Where(s => s.UserId == userId).ToListAsync();
        Assert.Contains(subs, s => s.Source == SubscriptionSources.Stripe && s.EndsAt == null);
        Assert.All(subs.Where(s => s.Source == SubscriptionSources.Choice), s => Assert.True(s.EndsAt <= DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Prices_are_found_by_lookup_key_when_not_configured()
    {
        using var app = Stripe(fixedPrices: false);
        var (client, userId) = await AccountAsync(app);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/billing/checkout", new { plan = "professor" })).StatusCode);
        Assert.Equal("price_educagame_professor_mensal", Assert.Single(_gateway.Checkouts, c => c.UserId == userId).PriceId);
    }

    [Fact]
    public async Task Switching_plans_in_the_portal_follows_the_price_not_the_old_metadata()
    {
        using var app = Stripe(fixedPrices: false);
        var webhook = app.CreateClient();
        var (client, userId) = await AccountAsync(app);
        var subId = $"sub_{Guid.NewGuid():N}";
        var t0 = DateTimeOffset.UtcNow.AddSeconds(-30);

        await webhook.SendAsync(Signed(EventJson("customer.subscription.created",
            SubscriptionObject(userId, subId, "active", priceId: "price_x", lookupKey: "educagame_familia_mensal"), t0)));
        Assert.Equal(["familia"], (await client.GetFromJsonAsync<JsonElement>("/api/me/plan")).GetProperty("paid").EnumerateArray().Select(p => p.GetString()));

        // No portal, trocou para o Professor Pro: a metadata do Checkout continua dizendo "familia".
        await webhook.SendAsync(Signed(EventJson("customer.subscription.updated",
            SubscriptionObject(userId, subId, "active", priceId: "price_y", lookupKey: "educagame_professor_mensal", metadataPlan: "familia"), t0.AddSeconds(10))));
        Assert.Equal(["professor"], (await client.GetFromJsonAsync<JsonElement>("/api/me/plan")).GetProperty("paid").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact]
    public async Task Setup_command_requires_the_secret_key()
    {
        var output = new StringWriter();
        Assert.Equal(1, await StripeSetup.RunAsync(factory.Services, ["stripe", "configurar"], output));
        Assert.Contains("STRIPE_SECRET_KEY", output.ToString());
        Assert.Equal("educagame_familia_mensal", StripeSettings.LookupKey("familia"));
        Assert.Equal("educagame_professor", StripeSettings.ProductId("professor"));
    }
}
