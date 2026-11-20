using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EduGame.Api.Tests;

[Collection(ApiCollection.Name)]
public class HealthAndContentTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_reports_database_healthy()
    {
        var client = factory.CreateClient();

        var res = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Subjects_are_seeded_with_four_levels()
    {
        var client = factory.CreateClient();

        var subjects = await client.GetFromJsonAsync<JsonElement>("/api/subjects");

        var slugs = subjects.EnumerateArray().Select(s => s.GetProperty("slug").GetString()).ToList();
        Assert.Equal(["matematica", "logica"], slugs);
        foreach (var s in subjects.EnumerateArray())
        {
            var levels = s.GetProperty("levels").EnumerateArray().Select(l => l.GetProperty("slug").GetString());
            Assert.Equal(["iniciante", "medio", "avancado", "expert"], levels);
        }
    }
}
