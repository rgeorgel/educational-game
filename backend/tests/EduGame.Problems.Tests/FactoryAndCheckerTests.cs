namespace EduGame.Problems.Tests;

public class FactoryAndCheckerTests
{
    [Fact]
    public void Same_seed_produces_same_problem()
    {
        var a = ProblemFactory.Default.Generate("mat.adicao", 500, seed: 42);
        var b = ProblemFactory.Default.Generate("mat.adicao", 500, seed: 42);

        Assert.Equal(a.Content.Prompt, b.Content.Prompt);
        Assert.Equal(a.Difficulty, b.Difficulty);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(600)]
    [InlineData(950)]
    public void Picks_problems_close_to_target_difficulty(int target)
    {
        var distances = Enumerable.Range(0, 200)
            .Select(seed => Math.Abs(ProblemFactory.Default.Generate("mat.adicao", target, seed).Difficulty - target))
            .ToList();

        Assert.True(distances.Average() < 120, $"distância média {distances.Average()}");
    }

    [Fact]
    public void Avoids_repeated_prompts_when_possible()
    {
        var first = ProblemFactory.Default.Generate("log.comparacao", 200, seed: 7);
        var avoid = new HashSet<string> { ProblemFactory.Fingerprint(first.Content) };

        var second = ProblemFactory.Default.Generate("log.comparacao", 200, seed: 7, avoid);

        Assert.NotEqual(ProblemFactory.Fingerprint(first.Content), ProblemFactory.Fingerprint(second.Content));
    }

    [Fact]
    public void Every_generator_key_is_unique_and_resolvable()
    {
        var keys = ProblemFactory.Default.Generators.Select(g => g.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.All(keys, k => Assert.NotNull(ProblemFactory.Default.Get(k)));
    }

    [Theory]
    [InlineData("12", "12", true)]
    [InlineData("12", " 12 ", true)]
    [InlineData("12", "12,0", true)]
    [InlineData("12", "12.0", true)]
    [InlineData("1000", "1 000", true)]
    [InlineData("12", "13", false)]
    [InlineData("12", "doze", false)]
    [InlineData("12", "", false)]
    public void Numeric_answers_are_normalized(string expected, string given, bool correct)
    {
        var problem = new ProblemContent(ProblemTypes.Numero, "?", expected, ["dica"], "explicação");
        Assert.Equal(correct, AnswerChecker.IsCorrect(problem, given));
    }

    [Fact]
    public void Choice_answers_must_match_exactly()
    {
        var problem = new ProblemContent(ProblemTypes.MultiplaEscolha, "?", ">", ["dica"], "explicação") { Choices = [">", "<", "="] };
        Assert.True(AnswerChecker.IsCorrect(problem, ">"));
        Assert.False(AnswerChecker.IsCorrect(problem, "<"));
    }
}

public class CuratedCatalogTests
{
    [Fact]
    public void Curated_problems_load_and_are_well_formed()
    {
        var all = CuratedCatalog.All;
        Assert.True(all.Count >= 15);
        Assert.Equal(all.Count, all.Select(p => p.Slug).Distinct().Count());
        foreach (var p in all)
        {
            var c = p.Content;
            Assert.Contains(c.Type, ProblemTypes.All);
            Assert.False(string.IsNullOrWhiteSpace(c.Prompt));
            Assert.NotEmpty(c.Hints);
            Assert.False(string.IsNullOrWhiteSpace(c.Explanation));
            Assert.InRange(p.Difficulty, 2000, 2700);
            if (c.Choices is not null) Assert.Contains(c.Answer, c.Choices);
            Assert.True(AnswerChecker.IsCorrect(c, c.Answer), p.Slug);
        }
    }
}

public class KnightsKnavesTests
{
    [Fact]
    public void Every_generated_puzzle_has_a_unique_consistent_answer()
    {
        // O gerador só devolve quando a solução é única; aqui garantimos que ele termina e varia.
        var g = new LogicSkills.KnightsKnavesGenerator();
        var rng = new Random(9);
        var prompts = Enumerable.Range(0, 300).Select(_ => g.Generate(rng).Content.Code).Distinct().Count();
        Assert.True(prompts > 20);
    }
}
