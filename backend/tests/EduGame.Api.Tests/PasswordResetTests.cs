using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EduGame.Api.Features.Email;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

/// <summary>Guarda os e-mails em vez de enviar.</summary>
public sealed class FakeEmailTransport : IEmailTransport
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>Espera o e-mail chegar (o envio é feito em segundo plano).</summary>
    public async Task<EmailMessage> WaitForAsync(string to, string kind)
    {
        for (var i = 0; i < 100; i++)
        {
            if (Sent.FirstOrDefault(m => m.To == to && m.Kind == kind) is { } found) return found;
            await Task.Delay(50);
        }
        throw new TimeoutException($"Nenhum e-mail \"{kind}\" para {to}.");
    }
}

/// <summary>Esqueci minha senha, nova senha pelo link e e-mails de conta.</summary>
[Collection(ApiCollection.Name)]
public class PasswordResetTests(ApiFactory factory)
{
    private readonly FakeEmailTransport _mail = new();

    private WebApplicationFactory<Program> App() => factory.WithWebHostBuilder(b =>
        b.ConfigureTestServices(s => s.AddSingleton<IEmailTransport>(_mail)));

    private static async Task<(HttpClient Client, string Email)> RegisterAsync(WebApplicationFactory<Program> app)
    {
        var client = app.CreateClient();
        var email = $"r-{Guid.NewGuid():N}@exemplo.com";
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha-antiga-123", acceptTerms = true })).StatusCode);
        return (client, email);
    }

    private static (string UserId, string Token) ParseLink(EmailMessage mail)
    {
        var match = Regex.Match(mail.Text, @"/jogar#/nova-senha/([0-9a-f]{32})/([A-Za-z0-9_-]+)");
        Assert.True(match.Success, "O e-mail deveria ter o link de nova senha.");
        return (match.Groups[1].Value, match.Groups[2].Value);
    }

    [Fact]
    public async Task Forgot_password_sends_a_link_that_sets_a_new_password_once()
    {
        using var app = App();
        var (_, email) = await RegisterAsync(app);
        await _mail.WaitForAsync(email, "boas-vindas");

        var anon = app.CreateClient();
        Assert.Equal(HttpStatusCode.Accepted, (await anon.PostAsJsonAsync("/api/auth/forgot-password", new { email = email.ToUpperInvariant() })).StatusCode);
        var mail = await _mail.WaitForAsync(email, "senha-redefinir");
        Assert.Contains("<a href=", mail.Html);
        var (userId, token) = ParseLink(mail);

        // Senha curta é recusada sem gastar o link.
        var shortPassword = await anon.PostAsJsonAsync("/api/auth/reset-password", new { userId, token, password = "123" });
        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);

        var reset = await anon.PostAsJsonAsync("/api/auth/reset-password", new { userId, token, password = "senha-nova-456" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(email, (await reset.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("email").GetString());
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/api/me")).StatusCode); // já entrou
        await _mail.WaitForAsync(email, "senha-alterada");

        // O mesmo link não vale de novo; a senha antiga não entra mais e a nova entra.
        var again = await anon.PostAsJsonAsync("/api/auth/reset-password", new { userId, token, password = "outra-senha-789" });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        var fresh = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync("/api/auth/login", new { email, password = "senha-antiga-123" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fresh.PostAsJsonAsync("/api/auth/login", new { email, password = "senha-nova-456" })).StatusCode);
    }

    [Fact]
    public async Task Forgot_password_does_not_reveal_who_has_an_account_nor_spam_the_inbox()
    {
        using var app = App();
        var anon = app.CreateClient();

        var unknown = $"ninguem-{Guid.NewGuid():N}@exemplo.com";
        Assert.Equal(HttpStatusCode.Accepted, (await anon.PostAsJsonAsync("/api/auth/forgot-password", new { email = unknown })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anon.PostAsJsonAsync("/api/auth/forgot-password", new { email = "não é e-mail" })).StatusCode);

        var (_, email) = await RegisterAsync(app);
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Accepted, (await anon.PostAsJsonAsync("/api/auth/forgot-password", new { email })).StatusCode);
        await _mail.WaitForAsync(email, "senha-redefinir");
        await Task.Delay(200);
        Assert.Single(_mail.Sent, m => m.To == email && m.Kind == "senha-redefinir");
        Assert.DoesNotContain(_mail.Sent, m => m.To == unknown);
    }

    [Fact]
    public async Task Bad_links_are_refused()
    {
        using var app = App();
        var (_, email) = await RegisterAsync(app);
        var anon = app.CreateClient();
        await anon.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var (userId, token) = ParseLink(await _mail.WaitForAsync(email, "senha-redefinir"));

        foreach (var body in new object[]
        {
            new { userId, token = token[..^4] + "AAAA", password = "senha-nova-456" },
            new { userId, token = "!!!", password = "senha-nova-456" },
            new { userId = Guid.NewGuid().ToString("N"), token, password = "senha-nova-456" },
            new { userId = "x", token, password = "senha-nova-456" },
        })
        {
            var res = await anon.PostAsJsonAsync("/api/auth/reset-password", body);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }
    }
}
