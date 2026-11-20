using System.Net.Http.Json;
using System.Text.Json;
using EduGame.Api.Common;
using EduGame.Infrastructure;
using EduGame.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

public class SignupOriginTests
{
    private static AppUser Apply(OriginRequest? origin)
    {
        var user = new AppUser();
        SignupOrigin.Apply(user, origin, "educa-game.exemplo");
        return user;
    }

    [Fact]
    public void Utm_values_are_kept_and_cleaned()
    {
        var u = Apply(new("WhatsApp", "Grupo Professores", "Volta às aulas!", null));
        Assert.Equal("whatsapp", u.SignupSource);
        Assert.Equal("grupo-professores", u.SignupMedium);
        Assert.Equal("volta-as-aulas", u.SignupCampaign); // sem acentos e sem símbolos
    }

    [Theory]
    [InlineData("https://www.google.com.br/search?q=jogo", "google", "google.com.br")]
    [InlineData("https://gemini.google.com/app/abc", "gemini", "gemini.google.com")]
    [InlineData("https://l.instagram.com/?u=x", "instagram", "l.instagram.com")]
    [InlineData("https://chatgpt.com/c/123", "chatgpt", "chatgpt.com")]
    [InlineData("https://blog.escola.com.br/post/1", "blog.escola.com.br", "blog.escola.com.br")]
    public void Without_utm_the_source_comes_from_the_referrer_domain(string referrer, string source, string host)
    {
        var u = Apply(new(null, null, null, referrer));
        Assert.Equal(source, u.SignupSource);
        Assert.Equal(host, u.SignupReferrer); // só o domínio, nunca o caminho
    }

    [Fact]
    public void Own_site_and_missing_origin_count_as_direct()
    {
        Assert.Equal(SignupOrigin.Direct, Apply(null).SignupSource);
        var own = Apply(new(null, null, null, "https://educa-game.exemplo/professores"));
        Assert.Equal(SignupOrigin.Direct, own.SignupSource);
        Assert.Null(own.SignupReferrer);
        Assert.Null(Apply(new(null, null, null, "javascript:alert(1)")).SignupReferrer);
    }

    [Fact]
    public void Long_values_are_truncated()
    {
        Assert.Equal(64, Apply(new(new string('a', 300))).SignupSource!.Length);
    }
}

[Collection(ApiCollection.Name)]
public class GrowthApiTests(ApiFactory factory)
{
    [Fact]
    public async Task Guest_signup_records_the_origin()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/guest", new
        {
            nickname = "Origem",
            avatar = "🐼",
            origin = new { source = "compartilhar", medium = "app", campaign = "desafio-do-dia", referrer = (string?)null },
        });
        var me = await res.Content.ReadFromJsonAsync<JsonElement>();
        var profileId = me.GetProperty("profiles")[0].GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Profiles.Where(p => p.Id == profileId).Select(p => p.UserId).SingleAsync();
        var user = await db.Users.SingleAsync(u => u.Id == userId);
        Assert.Equal("compartilhar", user.SignupSource);
        Assert.Equal("desafio-do-dia", user.SignupCampaign);
    }

    [Fact]
    public async Task Old_clients_without_origin_still_work_and_count_as_direct()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/guest", new { nickname = "Antigo", avatar = "🐼" });
        res.EnsureSuccessStatusCode();
        var me = await res.Content.ReadFromJsonAsync<JsonElement>();
        var profileId = me.GetProperty("profiles")[0].GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var source = await db.Profiles.Where(p => p.Id == profileId).Join(db.Users, p => p.UserId, u => u.Id, (p, u) => u.SignupSource).SingleAsync();
        Assert.Equal("direto", source);
    }

    [Fact]
    public async Task Session_summary_has_one_mark_per_problem_without_revealing_answers()
    {
        var client = factory.CreateClient();
        var guest = await client.PostAsJsonAsync("/api/auth/guest", new { nickname = "Marcas", avatar = "🐼" });
        var profileId = (await guest.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("profiles")[0].GetProperty("id").GetGuid();
        var map = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/subjects/matematica/map");
        var stage = map.GetProperty("levels")[0].GetProperty("regions")[0].GetProperty("stages")[0].GetProperty("id").GetInt32();
        var sessionId = (await (await client.PostAsJsonAsync($"/api/profiles/{profileId}/sessions", new { stageId = stage })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid();

        // Responde tudo errado: cada erro gera um "problema irmão", então há pelo menos 1 marca por resposta.
        var answered = 0;
        while (true)
        {
            var next = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next");
            if (next.GetProperty("done").GetBoolean()) break;
            var id = next.GetProperty("problem").GetProperty("id").GetGuid();
            await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{id}/answer", new { answer = "-987654" });
            answered++;
        }

        var summary = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/summary");
        var marks = summary.GetProperty("marks").EnumerateArray().Select(m => m.GetString()).ToList();
        Assert.Equal(answered, marks.Count);
        Assert.All(marks, m => Assert.Equal("erro", m));
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("dailyDate").ValueKind);
    }
}
