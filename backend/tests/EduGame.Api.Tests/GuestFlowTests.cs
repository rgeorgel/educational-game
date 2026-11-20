using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduGame.Api.Features.Auth;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

/// <summary>Jogar sem cadastro (convidado) e "salvar o progresso" depois.</summary>
[Collection(ApiCollection.Name)]
public class GuestFlowTests(ApiFactory factory)
{
    private async Task<(HttpClient Client, JsonElement Me)> NewGuestAsync(string nickname = "Pipoca")
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/guest", new { nickname, avatar = "🐼" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (client, await res.Content.ReadFromJsonAsync<JsonElement>());
    }

    private async Task<string> CorrectAnswerAsync(Guid problemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.SessionProblems.AsNoTracking().SingleAsync(p => p.Id == problemId)).Content.Answer;
    }

    private async Task PlayFirstStageAsync(HttpClient client, Guid profileId)
    {
        var map = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/subjects/matematica/map");
        var stage = map.GetProperty("levels")[0].GetProperty("regions")[0].GetProperty("stages")[0].GetProperty("id").GetInt32();
        var start = await client.PostAsJsonAsync($"/api/profiles/{profileId}/sessions", new { stageId = stage });
        var sessionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid();
        while (true)
        {
            var next = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next");
            if (next.GetProperty("done").GetBoolean()) break;
            var id = next.GetProperty("problem").GetProperty("id").GetGuid();
            await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{id}/answer", new { answer = await CorrectAnswerAsync(id) });
        }
    }

    [Fact]
    public async Task Guest_starts_with_just_a_nickname_and_keeps_playing_on_the_same_device()
    {
        var (client, me) = await NewGuestAsync();
        Assert.True(me.GetProperty("isGuest").GetBoolean());
        Assert.Equal(JsonValueKind.Null, me.GetProperty("email").ValueKind);
        var profileId = me.GetProperty("profiles")[0].GetProperty("id").GetGuid();
        Assert.Equal("Pipoca", me.GetProperty("profiles")[0].GetProperty("nickname").GetString());

        await PlayFirstStageAsync(client, profileId);

        // "Voltar ao jogo" no mesmo aparelho: o cookie traz a conta de volta com o progresso.
        var again = await client.GetFromJsonAsync<JsonElement>("/api/me");
        Assert.True(again.GetProperty("isGuest").GetBoolean());
        var progress = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/progress");
        Assert.True(progress.GetProperty("totals").GetProperty("problemsSolved").GetInt32() >= 8);
    }

    [Fact]
    public async Task Guest_nickname_is_validated()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/guest", new { nickname = "<x>", avatar = "🐼" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Saving_progress_turns_the_guest_into_an_account_without_losing_anything()
    {
        var (client, me) = await NewGuestAsync("Bolinha");
        var profileId = me.GetProperty("profiles")[0].GetProperty("id").GetGuid();
        await PlayFirstStageAsync(client, profileId);

        var email = $"salvo-{Guid.NewGuid():N}@exemplo.com";
        var noConsent = await client.PostAsJsonAsync("/api/auth/upgrade", new { email, password = "senha-segura-123", acceptTerms = false });
        Assert.Equal(HttpStatusCode.BadRequest, noConsent.StatusCode);

        var upgrade = await client.PostAsJsonAsync("/api/auth/upgrade", new { email, password = "senha-segura-123", acceptTerms = true });
        Assert.Equal(HttpStatusCode.OK, upgrade.StatusCode);
        var saved = await upgrade.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(saved.GetProperty("isGuest").GetBoolean());
        Assert.Equal(profileId, saved.GetProperty("profiles")[0].GetProperty("id").GetGuid());

        // Em outro aparelho: entra com e-mail e senha e encontra o mesmo perfil e progresso.
        var other = factory.CreateClient();
        var login = await other.PostAsJsonAsync("/api/auth/login", new { email, password = "senha-segura-123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var progress = await other.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/progress");
        Assert.True(progress.GetProperty("totals").GetProperty("problemsSolved").GetInt32() >= 8);

        // Já salva: não dá para "salvar" de novo.
        var twice = await client.PostAsJsonAsync("/api/auth/upgrade", new { email = $"x-{email}", password = "senha-segura-123", acceptTerms = true });
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
    }

    [Fact]
    public async Task Saving_with_an_email_already_in_use_is_rejected_and_guest_stays_a_guest()
    {
        var email = $"existente-{Guid.NewGuid():N}@exemplo.com";
        await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new { email, password = "senha-segura-123", acceptTerms = true });

        var (client, _) = await NewGuestAsync();
        var res = await client.PostAsJsonAsync("/api/auth/upgrade", new { email, password = "senha-segura-123", acceptTerms = true });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True((await client.GetFromJsonAsync<JsonElement>("/api/me")).GetProperty("isGuest").GetBoolean());
    }

    [Fact]
    public async Task Guests_cannot_create_or_join_classrooms()
    {
        var (client, me) = await NewGuestAsync();
        var profileId = me.GetProperty("profiles")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/classrooms", new { name = "Turma" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/profiles/{profileId}/classrooms", new { code = "ABCDEFGH" })).StatusCode);
    }

    [Fact]
    public async Task Abandoned_guests_are_cleaned_up_but_saved_accounts_are_kept()
    {
        var (_, staleMe) = await NewGuestAsync("Antigo");
        var (_, freshMe) = await NewGuestAsync("Novo");
        var staleProfile = staleMe.GetProperty("profiles")[0].GetProperty("id").GetGuid();
        var freshProfile = freshMe.GetProperty("profiles")[0].GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var staleUser = await db.Profiles.Where(p => p.Id == staleProfile).Select(p => p.UserId).SingleAsync();
        await db.Users.Where(u => u.Id == staleUser).ExecuteUpdateAsync(u => u.SetProperty(x => x.LastSeenAt, DateTimeOffset.UtcNow.AddDays(-400)));

        var removed = await GuestCleanupService.RunOnceAsync(db, DateTimeOffset.UtcNow, TimeSpan.FromDays(180), CancellationToken.None);

        Assert.True(removed >= 1);
        Assert.False(await db.Profiles.AnyAsync(p => p.Id == staleProfile));
        Assert.True(await db.Profiles.AnyAsync(p => p.Id == freshProfile));
        Assert.True(await db.Users.AnyAsync(u => !u.IsGuest)); // contas salvas continuam
    }
}

[Collection(ApiCollection.Name)]
public class VisitTrackingTests(ApiFactory factory)
{
    [Fact]
    public async Task Opening_the_game_counts_daily_visits()
    {
        var client = factory.CreateClient();
        var guest = await client.PostAsJsonAsync("/api/auth/guest", new { nickname = "Visita", avatar = "🐼" });
        var me = await guest.Content.ReadFromJsonAsync<JsonElement>();
        var profileId = me.GetProperty("profiles")[0].GetProperty("id").GetGuid();
        await client.GetAsync("/api/me");
        await client.GetAsync("/api/me");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Profiles.Where(p => p.Id == profileId).Select(p => p.UserId).SingleAsync();
        var visits = await db.DailyVisits.Where(v => v.UserId == userId).SumAsync(v => v.Visits);
        Assert.Equal(3, visits); // criação + 2 aberturas
    }
}
