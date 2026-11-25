using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EduGame.Api.Tests;

/// <summary>O cabeçalho X-Lang escolhe o idioma dos textos da API (o banco continua em português).</summary>
[Collection(ApiCollection.Name)]
public class LanguageTests(ApiFactory factory)
{
    private HttpClient Client(string? lang)
    {
        var client = factory.CreateClient();
        if (lang is not null) client.DefaultRequestHeaders.Add("X-Lang", lang);
        return client;
    }

    private static async Task<(string Subject, string Level)> FirstNamesAsync(HttpClient client)
    {
        var subjects = await client.GetFromJsonAsync<JsonElement>("/api/subjects");
        var first = subjects[0];
        return (first.GetProperty("name").GetString()!, first.GetProperty("levels")[1].GetProperty("name").GetString()!);
    }

    [Theory]
    [InlineData(null, "Matemática", "Médio")]
    [InlineData("pt", "Matemática", "Médio")]
    [InlineData("en", "Math", "Intermediate")]
    [InlineData("es", "Matemáticas", "Intermedio")]
    public async Task Catalog_names_follow_the_language(string? lang, string subject, string level)
    {
        var names = await FirstNamesAsync(Client(lang));
        Assert.Equal((subject, level), names);
    }

    [Fact]
    public async Task Curriculum_is_translated_and_varies_by_language()
    {
        var res = await Client("es").GetAsync("/api/curriculum");
        Assert.Contains("X-Lang", res.Headers.Vary);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var skill = body[0].GetProperty("levels")[0].GetProperty("skills")[0];
        Assert.Equal("Contar y comparar", skill.GetProperty("name").GetString());
        var bncc = skill.GetProperty("bncc")[0];
        Assert.Equal("1.º año", bncc.GetProperty("grade").GetString());
    }

    [Fact]
    public async Task Error_messages_follow_the_language()
    {
        var res = await Client("en").PostAsJsonAsync("/api/auth/login", new { email = "ninguem@example.com", password = "senha-errada-123" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Wrong email or password.", body.GetProperty("detail").GetString());
    }
}
