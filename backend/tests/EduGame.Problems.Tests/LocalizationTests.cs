using System.Text.RegularExpressions;

namespace EduGame.Problems.Tests;

/// <summary>Problemas em inglês e espanhol: bem formados, sem restos de português e com o mesmo sorteio do português.</summary>
public partial class LocalizationTests
{
    private const int Samples = 600;

    public static TheoryData<string, string> GeneratorsAndLanguages()
    {
        var data = new TheoryData<string, string>();
        foreach (var g in ProblemFactory.Default.Generators)
        foreach (var lang in new[] { Lang.En, Lang.Es })
            data.Add(g.Key, lang);
        return data;
    }

    public static TheoryData<string> GeneratorKeys()
    {
        var data = new TheoryData<string>();
        foreach (var g in ProblemFactory.Default.Generators) data.Add(g.Key);
        return data;
    }

    private static List<GeneratedProblem> Sample(string key, string lang, int seed = 777)
    {
        using var _ = Lang.Use(lang);
        var generator = ProblemFactory.Default.Get(key);
        var rng = new Random(seed);
        return [.. Enumerable.Range(0, Samples).Select(_ => generator.Generate(rng))];
    }

    private static IEnumerable<string> Texts(ProblemContent c) =>
        [c.Prompt, c.Explanation, .. c.Hints, .. c.Choices ?? [], c.Code ?? ""];

    // Letras que não existem em inglês (os nomes das listas em inglês não têm acento).
    [GeneratedRegex("[ãõçáéíóúâêôàñ¿¡]")]
    private static partial Regex NotEnglish();

    // Letras e palavras do português que não aparecem em espanhol.
    [GeneratedRegex(@"[ãõçâêô]|\b(não|você|então|Quanto|Quantos|Quantas|Qual|depois|dezenas|vezes|tem|há)\b")]
    private static partial Regex PortugueseOnly();

    [Theory, MemberData(nameof(GeneratorsAndLanguages))]
    public void Problems_are_translated_and_well_formed(string key, string lang)
    {
        foreach (var p in Sample(key, lang))
        {
            var c = p.Content;
            Assert.False(string.IsNullOrWhiteSpace(c.Prompt));
            Assert.NotEmpty(c.Hints);
            if (c.Choices is not null && c.Type != ProblemTypes.Ordenar) Assert.Contains(c.Answer, c.Choices);
            if (c.Type == ProblemTypes.VerdadeiroFalso)
            {
                using var _ = Lang.Use(lang);
                Assert.Equal([ProblemTypes.True, ProblemTypes.False], c.Choices!);
            }

            foreach (var text in Texts(c))
            {
                var leftover = lang == Lang.En ? NotEnglish().Match(text) : PortugueseOnly().Match(text);
                Assert.False(leftover.Success, $"{key} ({lang}): \"{leftover.Value}\" em: {text}");
            }
        }
    }

    [Theory, MemberData(nameof(GeneratorKeys))]
    public void Same_seed_gives_the_same_problem_in_every_language(string key)
    {
        // O desafio do dia usa a mesma semente para todos: o idioma não pode mudar o sorteio.
        var pt = Sample(key, Lang.Pt);
        foreach (var lang in new[] { Lang.En, Lang.Es })
        {
            var other = Sample(key, lang);
            for (var i = 0; i < pt.Count; i++)
            {
                Assert.Equal(pt[i].Difficulty, other[i].Difficulty);
                Assert.Equal(pt[i].Content.Type, other[i].Content.Type);
                if (pt[i].Content.Type is ProblemTypes.Numero or ProblemTypes.FracaoVisual)
                {
                    Assert.True(AnswerChecker.TryParseNumber(pt[i].Content.Answer, out var a));
                    Assert.True(AnswerChecker.TryParseNumber(other[i].Content.Answer, out var b));
                    Assert.Equal(a, b);
                }
            }
        }
    }

    [Fact]
    public void English_numbers_use_a_decimal_point()
    {
        using (Lang.Use(Lang.En)) Assert.Equal("2.5", AnswerChecker.FormatNumber(2.5m));
        using (Lang.Use(Lang.Es)) Assert.Equal("2,5", AnswerChecker.FormatNumber(2.5m));
        Assert.Equal("2,5", AnswerChecker.FormatNumber(2.5m));
    }

    [Theory]
    [InlineData("en-US", Lang.En)]
    [InlineData("es-419", Lang.Es)]
    [InlineData("pt-BR", Lang.Pt)]
    [InlineData("fr", Lang.Pt)]
    [InlineData("", Lang.Pt)]
    [InlineData(null, Lang.Pt)]
    public void Language_tags_are_normalized(string? tag, string expected) => Assert.Equal(expected, Lang.Normalize(tag));

    [Fact]
    public void Every_curated_problem_has_english_and_spanish_versions()
    {
        foreach (var def in CuratedCatalog.All)
        foreach (var lang in new[] { Lang.En, Lang.Es })
        {
            var t = CuratedCatalog.Translated(def.Slug, lang);
            Assert.True(t is not null, $"{def.Slug} sem tradução ({lang})");
            Assert.Equal(def.Content.Type, t.Type);
            Assert.Equal(def.Content.Hints.Count, t.Hints.Count);
            if (t.Choices is not null) Assert.Contains(t.Answer, t.Choices);
            if (def.Content.Type == ProblemTypes.Numero) Assert.Equal(def.Content.Answer, t.Answer);
        }
        Assert.Null(CuratedCatalog.Translated(CuratedCatalog.All[0].Slug, Lang.Pt));
    }
}
