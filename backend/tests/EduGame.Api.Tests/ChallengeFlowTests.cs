using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduGame.Domain.Players;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

/// <summary>Fluxos da Fase 2: nivelamento, Desafio do Guardião, histórico e medalhas.</summary>
[Collection(ApiCollection.Name)]
public class ChallengeFlowTests(ApiFactory factory)
{
    private async Task<(HttpClient Client, Guid ProfileId)> NewPlayerAsync(int? schoolGrade = 3)
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new { email = $"r-{Guid.NewGuid():N}@exemplo.com", password = "senha-segura-123", acceptTerms = true });
        var created = await client.PostAsJsonAsync("/api/profiles", new { nickname = "Teo", avatar = "🐼", schoolGrade });
        var profile = await created.Content.ReadFromJsonAsync<JsonElement>();
        return (client, profile.GetProperty("id").GetGuid());
    }

    private T WithDb<T>(Func<AppDbContext, T> action)
    {
        using var scope = factory.Services.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async Task<string> CorrectAnswerAsync(Guid problemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.SessionProblems.AsNoTracking().SingleAsync(p => p.Id == problemId)).Content.Answer;
    }

    /// <summary>Responde todos os problemas da sessão (certo ou errado) e devolve o resumo.</summary>
    private async Task<JsonElement> PlayAsync(HttpClient client, Guid sessionId, Func<int, bool> answerCorrectly)
    {
        for (var i = 0; ; i++)
        {
            var next = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next");
            if (next.GetProperty("done").GetBoolean()) break;
            var problem = next.GetProperty("problem");
            var id = problem.GetProperty("id").GetGuid();
            var answer = answerCorrectly(i) ? await CorrectAnswerAsync(id) : "resposta-errada";
            var res = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{id}/answer", new { answer });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(answerCorrectly(i), body.GetProperty("correct").GetBoolean());
        }
        return await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/summary");
    }

    private static async Task<Guid> StartAsync(HttpClient client, string url, object? body = null)
    {
        var res = await client.PostAsJsonAsync(url, body ?? new { });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid();
    }

    private static async Task<JsonElement> MapAsync(HttpClient client, Guid profileId, string subject = "matematica") =>
        await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/subjects/{subject}/map");

    [Fact]
    public async Task Placement_with_all_correct_answers_places_player_above_iniciante()
    {
        var (client, profileId) = await NewPlayerAsync(schoolGrade: 5);
        var map = await MapAsync(client, profileId);
        Assert.True(map.GetProperty("progress").GetProperty("placementAvailable").GetBoolean());

        var sessionId = await StartAsync(client, $"/api/profiles/{profileId}/subjects/matematica/placement");
        var first = (await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next")).GetProperty("problem");
        Assert.Equal("nivelamento", first.GetProperty("mode").GetString());
        Assert.Equal(0, first.GetProperty("hintsTotal").GetInt32()); // sem dicas no nivelamento

        var summary = await PlayAsync(client, sessionId, _ => true);

        Assert.Equal("avancado", summary.GetProperty("resultLevel").GetProperty("slug").GetString());
        map = await MapAsync(client, profileId);
        var progress = map.GetProperty("progress");
        Assert.Equal("avancado", progress.GetProperty("level").GetProperty("slug").GetString());
        Assert.False(progress.GetProperty("placementAvailable").GetBoolean());

        // Os níveis anteriores ficam abertos e a primeira etapa de cada nível também.
        var levels = map.GetProperty("levels").EnumerateArray().ToList();
        Assert.All(levels.Take(3), l => Assert.True(l.GetProperty("unlocked").GetBoolean()));
        Assert.True(levels[2].GetProperty("current").GetBoolean());
        var avancadoFirstStage = levels[2].GetProperty("regions")[0].GetProperty("stages")[0];
        Assert.True(avancadoFirstStage.GetProperty("unlocked").GetBoolean());
    }

    [Fact]
    public async Task Placement_with_wrong_answers_keeps_iniciante_and_cannot_be_repeated()
    {
        var (client, profileId) = await NewPlayerAsync();
        var sessionId = await StartAsync(client, $"/api/profiles/{profileId}/subjects/logica/placement");

        var summary = await PlayAsync(client, sessionId, _ => false);

        Assert.Equal("iniciante", summary.GetProperty("resultLevel").GetProperty("slug").GetString());
        var again = await client.PostAsJsonAsync($"/api/profiles/{profileId}/subjects/logica/placement", new { });
        Assert.Equal(HttpStatusCode.Forbidden, again.StatusCode);
    }

    [Fact]
    public async Task Skipping_placement_hides_the_offer()
    {
        var (client, profileId) = await NewPlayerAsync();
        var skip = await client.PostAsJsonAsync($"/api/profiles/{profileId}/subjects/matematica/placement/skip", new { });
        Assert.Equal(HttpStatusCode.NoContent, skip.StatusCode);

        var map = await MapAsync(client, profileId);
        Assert.False(map.GetProperty("progress").GetProperty("placementAvailable").GetBoolean());
    }

    [Fact]
    public async Task Guardian_is_locked_until_mastery_then_promotes_on_pass()
    {
        var (client, profileId) = await NewPlayerAsync();
        var locked = await client.PostAsJsonAsync($"/api/profiles/{profileId}/subjects/matematica/guardian", new { });
        Assert.Equal(HttpStatusCode.Forbidden, locked.StatusCode);

        // Simula o domínio de todas as habilidades do Iniciante.
        WithDb(db =>
        {
            var subjectId = db.Subjects.Single(s => s.Slug == "matematica").Id;
            foreach (var skill in db.Skills.Where(s => s.SubjectId == subjectId && s.Level!.Slug == "iniciante").ToList())
                db.SkillMasteries.Add(new SkillMastery { ProfileId = profileId, SkillId = skill.Id, Rating = 1050, AttemptsCount = 40, CorrectCount = 35, LastPracticedAt = DateTimeOffset.UtcNow });
            return db.SaveChanges();
        });

        var map = await MapAsync(client, profileId);
        Assert.True(map.GetProperty("progress").GetProperty("guardianAvailable").GetBoolean());
        Assert.Equal(100, map.GetProperty("progress").GetProperty("percent").GetInt32());

        var sessionId = await StartAsync(client, $"/api/profiles/{profileId}/subjects/matematica/guardian");
        var summary = await PlayAsync(client, sessionId, i => i != 0); // 11 de 12

        Assert.Equal("guardiao", summary.GetProperty("mode").GetString());
        Assert.True(summary.GetProperty("guardianPassed").GetBoolean());
        Assert.Equal("medio", summary.GetProperty("resultLevel").GetProperty("slug").GetString());
        Assert.Contains(summary.GetProperty("achievements").EnumerateArray(), a => a.GetProperty("slug").GetString() == "nivel-medio");

        map = await MapAsync(client, profileId);
        Assert.Equal("medio", map.GetProperty("progress").GetProperty("level").GetProperty("slug").GetString());
        var medio = map.GetProperty("levels")[1];
        Assert.True(medio.GetProperty("current").GetBoolean());
        Assert.True(medio.GetProperty("regions")[0].GetProperty("stages")[0].GetProperty("unlocked").GetBoolean());
    }

    [Fact]
    public async Task Failing_the_guardian_does_not_promote()
    {
        var (client, profileId) = await NewPlayerAsync();
        WithDb(db =>
        {
            var subjectId = db.Subjects.Single(s => s.Slug == "logica").Id;
            foreach (var skill in db.Skills.Where(s => s.SubjectId == subjectId && s.Level!.Slug == "iniciante").ToList())
                db.SkillMasteries.Add(new SkillMastery { ProfileId = profileId, SkillId = skill.Id, Rating = 1100, AttemptsCount = 40, CorrectCount = 35, LastPracticedAt = DateTimeOffset.UtcNow });
            return db.SaveChanges();
        });

        var sessionId = await StartAsync(client, $"/api/profiles/{profileId}/subjects/logica/guardian");
        var summary = await PlayAsync(client, sessionId, i => i < 6);

        Assert.False(summary.GetProperty("guardianPassed").GetBoolean());
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("resultLevel").ValueKind);
        var map = await MapAsync(client, profileId, "logica");
        Assert.Equal("iniciante", map.GetProperty("progress").GetProperty("level").GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Medio_stages_play_with_new_problem_types()
    {
        var (client, profileId) = await NewPlayerAsync();
        WithDb(db =>
        {
            var subject = db.Subjects.Single(s => s.Slug == "logica");
            var medio = db.Levels.Single(l => l.SubjectId == subject.Id && l.Slug == "medio");
            db.SubjectProgresses.Add(new SubjectProgress { ProfileId = profileId, SubjectId = subject.Id, CurrentLevelId = medio.Id, PlacementDone = true });
            return db.SaveChanges();
        });

        var map = await MapAsync(client, profileId, "logica");
        var gridStage = map.GetProperty("levels")[1].GetProperty("regions")[0].GetProperty("stages")[1];
        Assert.False(gridStage.GetProperty("unlocked").GetBoolean()); // precisa da anterior
        var firstMedio = map.GetProperty("levels")[1].GetProperty("regions")[0].GetProperty("stages")[0].GetProperty("id").GetInt32();
        var s1 = await StartAsync(client, $"/api/profiles/{profileId}/sessions", new { stageId = firstMedio });
        await PlayAsync(client, s1, _ => true);

        // Sudoku / quadrado mágico: a resposta é a grade inteira.
        var s2 = await StartAsync(client, $"/api/profiles/{profileId}/sessions", new { stageId = gridStage.GetProperty("id").GetInt32() });
        var problem = (await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{s2}/next")).GetProperty("problem");
        Assert.Equal("grade", problem.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Object, problem.GetProperty("grid").ValueKind);
        var summary = await PlayAsync(client, s2, _ => true);
        Assert.Equal(3, summary.GetProperty("stars").GetInt32());
    }

    [Fact]
    public async Task History_and_achievements_track_activity()
    {
        var (client, profileId) = await NewPlayerAsync();
        var first = (await MapAsync(client, profileId)).GetProperty("levels")[0].GetProperty("regions")[0].GetProperty("stages")[0].GetProperty("id").GetInt32();
        var sessionId = await StartAsync(client, $"/api/profiles/{profileId}/sessions", new { stageId = first });
        var summary = await PlayAsync(client, sessionId, _ => true);

        var slugs = summary.GetProperty("achievements").EnumerateArray().Select(a => a.GetProperty("slug").GetString()).ToList();
        Assert.Contains("primeiros-passos", slugs);
        Assert.Contains("tres-estrelas", slugs);
        Assert.Contains("sem-ajuda", slugs);

        var history = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/history?days=7");
        Assert.Equal(1, history.GetProperty("streak").GetInt32());
        var days = history.GetProperty("days").EnumerateArray().ToList();
        Assert.Equal(7, days.Count);
        Assert.Equal(8, days[^1].GetProperty("problems").GetInt32());

        var medals = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/achievements");
        Assert.Contains(medals.EnumerateArray(), m => m.GetProperty("slug").GetString() == "primeiros-passos" && m.GetProperty("earnedAt").ValueKind != JsonValueKind.Null);
        Assert.Contains(medals.EnumerateArray(), m => m.GetProperty("slug").GetString() == "maratonista" && m.GetProperty("earnedAt").ValueKind == JsonValueKind.Null);
    }
}
