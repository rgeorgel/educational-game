using EduGame.Domain.Billing;
using EduGame.Infrastructure;
using EduGame.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Billing;

/// <summary>
/// Comando de terminal para conceder planos manualmente (primeiros clientes, sem cobrança automática).
/// No servidor (Coolify → Terminal do serviço edugame-api, ou docker compose exec edugame-api):
///   dotnet EduGame.Api.dll plano listar
///   dotnet EduGame.Api.dll plano conceder professora@escola.com professor 365 "Pix 12/2026"
///   dotnet EduGame.Api.dll plano remover professora@escola.com
/// </summary>
public static class PlanCommand
{
    public static bool IsInvoked(string[] args) => args.Length > 0 && args[0] == "plano";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args, TextWriter output)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var now = DateTimeOffset.UtcNow;
        var action = args.ElementAtOrDefault(1);

        switch (action)
        {
            case "listar":
            {
                var rows = await db.Subscriptions.AsNoTracking()
                    .Join(db.Users, s => s.UserId, u => u.Id, (s, u) => new { u.Email, s.Plan, s.Source, s.StartsAt, s.EndsAt, s.Note })
                    .OrderBy(x => x.Email).ToListAsync();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                output.WriteLine(config.GetValue("Plans:Enforce", false)
                    ? "Cobrança ligada (PLANS_ENFORCE=true): quem não tem assinatura está no plano Grátis."
                    : "Cobrança desligada (PLANS_ENFORCE=false): todos têm os recursos do Professor Pro e da Família.");
                if (rows.Count == 0) output.WriteLine("Nenhuma assinatura.");
                foreach (var r in rows)
                {
                    var active = r.StartsAt <= now && (r.EndsAt is null || r.EndsAt > now);
                    output.WriteLine($"{r.Email}\t{r.Plan}\t{r.Source}\t{(active ? "ativa" : "encerrada")}\taté {(r.EndsAt?.ToString("yyyy-MM-dd") ?? "sem fim")}\t{r.Note}");
                }
                return 0;
            }
            case "conceder":
            {
                var email = args.ElementAtOrDefault(2);
                var plan = Plans.BySlug(args.ElementAtOrDefault(3));
                if (email is null || plan is null || plan.Slug == Plans.FreeSlug)
                    return Usage(output, "Informe o e-mail e o plano (professor, familia ou escola).");
                var user = await users.FindByEmailAsync(email);
                if (user is null || user.IsGuest) return Usage(output, $"Conta não encontrada: {email}");
                int? days = int.TryParse(args.ElementAtOrDefault(4), out var d) && d > 0 ? d : null;
                db.Subscriptions.Add(new Subscription
                {
                    UserId = user.Id,
                    Plan = plan.Slug,
                    Source = SubscriptionSources.Manual,
                    StartsAt = now,
                    EndsAt = days is null ? null : now.AddDays(days.Value),
                    Note = args.ElementAtOrDefault(5)?[..Math.Min(200, args[5].Length)],
                });
                await db.SaveChangesAsync();
                output.WriteLine($"{plan.Name} concedido para {email}{(days is null ? " sem data para acabar" : $" por {days} dias")}.");
                return 0;
            }
            case "remover":
            {
                var email = args.ElementAtOrDefault(2);
                var user = email is null ? null : await users.FindByEmailAsync(email);
                if (user is null) return Usage(output, $"Conta não encontrada: {email}");
                var ended = await db.Subscriptions.Where(s => s.UserId == user.Id && (s.EndsAt == null || s.EndsAt > now))
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.EndsAt, now));
                output.WriteLine($"{ended} assinatura(s) encerrada(s) para {email}.");
                return 0;
            }
            default:
                return Usage(output, null);
        }
    }

    private static int Usage(TextWriter output, string? error)
    {
        if (error is not null) output.WriteLine($"Erro: {error}");
        output.WriteLine("Uso:");
        output.WriteLine("  plano listar");
        output.WriteLine("  plano conceder <e-mail> <professor|familia|escola> [dias] [nota]");
        output.WriteLine("  plano remover <e-mail>");
        return error is null ? 0 : 1;
    }
}
