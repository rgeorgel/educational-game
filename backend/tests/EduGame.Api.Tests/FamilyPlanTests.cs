using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduGame.Domain.Billing;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

/// <summary>Escolha de plano (sem pagamento) e recursos do plano Família.</summary>
[Collection(ApiCollection.Name)]
public class FamilyPlanTests(ApiFactory factory)
{
    private async Task<(HttpClient Client, Guid KidId)> FamilyAsync()
    {
        var client = factory.CreateClient();
        var email = $"f-{Guid.NewGuid():N}@exemplo.com";
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha-segura-123", acceptTerms = true })).StatusCode);
        var kid = await (await client.PostAsJsonAsync("/api/profiles", new { nickname = "Bia", avatar = "🦊" })).Content.ReadFromJsonAsync<JsonElement>();
        return (client, kid.GetProperty("id").GetGuid());
    }

    private async Task<string> CorrectAnswerAsync(Guid problemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.SessionProblems.AsNoTracking().SingleAsync(p => p.Id == problemId)).Content.Answer;
    }

    private async Task<JsonElement> PlayAsync(HttpClient client, Guid sessionId, bool correct)
    {
        while (true)
        {
            var next = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next");
            if (next.GetProperty("done").GetBoolean()) break;
            var id = next.GetProperty("problem").GetProperty("id").GetGuid();
            await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{id}/answer", new { answer = correct ? await CorrectAnswerAsync(id) : "-987654" });
        }
        return await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/summary");
    }

    [Fact]
    public async Task Anyone_can_see_the_plans_and_accounts_choose_one_without_paying()
    {
        var offers = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/plans");
        Assert.Contains(offers.EnumerateArray(), o => o.GetProperty("slug").GetString() == "familia" && o.GetProperty("selfService").GetBoolean());
        Assert.Contains(offers.EnumerateArray(), o => o.GetProperty("slug").GetString() == "escola" && !o.GetProperty("selfService").GetBoolean());

        var (client, _) = await FamilyAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/me/plan", new { plan = "escola" })).StatusCode);

        var chosen = await (await client.PutAsJsonAsync("/api/me/plan", new { plan = "familia" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("familia", chosen.GetProperty("chosen").GetString());
        Assert.Equal("Família", chosen.GetProperty("name").GetString());

        // Trocar de ideia: a escolha anterior é encerrada.
        var swapped = await (await client.PutAsJsonAsync("/api/me/plan", new { plan = "professor" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("professor", swapped.GetProperty("chosen").GetString());
        Assert.Single(swapped.GetProperty("plans").EnumerateArray());

        var back = await (await client.PutAsJsonAsync("/api/me/plan", new { plan = "gratis" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("gratis", back.GetProperty("slug").GetString());

        // Convidados precisam criar conta antes.
        var guest = factory.CreateClient();
        await guest.PostAsJsonAsync("/api/auth/guest", new { nickname = "Conv", avatar = "🐼" });
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PutAsJsonAsync("/api/me/plan", new { plan = "familia" })).StatusCode);
    }

    [Fact]
    public async Task Trial_from_the_admin_panel_shows_until_when_it_lasts()
    {
        var (client, kidId) = await FamilyAsync();
        var endsAt = DateTimeOffset.UtcNow.AddDays(14);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userId = await db.Profiles.Where(p => p.Id == kidId).Select(p => p.UserId).SingleAsync();
            db.Subscriptions.Add(new Subscription { UserId = userId, Plan = "familia", Source = SubscriptionSources.Trial, EndsAt = endsAt });
            db.Subscriptions.Add(new Subscription { UserId = userId, Plan = "professor", Source = SubscriptionSources.Trial,
                StartsAt = DateTimeOffset.UtcNow.AddDays(-10), EndsAt = DateTimeOffset.UtcNow.AddDays(-3) }); // teste que já acabou
            await db.SaveChangesAsync();
        }

        var plan = await client.GetFromJsonAsync<JsonElement>("/api/me/plan");
        Assert.Equal("familia", plan.GetProperty("slug").GetString());
        var trial = Assert.Single(plan.GetProperty("trials").EnumerateArray());
        Assert.Equal("familia", trial.GetProperty("slug").GetString());
        Assert.Equal(endsAt.ToUnixTimeSeconds(), trial.GetProperty("endsAt").GetDateTimeOffset().ToUnixTimeSeconds());
        Assert.Null(plan.GetProperty("chosen").GetString());
    }

    [Fact]
    public async Task Weekly_report_and_reinforcement_goal_flow()
    {
        var (client, kidId) = await FamilyAsync();

        // Joga uma etapa errando tudo: "Contagem e comparação" passa a pedir atenção.
        var map = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{kidId}/subjects/matematica/map");
        var stage = map.GetProperty("levels")[0].GetProperty("regions")[0].GetProperty("stages")[0].GetProperty("id").GetInt32();
        var start = await client.PostAsJsonAsync($"/api/profiles/{kidId}/sessions", new { stageId = stage });
        await PlayAsync(client, (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid(), correct: false);

        var report = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{kidId}/weekly-report");
        Assert.True(report.GetProperty("thisWeek").GetProperty("problems").GetInt32() >= 8);
        Assert.Equal(1, report.GetProperty("thisWeek").GetProperty("activeDays").GetInt32());
        Assert.Equal(7, report.GetProperty("days").GetArrayLength());
        Assert.NotEmpty(report.GetProperty("skills").EnumerateArray());
        var attention = report.GetProperty("attention").EnumerateArray().ToList();
        Assert.NotEmpty(attention);
        Assert.NotEmpty(report.GetProperty("tips").EnumerateArray());

        // O responsável cria um treino de reforço; a criança vê e joga.
        var skill = attention[0];
        var created = await client.PostAsJsonAsync($"/api/profiles/{kidId}/goals", new { skillSlug = skill.GetProperty("slug").GetString(), subjectSlug = skill.GetProperty("subjectSlug").GetString() });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var goalId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/profiles/{kidId}/goals", new { skillSlug = skill.GetProperty("slug").GetString() })).StatusCode); // repetido: devolve o mesmo
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/profiles/{kidId}/goals", new { skillSlug = "nao-existe" })).StatusCode);

        var goals = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{kidId}/goals");
        Assert.Equal(JsonValueKind.Null, Assert.Single(goals.EnumerateArray()).GetProperty("completedAt").ValueKind);

        var session = await client.PostAsJsonAsync($"/api/profiles/{kidId}/goals/{goalId}/start", new { });
        Assert.Equal(HttpStatusCode.Created, session.StatusCode);
        var summary = await PlayAsync(client, (await session.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid(), correct: true);
        Assert.Equal("reforco", summary.GetProperty("mode").GetString());
        Assert.Equal(3, summary.GetProperty("stars").GetInt32());

        goals = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{kidId}/goals");
        var goal = Assert.Single(goals.EnumerateArray());
        Assert.Equal(JsonValueKind.String, goal.GetProperty("completedAt").ValueKind);
        Assert.Equal(1, goal.GetProperty("attempts").GetInt32());

        // Outra família não acessa este perfil.
        var (other, _) = await FamilyAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/profiles/{kidId}/weekly-report")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/profiles/{kidId}/goals/{goalId}/start", new { })).StatusCode);
    }

    [Fact]
    public async Task Worksheets_have_printable_problems_and_an_answer_key()
    {
        var (client, _) = await FamilyAsync();
        var ws = await client.GetFromJsonAsync<JsonElement>("/api/worksheets?subject=matematica&skill=tabuada-completa&count=12&difficulty=dificil&seed=42");
        Assert.Equal(12, ws.GetProperty("problems").GetArrayLength());
        Assert.Equal(12, ws.GetProperty("answers").GetArrayLength());
        Assert.Equal("dificil", ws.GetProperty("difficulty").GetString());
        Assert.All(ws.GetProperty("problems").EnumerateArray(), p => Assert.NotEqual("fracao-visual", p.GetProperty("type").GetString()));
        Assert.False(ws.GetProperty("problems")[0].TryGetProperty("answer", out _)); // a resposta só no gabarito

        // A mesma semente gera a mesma ficha (para reimprimir).
        var again = await client.GetFromJsonAsync<JsonElement>("/api/worksheets?subject=matematica&skill=tabuada-completa&count=12&difficulty=dificil&seed=42");
        Assert.Equal(ws.GetProperty("problems")[0].GetProperty("prompt").GetString(), again.GetProperty("problems")[0].GetProperty("prompt").GetString());

        // Sudoku vira grade para preencher no papel.
        var sudoku = await client.GetFromJsonAsync<JsonElement>("/api/worksheets?subject=logica&skill=sudoku-6x6&count=4");
        Assert.All(sudoku.GetProperty("problems").EnumerateArray(), p => Assert.Equal("grade", p.GetProperty("type").GetString()));

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/worksheets?skill=nao-existe")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/worksheets?skill=tabuada-completa")).StatusCode);
    }

    [Fact]
    public async Task Family_accounts_can_have_up_to_eight_profiles()
    {
        var (client, _) = await FamilyAsync();
        for (var i = 2; i <= 8; i++)
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/profiles", new { nickname = $"Filho{i}", avatar = "🐼" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/profiles", new { nickname = "Nove", avatar = "🐼" })).StatusCode);
    }
}
