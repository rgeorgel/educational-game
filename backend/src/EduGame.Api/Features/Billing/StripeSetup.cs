using System.Globalization;
using EduGame.Domain.Billing;
using Stripe;

namespace EduGame.Api.Features.Billing;

/// <summary>
/// Configura a conta do Stripe a partir do código (rodar no container da API):
///   dotnet EduGame.Api.dll stripe configurar [--recriar-webhook]
///   dotnet EduGame.Api.dll stripe status
///   dotnet EduGame.Api.dll stripe preco familia 12,90
///
/// "configurar" cria (ou reaproveita) os produtos e preços mensais dos planos, o webhook e a configuração
/// do portal do cliente. Pode ser rodado de novo sem duplicar nada. Usa STRIPE_SECRET_KEY e PUBLIC_BASE_URL;
/// funciona igual no modo de teste (sk_test_) e no de produção (sk_live_).
/// </summary>
public static class StripeSetup
{
    public const string AppTag = "educagame";

    public static readonly string[] WebhookEvents =
    [
        StripeWebhook.CheckoutCompleted,
        StripeWebhook.SubscriptionCreated,
        StripeWebhook.SubscriptionUpdated,
        StripeWebhook.SubscriptionDeleted,
    ];

    public static bool IsInvoked(string[] args) => args.Length > 0 && args[0] == "stripe";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args, TextWriter output)
    {
        var settings = services.GetRequiredService<StripeSettings>();
        if (args.ElementAtOrDefault(1) is not ("configurar" or "status" or "preco"))
        {
            output.WriteLine("Uso:");
            output.WriteLine("  stripe configurar [--recriar-webhook]   cria produtos, preços, webhook e portal");
            output.WriteLine("  stripe status                         mostra o que está configurado");
            output.WriteLine("  stripe preco <professor|familia> <valor>   muda o preço mensal (ex.: 12,90)");
            return 0;
        }
        if (settings.SecretKey is null)
        {
            output.WriteLine("Erro: defina STRIPE_SECRET_KEY (a chave secreta do Stripe) e rode de novo.");
            return 1;
        }
        var client = new StripeClient(settings.SecretKey);
        var mode = settings.SecretKey.StartsWith("sk_live_") || settings.SecretKey.StartsWith("rk_live_") ? "PRODUÇÃO" : "TESTE";

        try
        {
            switch (args.ElementAtOrDefault(1))
            {
                case "configurar":
                    output.WriteLine($"Configurando o Stripe (modo {mode})…");
                    var prices = new Dictionary<string, string>();
                    foreach (var plan in PaidPlans()) prices[plan.Slug] = await EnsureProductAndPriceAsync(client, plan, output);
                    var webhookOk = await EnsureWebhookAsync(client, settings, args.Contains("--recriar-webhook"), output);
                    await EnsurePortalAsync(client, settings, prices, output);
                    output.WriteLine();
                    output.WriteLine(webhookOk
                        ? "Pronto. Confira STRIPE_SECRET_KEY e STRIPE_WEBHOOK_SECRET no Coolify e faça Redeploy."
                        : "Quase pronto: falta o webhook (veja acima). Depois faça Redeploy.");
                    return 0;

                case "status":
                    output.WriteLine($"Stripe no modo {mode}.");
                    foreach (var plan in PaidPlans())
                    {
                        var price = await FindPriceAsync(client, StripeSettings.LookupKey(plan.Slug));
                        output.WriteLine(price is null
                            ? $"  {plan.Name}: SEM PREÇO (rode \"stripe configurar\")"
                            : $"  {plan.Name}: {Money(price.UnitAmount ?? 0)}/mês ({price.Id})");
                    }
                    var url = WebhookUrl(settings);
                    var hook = url is null ? null : (await new WebhookEndpointService(client).ListAsync(new WebhookEndpointListOptions { Limit = 100 })).Data.FirstOrDefault(w => w.Url == url);
                    output.WriteLine(hook is null ? $"  Webhook: não encontrado ({url ?? "defina PUBLIC_BASE_URL"})" : $"  Webhook: {hook.Url} ({hook.Status}, API {hook.ApiVersion})");
                    output.WriteLine($"  Pagamento no site: {(settings.Enabled ? "ligado" : "desligado (falta STRIPE_WEBHOOK_SECRET)")}");
                    return 0;

                case "preco":
                {
                    var plan = Plans.BySlug(args.ElementAtOrDefault(2));
                    if (plan is null || !StripeSettings.PaidPlans.Contains(plan.Slug)
                        || !decimal.TryParse(args.ElementAtOrDefault(3)?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var reais) || reais <= 0)
                    {
                        output.WriteLine("Uso: stripe preco <professor|familia> <valor em reais, ex.: 12,90>");
                        return 1;
                    }
                    var cents = (long)Math.Round(reais * 100);
                    await EnsureProductAndPriceAsync(client, plan, output, newAmount: cents);
                    var prices2 = new Dictionary<string, string>();
                    foreach (var p in PaidPlans()) prices2[p.Slug] = (await FindPriceAsync(client, StripeSettings.LookupKey(p.Slug)))!.Id;
                    await EnsurePortalAsync(client, settings, prices2, output);
                    output.WriteLine("Novas assinaturas usam o preço novo; quem já assina continua no preço antigo.");
                    output.WriteLine("Atualize também o preço exibido na página de planos (PriceLabel em Plans.cs).");
                    return 0;
                }

                default:
                    return 0;
            }
        }
        catch (StripeException ex)
        {
            output.WriteLine($"Erro do Stripe: {ex.StripeError?.Message ?? ex.Message}");
            return 1;
        }
    }

    private static IEnumerable<PlanDefinition> PaidPlans() => StripeSettings.PaidPlans.Select(p => Plans.BySlug(p)!);

    private static string Money(long cents) => (cents / 100m).ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

    private static string? WebhookUrl(StripeSettings settings) => settings.PublicBaseUrl is { } b ? $"{b}/api/billing/webhook" : null;

    private static async Task<Price?> FindPriceAsync(StripeClient client, string lookupKey) =>
        (await new PriceService(client).ListAsync(new PriceListOptions { LookupKeys = [lookupKey], Active = true, Limit = 1 })).Data.FirstOrDefault();

    /// <summary>Produto com id fixo e preço mensal com nome fixo (lookup key). Devolve o id do preço.</summary>
    private static async Task<string> EnsureProductAndPriceAsync(StripeClient client, PlanDefinition plan, TextWriter output, long? newAmount = null)
    {
        var products = new ProductService(client);
        var productId = StripeSettings.ProductId(plan.Slug);
        Product product;
        try
        {
            product = await products.GetAsync(productId);
            if (!product.Active) product = await products.UpdateAsync(productId, new ProductUpdateOptions { Active = true });
            output.WriteLine($"  Produto {plan.Name}: já existe.");
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            product = await products.CreateAsync(new ProductCreateOptions
            {
                Id = productId,
                Name = $"Educa Game {plan.Name}",
                Description = plan.Audience,
                Metadata = new Dictionary<string, string> { ["app"] = AppTag, ["plan"] = plan.Slug },
            });
            output.WriteLine($"  Produto {plan.Name}: criado.");
        }

        var lookupKey = StripeSettings.LookupKey(plan.Slug);
        var existing = await FindPriceAsync(client, lookupKey);
        if (existing is not null && (newAmount is null || existing.UnitAmount == newAmount))
        {
            output.WriteLine($"  Preço {plan.Name}: {Money(existing.UnitAmount ?? 0)}/mês já existe ({existing.Id}).");
            return existing.Id;
        }

        var amount = newAmount ?? plan.MonthlyPriceCents;
        var price = await new PriceService(client).CreateAsync(new PriceCreateOptions
        {
            Product = product.Id,
            Currency = "brl",
            UnitAmount = amount,
            Recurring = new PriceRecurringOptions { Interval = "month" },
            LookupKey = lookupKey,
            TransferLookupKey = true, // o nome fixo passa para o preço novo
            Nickname = "Mensal",
            Metadata = new Dictionary<string, string> { ["app"] = AppTag, ["plan"] = plan.Slug },
        });
        output.WriteLine($"  Preço {plan.Name}: {Money(amount)}/mês criado ({price.Id}).");
        return price.Id;
    }

    /// <returns>false quando não deu para configurar o webhook (falta PUBLIC_BASE_URL).</returns>
    private static async Task<bool> EnsureWebhookAsync(StripeClient client, StripeSettings settings, bool recreate, TextWriter output)
    {
        var url = WebhookUrl(settings);
        if (url is null)
        {
            output.WriteLine("  Webhook: defina PUBLIC_BASE_URL (ex.: https://educa-game.seu-dominio) e rode de novo.");
            return false;
        }

        var service = new WebhookEndpointService(client);
        var existing = (await service.ListAsync(new WebhookEndpointListOptions { Limit = 100 })).Data.FirstOrDefault(w => w.Url == url);
        if (existing is not null && recreate)
        {
            await service.DeleteAsync(existing.Id);
            existing = null;
            output.WriteLine("  Webhook antigo apagado.");
        }

        if (existing is not null)
        {
            if (!WebhookEvents.All(e => existing.EnabledEvents.Contains(e)))
            {
                await service.UpdateAsync(existing.Id, new WebhookEndpointUpdateOptions { EnabledEvents = [.. WebhookEvents] });
                output.WriteLine("  Webhook: eventos atualizados.");
            }
            output.WriteLine($"  Webhook: já existe em {url}.");
            if (existing.ApiVersion != StripeConfiguration.ApiVersion)
                output.WriteLine($"    Atenção: o webhook usa a API {existing.ApiVersion}, e o app usa {StripeConfiguration.ApiVersion}. Rode com --recriar-webhook.");
            output.WriteLine("    O segredo (whsec_…) só aparece na criação: pegue no painel do Stripe ou rode com --recriar-webhook.");
            return true;
        }

        var created = await service.CreateAsync(new WebhookEndpointCreateOptions
        {
            Url = url,
            EnabledEvents = [.. WebhookEvents],
            ApiVersion = StripeConfiguration.ApiVersion,
            Description = "Educa Game: assinaturas",
            Metadata = new Dictionary<string, string> { ["app"] = AppTag },
        });
        output.WriteLine($"  Webhook: criado em {url} (API {StripeConfiguration.ApiVersion}).");
        output.WriteLine();
        output.WriteLine("  >>> Copie para STRIPE_WEBHOOK_SECRET (aparece só agora):");
        output.WriteLine($"  >>> {created.Secret}");
        output.WriteLine();
        return true;
    }

    /// <summary>Portal do cliente: cartão, recibos, cancelar no fim do período e trocar entre Professor Pro e Família.</summary>
    private static async Task EnsurePortalAsync(StripeClient client, StripeSettings settings, IReadOnlyDictionary<string, string> prices, TextWriter output)
    {
        var features = new Stripe.BillingPortal.ConfigurationFeaturesOptions
        {
            InvoiceHistory = new Stripe.BillingPortal.ConfigurationFeaturesInvoiceHistoryOptions { Enabled = true },
            PaymentMethodUpdate = new Stripe.BillingPortal.ConfigurationFeaturesPaymentMethodUpdateOptions { Enabled = true },
            CustomerUpdate = new Stripe.BillingPortal.ConfigurationFeaturesCustomerUpdateOptions { Enabled = true, AllowedUpdates = ["email", "name", "address", "tax_id"] },
            SubscriptionCancel = new Stripe.BillingPortal.ConfigurationFeaturesSubscriptionCancelOptions { Enabled = true, Mode = "at_period_end" },
            SubscriptionUpdate = new Stripe.BillingPortal.ConfigurationFeaturesSubscriptionUpdateOptions
            {
                Enabled = true,
                DefaultAllowedUpdates = ["price"],
                ProrationBehavior = "create_prorations",
                Products = [.. prices.Select(p => new Stripe.BillingPortal.ConfigurationFeaturesSubscriptionUpdateProductOptions
                {
                    Product = StripeSettings.ProductId(p.Key),
                    Prices = [p.Value],
                })],
            },
        };
        var business = new Stripe.BillingPortal.ConfigurationBusinessProfileOptions { Headline = "Educa Game: sua assinatura" };
        var returnUrl = settings.PublicBaseUrl is { } b ? $"{b}/jogar#/planos" : null;

        var service = new Stripe.BillingPortal.ConfigurationService(client);
        var existing = (await service.ListAsync(new Stripe.BillingPortal.ConfigurationListOptions { Active = true, Limit = 100 }))
            .Data.FirstOrDefault(c => c.Metadata?.GetValueOrDefault("app") == AppTag);
        if (existing is null)
        {
            await service.CreateAsync(new Stripe.BillingPortal.ConfigurationCreateOptions
            {
                Name = "Educa Game",
                BusinessProfile = business,
                DefaultReturnUrl = returnUrl,
                Features = features,
                Metadata = new Dictionary<string, string> { ["app"] = AppTag },
            });
            output.WriteLine("  Portal do cliente: configurado.");
        }
        else
        {
            await service.UpdateAsync(existing.Id, new Stripe.BillingPortal.ConfigurationUpdateOptions
            {
                BusinessProfile = new Stripe.BillingPortal.ConfigurationBusinessProfileOptions { Headline = business.Headline },
                DefaultReturnUrl = returnUrl,
                Features = features,
            });
            output.WriteLine("  Portal do cliente: atualizado.");
        }
    }
}
