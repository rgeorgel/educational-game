using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

[Collection(ApiCollection.Name)]
public class GameFlowTests(ApiFactory factory)
{
    private async Task<(HttpClient Client, Guid ProfileId)> NewPlayerAsync()
    {
        var client = factory.CreateClient();
        var email = $"resp-{Guid.NewGuid():N}@exemplo.com";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha-segura-123", acceptTerms = true });
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

        var created = await client.PostAsJsonAsync("/api/profiles", new { nickname = "Luna", avatar = "🦊", schoolGrade = 3 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var profile = await created.Content.ReadFromJsonAsync<JsonElement>();
        return (client, profile.GetProperty("id").GetGuid());
    }

    private async Task<JsonElement> MapAsync(HttpClient client, Guid profileId, string subject = "matematica") =>
        await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/subjects/{subject}/map");

    private static List<JsonElement> Stages(JsonElement map) =>
        [.. map.GetProperty("levels").EnumerateArray()
            .SelectMany(l => l.GetProperty("regions").EnumerateArray())
            .SelectMany(r => r.GetProperty("stages").EnumerateArray())];

    private async Task<string> CorrectAnswerAsync(Guid problemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var problem = await db.SessionProblems.AsNoTracking().SingleAsync(p => p.Id == problemId);
        return problem.Content.Answer;
    }

    private async Task<Guid> StartAsync(HttpClient client, Guid profileId, int stageId)
    {
        var res = await client.PostAsJsonAsync($"/api/profiles/{profileId}/sessions", new { stageId });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid();
    }

    private static async Task<JsonElement> NextAsync(HttpClient client, Guid sessionId) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next");

    private static async Task<JsonElement> AnswerAsync(HttpClient client, Guid sessionId, Guid problemId, string answer)
    {
        var res = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{problemId}/answer", new { answer });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Unauthenticated_requests_get_401()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profiles")).StatusCode);
    }

    [Fact]
    public async Task Register_requires_consent_and_valid_data()
    {
        var client = factory.CreateClient();
        var noConsent = await client.PostAsJsonAsync("/api/auth/register", new { email = "a@b.com", password = "senha-segura-123", acceptTerms = false });
        Assert.Equal(HttpStatusCode.BadRequest, noConsent.StatusCode);

        var shortPassword = await client.PostAsJsonAsync("/api/auth/register", new { email = $"{Guid.NewGuid():N}@b.com", password = "123", acceptTerms = true });
        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);
    }

    [Fact]
    public async Task Login_and_logout_round_trip()
    {
        var email = $"login-{Guid.NewGuid():N}@exemplo.com";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha-segura-123", acceptTerms = true });
        await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);

        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "errada-123456" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        var ok = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "senha-segura-123" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Profile_validation_rejects_bad_nickname_and_avatar()
    {
        var (client, _) = await NewPlayerAsync();
        var bad = await client.PostAsJsonAsync("/api/profiles", new { nickname = "<script>", avatar = "🦊" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var badAvatar = await client.PostAsJsonAsync("/api/profiles", new { nickname = "Teo", avatar = "💩" });
        Assert.Equal(HttpStatusCode.BadRequest, badAvatar.StatusCode);
    }

    [Fact]
    public async Task New_player_sees_only_the_first_stage_unlocked()
    {
        var (client, profileId) = await NewPlayerAsync();

        var map = await MapAsync(client, profileId);
        var stages = Stages(map);

        Assert.Equal(27, stages.Count);
        Assert.True(stages[0].GetProperty("unlocked").GetBoolean());
        Assert.All(stages.Skip(1), s => Assert.False(s.GetProperty("unlocked").GetBoolean()));
        Assert.Equal("iniciante", map.GetProperty("progress").GetProperty("level").GetProperty("slug").GetString());
        Assert.Equal(0, map.GetProperty("progress").GetProperty("percent").GetInt32());
    }

    [Fact]
    public async Task Locked_stage_cannot_be_started()
    {
        var (client, profileId) = await NewPlayerAsync();
        var second = Stages(await MapAsync(client, profileId))[1].GetProperty("id").GetInt32();

        var res = await client.PostAsJsonAsync($"/api/profiles/{profileId}/sessions", new { stageId = second });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Problems_never_expose_the_answer()
    {
        var (client, profileId) = await NewPlayerAsync();
        var first = Stages(await MapAsync(client, profileId))[0].GetProperty("id").GetInt32();
        var sessionId = await StartAsync(client, profileId, first);

        var raw = await client.GetStringAsync($"/api/sessions/{sessionId}/next");

        Assert.DoesNotContain("\"answer\"", raw);
        Assert.DoesNotContain("\"explanation\"", raw);
    }

    [Fact]
    public async Task Perfect_session_gives_three_stars_xp_and_unlocks_next_stage()
    {
        var (client, profileId) = await NewPlayerAsync();
        var first = Stages(await MapAsync(client, profileId))[0];
        var sessionId = await StartAsync(client, profileId, first.GetProperty("id").GetInt32());
        var total = first.GetProperty("problemCount").GetInt32();

        for (var i = 1; i <= total; i++)
        {
            var next = await NextAsync(client, sessionId);
            Assert.False(next.GetProperty("done").GetBoolean());
            var problem = next.GetProperty("problem");
            Assert.Equal(i, problem.GetProperty("number").GetInt32());
            var problemId = problem.GetProperty("id").GetGuid();

            var result = await AnswerAsync(client, sessionId, problemId, await CorrectAnswerAsync(problemId));

            Assert.True(result.GetProperty("correct").GetBoolean());
            Assert.True(result.GetProperty("xpEarned").GetInt32() > 0);
            Assert.True(result.GetProperty("ratingAfter").GetInt32() > result.GetProperty("ratingBefore").GetInt32());
            Assert.Equal(i == total, result.GetProperty("sessionFinished").GetBoolean());
        }

        Assert.True((await NextAsync(client, sessionId)).GetProperty("done").GetBoolean());

        var summary = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/summary");
        Assert.Equal(3, summary.GetProperty("stars").GetInt32());
        Assert.Equal(total, summary.GetProperty("correct").GetInt32());
        Assert.True(summary.GetProperty("nextStageUnlocked").GetBoolean());
        Assert.True(summary.GetProperty("progress").GetProperty("percent").GetInt32() > 0);

        var stages = Stages(await MapAsync(client, profileId));
        Assert.Equal(3, stages[0].GetProperty("stars").GetInt32());
        Assert.True(stages[1].GetProperty("unlocked").GetBoolean());
        Assert.False(stages[2].GetProperty("unlocked").GetBoolean());

        var progress = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/progress");
        var totals = progress.GetProperty("totals");
        Assert.Equal(total, totals.GetProperty("problemsSolved").GetInt32());
        Assert.Equal(3, totals.GetProperty("stars").GetInt32());
        Assert.True(totals.GetProperty("xp").GetInt32() > 0);
    }

    [Fact]
    public async Task Wrong_answer_is_followed_by_a_sibling_problem_and_keeps_stage_locked_when_failing()
    {
        var (client, profileId) = await NewPlayerAsync();
        var first = Stages(await MapAsync(client, profileId))[0];
        var sessionId = await StartAsync(client, profileId, first.GetProperty("id").GetInt32());
        var total = first.GetProperty("problemCount").GetInt32();

        string? previousGenerator = null;
        for (var i = 0; i < total; i++)
        {
            var problem = (await NextAsync(client, sessionId)).GetProperty("problem");
            var problemId = problem.GetProperty("id").GetGuid();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var key = await db.SessionProblems.Where(p => p.Id == problemId).Select(p => p.GeneratorKey).SingleAsync();
                if (previousGenerator is not null) Assert.Equal(previousGenerator, key);
                previousGenerator = key;
            }

            var result = await AnswerAsync(client, sessionId, problemId, "resposta-errada");
            Assert.False(result.GetProperty("correct").GetBoolean());
            Assert.False(string.IsNullOrEmpty(result.GetProperty("explanation").GetString()));
            Assert.Equal(1, result.GetProperty("xpEarned").GetInt32()); // XP de esforço
        }

        var summary = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/summary");
        Assert.Equal(0, summary.GetProperty("stars").GetInt32());
        Assert.False(summary.GetProperty("nextStageUnlocked").GetBoolean());
    }

    [Fact]
    public async Task Hints_are_revealed_one_at_a_time_and_answering_twice_conflicts()
    {
        var (client, profileId) = await NewPlayerAsync();
        var first = Stages(await MapAsync(client, profileId))[0].GetProperty("id").GetInt32();
        var sessionId = await StartAsync(client, profileId, first);
        var problem = (await NextAsync(client, sessionId)).GetProperty("problem");
        var problemId = problem.GetProperty("id").GetGuid();
        var hintsTotal = problem.GetProperty("hintsTotal").GetInt32();

        for (var i = 1; i <= hintsTotal; i++)
        {
            var hint = await client.PostAsync($"/api/sessions/{sessionId}/problems/{problemId}/hint", null);
            Assert.Equal(HttpStatusCode.OK, hint.StatusCode);
            var body = await hint.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(i, body.GetProperty("hints").GetArrayLength());
        }
        var tooMany = await client.PostAsync($"/api/sessions/{sessionId}/problems/{problemId}/hint", null);
        Assert.Equal(HttpStatusCode.Conflict, tooMany.StatusCode);

        // Revelar dicas persiste: o próximo "next" devolve o mesmo problema com as dicas já vistas.
        var again = (await NextAsync(client, sessionId)).GetProperty("problem");
        Assert.Equal(problemId, again.GetProperty("id").GetGuid());
        Assert.Equal(hintsTotal, again.GetProperty("hints").GetArrayLength());

        await AnswerAsync(client, sessionId, problemId, await CorrectAnswerAsync(problemId));
        var twice = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{problemId}/answer", new { answer = "1" });
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
    }

    [Fact]
    public async Task Players_cannot_access_other_accounts_data()
    {
        var (alice, aliceProfile) = await NewPlayerAsync();
        var (bob, bobProfile) = await NewPlayerAsync();
        var first = Stages(await MapAsync(alice, aliceProfile))[0].GetProperty("id").GetInt32();
        var aliceSession = await StartAsync(alice, aliceProfile, first);

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/profiles/{aliceProfile}/progress")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/sessions/{aliceSession}/next")).StatusCode);
        var start = await bob.PostAsJsonAsync($"/api/profiles/{aliceProfile}/sessions", new { stageId = first });
        Assert.Equal(HttpStatusCode.NotFound, start.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync($"/api/profiles/{bobProfile}/progress")).StatusCode);
    }

    [Fact]
    public async Task Logic_subject_has_its_own_map_and_progress()
    {
        var (client, profileId) = await NewPlayerAsync();
        var map = await MapAsync(client, profileId, "logica");

        Assert.Equal(20, Stages(map).Count);
        Assert.Equal(4, map.GetProperty("progress").GetProperty("totalSkills").GetInt32());
        Assert.Equal("notStarted", map.GetProperty("progress").GetProperty("skills")[0].GetProperty("status").GetString());
    }
}
