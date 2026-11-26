using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

/// <summary>Menu de jogos: escolher um jogo (habilidade) e jogar fora da trilha.</summary>
[Collection(ApiCollection.Name)]
public class GameMenuTests(ApiFactory factory)
{
    private async Task<(HttpClient Client, Guid ProfileId)> NewPlayerAsync()
    {
        var client = factory.CreateClient();
        var email = $"jogos-{Guid.NewGuid():N}@exemplo.com";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha-segura-123", acceptTerms = true });
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);
        var created = await client.PostAsJsonAsync("/api/profiles", new { nickname = "Luna", avatar = "🦊", schoolGrade = 3 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (client, (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
    }

    private async Task<string> CorrectAnswerAsync(Guid problemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.SessionProblems.AsNoTracking().SingleAsync(p => p.Id == problemId)).Content.Answer;
    }

    [Fact]
    public async Task Lists_every_game_with_its_level()
    {
        var (client, profileId) = await NewPlayerAsync();
        var res = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/games");

        var games = res.GetProperty("games").EnumerateArray().ToList();
        Assert.True(games.Count >= 40);
        Assert.Equal(8, res.GetProperty("problemsPerGame").GetInt32());
        Assert.Equal(2, res.GetProperty("subjects").GetArrayLength());

        var sudoku = games.Single(g => g.GetProperty("slug").GetString() == "sudoku-6x6");
        Assert.Equal("logica", sudoku.GetProperty("subjectSlug").GetString());
        Assert.Equal("avancado", sudoku.GetProperty("level").GetProperty("slug").GetString());
        Assert.True(sudoku.GetProperty("aboveLevel").GetBoolean());
        Assert.Equal("notStarted", sudoku.GetProperty("status").GetString());

        var counting = games.Single(g => g.GetProperty("slug").GetString() == "contagem-comparacao");
        Assert.True(counting.GetProperty("currentLevel").GetBoolean());
        Assert.False(counting.GetProperty("aboveLevel").GetBoolean());
    }

    [Fact]
    public async Task Free_play_serves_only_the_chosen_game_and_keeps_the_trail_untouched()
    {
        var (client, profileId) = await NewPlayerAsync();

        // Sudoku 6×6 é do nível Avançado, mas no menu de jogos qualquer jogo pode ser jogado.
        var start = await client.PostAsync($"/api/profiles/{profileId}/subjects/logica/games/sudoku-6x6", null);
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        var started = await start.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("livre", started.GetProperty("mode").GetString());
        var sessionId = started.GetProperty("sessionId").GetGuid();

        for (var i = 0; i < 8; i++)
        {
            var next = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next");
            var problem = next.GetProperty("problem");
            Assert.Equal("sudoku-6x6", problem.GetProperty("skill").GetProperty("slug").GetString());
            Assert.Equal("livre", problem.GetProperty("mode").GetString());
            var id = problem.GetProperty("id").GetGuid();
            var answer = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{id}/answer", new { answer = await CorrectAnswerAsync(id) });
            Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        }

        var summary = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/summary");
        Assert.True(summary.GetProperty("finished").GetBoolean());
        Assert.Equal(3, summary.GetProperty("stars").GetInt32());
        Assert.Equal("sudoku-6x6", summary.GetProperty("game").GetProperty("slug").GetString());

        // O jogo aparece como jogado no menu…
        var games = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/games");
        var sudoku = games.GetProperty("games").EnumerateArray().Single(g => g.GetProperty("slug").GetString() == "sudoku-6x6");
        Assert.Equal(8, sudoku.GetProperty("attempts").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, sudoku.GetProperty("lastPlayedAt").ValueKind);

        // …mas a trilha continua como antes: mesmo nível e o nivelamento ainda disponível.
        var map = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/subjects/logica/map");
        var progress = map.GetProperty("progress");
        Assert.Equal("iniciante", progress.GetProperty("level").GetProperty("slug").GetString());
        Assert.True(progress.GetProperty("placementAvailable").GetBoolean());
    }

    [Fact]
    public async Task Unknown_game_returns_404()
    {
        var (client, profileId) = await NewPlayerAsync();
        var wrongSubject = await client.PostAsync($"/api/profiles/{profileId}/subjects/matematica/games/sudoku-6x6", null);
        Assert.Equal(HttpStatusCode.NotFound, wrongSubject.StatusCode);
        var unknown = await client.PostAsync($"/api/profiles/{profileId}/subjects/logica/games/nao-existe", null);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
