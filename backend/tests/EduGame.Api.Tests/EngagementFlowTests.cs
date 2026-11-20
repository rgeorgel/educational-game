using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduGame.Domain.Players;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

/// <summary>Fluxos da Fase 3: Expert, desafio diário, revisão, avatar, painel do responsável e turmas.</summary>
[Collection(ApiCollection.Name)]
public class EngagementFlowTests(ApiFactory factory)
{
    private async Task<(HttpClient Client, Guid ProfileId)> NewPlayerAsync(string nickname = "Rui")
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new { email = $"r-{Guid.NewGuid():N}@exemplo.com", password = "senha-segura-123", acceptTerms = true });
        var created = await client.PostAsJsonAsync("/api/profiles", new { nickname, avatar = "🐸" });
        return (client, (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
    }

    private void WithDb(Action<AppDbContext> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        action(db);
        db.SaveChanges();
    }

    private void SetLevel(Guid profileId, string subject, string level, int xp = 0) => WithDb(db =>
    {
        var s = db.Subjects.Single(x => x.Slug == subject);
        var l = db.Levels.Single(x => x.SubjectId == s.Id && x.Slug == level);
        db.SubjectProgresses.Add(new SubjectProgress { ProfileId = profileId, SubjectId = s.Id, CurrentLevelId = l.Id, PlacementDone = true, Xp = xp });
    });

    private async Task<string> CorrectAnswerAsync(Guid problemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.SessionProblems.AsNoTracking().SingleAsync(p => p.Id == problemId)).Content.Answer;
    }

    private static async Task<Guid> StartAsync(HttpClient client, string url, object? body = null)
    {
        var res = await client.PostAsJsonAsync(url, body ?? new { });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid();
    }

    private async Task<JsonElement> PlayAsync(HttpClient client, Guid sessionId, bool correct = true)
    {
        while (true)
        {
            var next = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next");
            if (next.GetProperty("done").GetBoolean()) break;
            var id = next.GetProperty("problem").GetProperty("id").GetGuid();
            var answer = correct ? await CorrectAnswerAsync(id) : "x";
            var res = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{id}/answer", new { answer });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
        return await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/summary");
    }

    [Fact]
    public async Task Daily_challenge_is_the_same_for_players_of_a_level_and_once_per_day()
    {
        var (a, aId) = await NewPlayerAsync();
        var (b, bId) = await NewPlayerAsync();
        SetLevel(aId, "matematica", "medio");
        SetLevel(bId, "matematica", "medio");

        var sa = await StartAsync(a, $"/api/profiles/{aId}/subjects/matematica/daily");
        var sb = await StartAsync(b, $"/api/profiles/{bId}/subjects/matematica/daily");
        var pa = (await a.GetFromJsonAsync<JsonElement>($"/api/sessions/{sa}/next")).GetProperty("problem");
        var pb = (await b.GetFromJsonAsync<JsonElement>($"/api/sessions/{sb}/next")).GetProperty("problem");
        Assert.Equal("diario", pa.GetProperty("mode").GetString());
        Assert.Equal(pa.GetProperty("prompt").GetString(), pb.GetProperty("prompt").GetString());
        Assert.Equal(1, pa.GetProperty("total").GetInt32());

        // Começar de novo antes de terminar retoma a mesma sessão.
        Assert.Equal(sa, await StartAsync(a, $"/api/profiles/{aId}/subjects/matematica/daily"));

        var answer = await a.PostAsJsonAsync($"/api/sessions/{sa}/problems/{pa.GetProperty("id").GetGuid()}/answer", new { answer = await CorrectAnswerAsync(pa.GetProperty("id").GetGuid()) });
        var result = await answer.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("sessionFinished").GetBoolean());
        Assert.True(result.GetProperty("xpEarned").GetInt32() >= 20); // XP em dobro

        var again = await a.PostAsJsonAsync($"/api/profiles/{aId}/subjects/matematica/daily", new { });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        var status = await a.GetFromJsonAsync<JsonElement>($"/api/profiles/{aId}/daily");
        var math = status.EnumerateArray().Single(s => s.GetProperty("slug").GetString() == "matematica");
        Assert.True(math.GetProperty("done").GetBoolean());
        Assert.True(math.GetProperty("correct").GetBoolean());
    }

    [Fact]
    public async Task Review_needs_practice_first_then_targets_practiced_skills()
    {
        var (client, profileId) = await NewPlayerAsync();
        var none = await client.PostAsJsonAsync($"/api/profiles/{profileId}/subjects/logica/review", new { });
        Assert.Equal(HttpStatusCode.Forbidden, none.StatusCode);

        var map = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/subjects/logica/map");
        var first = map.GetProperty("levels")[0].GetProperty("regions")[0].GetProperty("stages")[0].GetProperty("id").GetInt32();
        await PlayAsync(client, await StartAsync(client, $"/api/profiles/{profileId}/sessions", new { stageId = first }));

        map = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/subjects/logica/map");
        Assert.True(map.GetProperty("progress").GetProperty("reviewAvailable").GetBoolean());

        var review = await StartAsync(client, $"/api/profiles/{profileId}/subjects/logica/review");
        var problem = (await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{review}/next")).GetProperty("problem");
        Assert.Equal("revisao", problem.GetProperty("mode").GetString());
        Assert.Equal("padroes-visuais", problem.GetProperty("skill").GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Expert_curated_stage_serves_handwritten_problems_without_repeating()
    {
        var (client, profileId) = await NewPlayerAsync();
        SetLevel(profileId, "logica", "expert");
        WithDb(db =>
        {
            // Libera a etapa de desafios (a anterior precisa de estrela).
            var stages = db.Stages.Include(s => s.Region).Where(s => s.Region!.Slug == "laboratorio-do-programador").OrderBy(s => s.Order).ToList();
            db.StageProgresses.Add(new StageProgress { ProfileId = profileId, StageId = stages[0].Id, BestStars = 1, TimesCompleted = 1 });
        });

        var map = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/subjects/logica/map");
        var expert = map.GetProperty("levels")[3];
        Assert.True(expert.GetProperty("current").GetBoolean());
        var curatedStage = expert.GetProperty("regions")[1].GetProperty("stages")[1];
        Assert.True(curatedStage.GetProperty("unlocked").GetBoolean());

        var sessionId = await StartAsync(client, $"/api/profiles/{profileId}/sessions", new { stageId = curatedStage.GetProperty("id").GetInt32() });
        await PlayAsync(client, sessionId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var served = await db.SessionProblems.Where(p => p.SessionId == sessionId).ToListAsync();
        Assert.All(served, p => Assert.Equal("curado", p.GeneratorKey));
        Assert.Equal(served.Count, served.Select(p => p.Seed).Distinct().Count());
    }

    [Fact]
    public async Task Avatar_items_require_enough_xp()
    {
        var (client, profileId) = await NewPlayerAsync();
        var locked = await client.PutAsJsonAsync($"/api/profiles/{profileId}/avatar", new { avatar = "🦉", accessory = "coroa", color = "padrao" });
        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);

        SetLevel(profileId, "matematica", "iniciante", xp: 3000);
        var options = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/avatar");
        Assert.True(options.GetProperty("accessories").EnumerateArray().All(a => a.GetProperty("unlocked").GetBoolean()));

        var ok = await client.PutAsJsonAsync($"/api/profiles/{profileId}/avatar", new { avatar = "🦉", accessory = "coroa", color = "ouro" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var profile = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("👑", profile.GetProperty("accessoryIcon").GetString());
    }

    [Fact]
    public async Task Guardian_report_lists_only_own_profiles()
    {
        var (client, profileId) = await NewPlayerAsync("Ana");
        await client.PostAsJsonAsync("/api/profiles", new { nickname = "Beto", avatar = "🐼" });
        await NewPlayerAsync("Outro");

        var report = await client.GetFromJsonAsync<JsonElement>("/api/guardian/report");
        var names = report.EnumerateArray().Select(r => r.GetProperty("nickname").GetString()).ToList();
        Assert.Equal(["Ana", "Beto"], names);
        Assert.Equal(2, report[0].GetProperty("subjects").GetArrayLength());
    }

    [Fact]
    public async Task Classroom_flow_with_guardian_consent()
    {
        var (teacher, _) = await NewPlayerAsync("Prof");
        var (guardian, kidId) = await NewPlayerAsync("Kiko");
        var (stranger, _) = await NewPlayerAsync("Zé");

        var created = await teacher.PostAsJsonAsync("/api/classrooms", new { name = "5º ano B" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var classroom = await created.Content.ReadFromJsonAsync<JsonElement>();
        var classId = classroom.GetProperty("id").GetGuid();
        var code = classroom.GetProperty("code").GetString()!;
        Assert.Equal(8, code.Length);

        var wrong = await guardian.PostAsJsonAsync($"/api/profiles/{kidId}/classrooms", new { code = "ZZZZZZZZ" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        var join = await guardian.PostAsJsonAsync($"/api/profiles/{kidId}/classrooms", new { code = code.ToLowerInvariant() });
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        var report = await teacher.GetFromJsonAsync<JsonElement>($"/api/classrooms/{classId}/report");
        Assert.Equal("Kiko", report.GetProperty("students")[0].GetProperty("nickname").GetString());
        Assert.DoesNotContain("@exemplo.com", report.GetRawText()); // nada de e-mail do responsável

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/classrooms/{classId}/report")).StatusCode);

        var leave = await guardian.DeleteAsync($"/api/profiles/{kidId}/classrooms/{classId}");
        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
        report = await teacher.GetFromJsonAsync<JsonElement>($"/api/classrooms/{classId}/report");
        Assert.Equal(0, report.GetProperty("students").GetArrayLength());
    }
}
