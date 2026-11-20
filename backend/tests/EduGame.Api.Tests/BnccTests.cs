using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EduGame.Infrastructure.Seeding;

namespace EduGame.Api.Tests;

/// <summary>O alinhamento com a BNCC precisa cobrir todo o conteúdo e só usar códigos conhecidos.</summary>
public partial class BnccCatalogTests
{
    [GeneratedRegex(@"^(EF0[1-9](MA|CO)\d{2}|EM13MAT\d{3})$")]
    private static partial Regex CodeFormat();

    [Fact]
    public void Every_game_skill_has_an_explicit_bncc_entry()
    {
        var skills = ContentCatalog.Subjects.SelectMany(s => s.Skills).Select(s => s.Slug).ToHashSet();
        Assert.Empty(skills.Except(BnccCatalog.BySkill.Keys));
        Assert.Empty(BnccCatalog.BySkill.Keys.Except(skills)); // sem chaves de habilidades que não existem
    }

    [Fact]
    public void Every_code_is_well_formed_unique_and_used()
    {
        var codes = BnccCatalog.Habilidades.Select(h => h.Code).ToList();
        Assert.All(codes, c => Assert.Matches(CodeFormat(), c));
        Assert.Equal(codes.Count, codes.Distinct().Count());

        var used = BnccCatalog.BySkill.Values.SelectMany(c => c).ToHashSet();
        Assert.Empty(used.Except(codes));   // nenhum código sem descrição
        Assert.Empty(codes.Except(used));   // nenhuma descrição sem uso
        Assert.All(BnccCatalog.BySkill.Values, list => Assert.Equal(list.Length, list.Distinct().Count()));
    }

    [Fact]
    public void Math_skills_are_all_aligned()
    {
        var math = ContentCatalog.Subjects.Single(s => s.Slug == "matematica").Skills;
        Assert.All(math, s => Assert.NotEmpty(BnccCatalog.ForSkill(s.Slug)));
    }

    [Theory]
    [InlineData("EF03MA07", "3º ano", "Matemática", 3)]
    [InlineData("EF05CO03", "5º ano", "Computação", 5)]
    [InlineData("EM13MAT310", "Ensino Médio", "Matemática", 10)]
    public void Grade_and_component_come_from_the_code(string code, string grade, string component, int order)
    {
        var h = BnccCatalog.Habilidades.Single(x => x.Code == code);
        Assert.Equal(grade, h.Grade);
        Assert.Equal(component, h.Component);
        Assert.Equal(order, h.GradeOrder);
    }
}

[Collection(ApiCollection.Name)]
public class BnccApiTests(ApiFactory factory)
{
    [Fact]
    public async Task Curriculum_is_public_and_lists_bncc_per_skill()
    {
        var res = await factory.CreateClient().GetAsync("/api/curriculum");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var subjects = await res.Content.ReadFromJsonAsync<JsonElement>();

        var math = subjects.EnumerateArray().Single(s => s.GetProperty("slug").GetString() == "matematica");
        var skill = math.GetProperty("levels").EnumerateArray().SelectMany(l => l.GetProperty("skills").EnumerateArray())
            .Single(k => k.GetProperty("slug").GetString() == "multiplicacao-grupos");
        var bncc = skill.GetProperty("bncc").EnumerateArray().ToList();
        Assert.Contains(bncc, b => b.GetProperty("code").GetString() == "EF02MA07" && b.GetProperty("grade").GetString() == "2º ano");
        Assert.All(bncc, b => Assert.False(string.IsNullOrWhiteSpace(b.GetProperty("summary").GetString())));
    }

    [Fact]
    public async Task Player_progress_includes_bncc_codes()
    {
        var client = factory.CreateClient();
        var guest = await client.PostAsJsonAsync("/api/auth/guest", new { nickname = "Bncc", avatar = "🐼" });
        var me = await guest.Content.ReadFromJsonAsync<JsonElement>();
        var profileId = me.GetProperty("profiles")[0].GetProperty("id").GetGuid();

        var progress = await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profileId}/progress");
        var skills = progress.GetProperty("subjects").EnumerateArray().First(s => s.GetProperty("slug").GetString() == "matematica").GetProperty("skills");
        var counting = skills.EnumerateArray().Single(k => k.GetProperty("slug").GetString() == "contagem-comparacao");
        Assert.Contains(counting.GetProperty("bncc").EnumerateArray(), b => b.GetProperty("code").GetString() == "EF01MA04");
    }
}
