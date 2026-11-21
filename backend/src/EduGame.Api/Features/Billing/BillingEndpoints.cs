using EduGame.Api.Common;
using EduGame.Domain.Billing;
using EduGame.Infrastructure;
using EduGame.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace EduGame.Api.Features.Billing;

/// <summary>Pagamento das assinaturas pelo Stripe: Checkout (assinar), Portal (gerenciar/cancelar) e webhook.</summary>
public static class BillingEndpoints
{
    public sealed record CheckoutRequest(string Plan);
    public sealed record BillingStatus(string Mode, IReadOnlyList<string> PaidPlans);

    public static void MapBilling(this RouteGroupBuilder api)
    {
        var billing = api.MapGroup("/billing").WithTags("Billing");

        // "stripe": os planos pagos são assinados pelo Checkout; "lancamento": escolha grátis (sem pagamento).
        billing.MapGet("/status", (StripeSettings stripe) =>
            Results.Ok(new BillingStatus(stripe.Enabled ? "stripe" : "lancamento", stripe.Enabled ? StripeSettings.PaidPlans : [])));

        billing.MapPost("/checkout", async (CheckoutRequest req, HttpContext http, AppDbContext db, StripeSettings stripe,
            IStripeGateway gateway, PlanService plans, CancellationToken ct) =>
        {
            if (!stripe.Enabled) return Unavailable();
            var userId = http.User.UserId();
            var user = await db.Users.SingleAsync(u => u.Id == userId, ct);
            if (user.IsGuest || user.Email is null)
                return ApiResults.Forbidden("Para assinar, salve o progresso criando uma conta.");
            if (!StripeSettings.PaidPlans.Contains(req.Plan))
                return ApiResults.Validation("plan", "Escolha o plano Professor Pro ou Família.");

            var current = await plans.ForUserAsync(userId, ct);
            if (current.Paid.Contains(req.Plan))
                return ApiResults.Conflict("Você já assina este plano. Use \"Gerenciar assinatura\" para mudar ou cancelar.");

            try
            {
                var price = stripe.PriceFor(req.Plan) ?? await gateway.FindPriceIdAsync(StripeSettings.LookupKey(req.Plan), ct);
                if (price is null)
                {
                    http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Stripe")
                        .LogError("Preço {LookupKey} não encontrado no Stripe. Rode \"dotnet EduGame.Api.dll stripe configurar\".", StripeSettings.LookupKey(req.Plan));
                    return Unavailable();
                }
                if (user.StripeCustomerId is null)
                {
                    user.StripeCustomerId = await gateway.CreateCustomerAsync(user.Email, user.Id, ct);
                    await db.SaveChangesAsync(ct);
                }
                var baseUrl = BaseUrl(http, stripe);
                var url = await gateway.CreateCheckoutAsync(user.StripeCustomerId, user.Id, req.Plan, price,
                    successUrl: $"{baseUrl}/jogar?pagamento=sucesso#/planos",
                    cancelUrl: $"{baseUrl}/jogar?pagamento=cancelado#/planos", ct);
                return Results.Ok(new { url });
            }
            catch (StripeException ex)
            {
                http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Stripe").LogError(ex, "Erro ao criar o Checkout do Stripe.");
                return Results.Problem("Não foi possível abrir o pagamento agora. Tente de novo em alguns minutos.", statusCode: StatusCodes.Status502BadGateway);
            }
        }).RequireAuthorization().RequireRateLimiting(RateLimits.Auth);

        billing.MapPost("/portal", async (HttpContext http, AppDbContext db, StripeSettings stripe, IStripeGateway gateway, CancellationToken ct) =>
        {
            if (!stripe.Enabled) return Unavailable();
            var userId = http.User.UserId();
            var customerId = await db.Users.Where(u => u.Id == userId).Select(u => u.StripeCustomerId).SingleOrDefaultAsync(ct);
            if (customerId is null) return ApiResults.NotFound("Você ainda não tem assinatura paga.");
            try
            {
                return Results.Ok(new { url = await gateway.CreatePortalAsync(customerId, $"{BaseUrl(http, stripe)}/jogar#/planos", ct) });
            }
            catch (StripeException ex)
            {
                http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Stripe").LogError(ex, "Erro ao abrir o portal do Stripe.");
                return Results.Problem("Não foi possível abrir a área da assinatura agora. Tente de novo em alguns minutos.", statusCode: StatusCodes.Status502BadGateway);
            }
        }).RequireAuthorization().RequireRateLimiting(RateLimits.Auth);

        // Avisos do Stripe. A assinatura (cabeçalho Stripe-Signature) é conferida com STRIPE_WEBHOOK_SECRET.
        billing.MapPost("/webhook", async (HttpContext http, StripeSettings stripe, StripeWebhook handler, ILoggerFactory logs, CancellationToken ct) =>
        {
            if (stripe.WebhookSecret is null) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            using var reader = new StreamReader(http.Request.Body);
            var json = await reader.ReadToEndAsync(ct);
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, http.Request.Headers["Stripe-Signature"], stripe.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                logs.CreateLogger("Stripe").LogWarning("Webhook do Stripe recusado: {Message}", ex.Message);
                return Results.BadRequest();
            }
            await handler.HandleAsync(stripeEvent, ct);
            return Results.Ok();
        }).AllowAnonymous();
    }

    private static IResult Unavailable() =>
        Results.Problem("O pagamento ainda não está ativo. Por enquanto, escolha o plano na página de planos.", statusCode: StatusCodes.Status503ServiceUnavailable);

    /// <summary>Endereço público para os links de volta. Em produção use App:PublicBaseUrl (PUBLIC_BASE_URL).</summary>
    private static string BaseUrl(HttpContext http, StripeSettings stripe) =>
        stripe.PublicBaseUrl ?? $"{http.Request.Scheme}://{http.Request.Host}";
}
