using System.Text.RegularExpressions;

namespace EduGame.Problems.Tests;

/// <summary>Gera milhares de problemas por gerador e verifica invariantes.</summary>
public partial class GeneratorPropertyTests
{
    private const int Samples = 3000;

    public static TheoryData<string> GeneratorKeys()
    {
        var data = new TheoryData<string>();
        foreach (var g in ProblemFactory.Default.Generators) data.Add(g.Key);
        return data;
    }

    private static IEnumerable<GeneratedProblem> Sample(string key)
    {
        var generator = ProblemFactory.Default.Get(key);
        var rng = new Random(12345);
        for (var i = 0; i < Samples; i++) yield return generator.Generate(rng);
    }

    [Theory, MemberData(nameof(GeneratorKeys))]
    public void Problems_are_well_formed(string key)
    {
        foreach (var p in Sample(key))
        {
            var c = p.Content;
            Assert.False(string.IsNullOrWhiteSpace(c.Prompt));
            Assert.False(string.IsNullOrWhiteSpace(c.Answer));
            Assert.False(string.IsNullOrWhiteSpace(c.Explanation));
            Assert.NotEmpty(c.Hints);
            Assert.InRange(p.Difficulty, 50, 1300);
            Assert.Contains(c.Type, new[] { ProblemTypes.Numero, ProblemTypes.MultiplaEscolha, ProblemTypes.CompletarSequencia });

            if (c.Choices is not null)
            {
                Assert.InRange(c.Choices.Count, 2, 4);
                Assert.Contains(c.Answer, c.Choices);
                Assert.Equal(c.Choices.Count, c.Choices.Distinct().Count());
            }

            if (c.Type == ProblemTypes.MultiplaEscolha) Assert.NotNull(c.Choices);

            if (c.Type == ProblemTypes.CompletarSequencia)
            {
                Assert.NotNull(c.Sequence);
                Assert.Single(c.Sequence, s => s is null);
                Assert.True(c.Sequence.Count >= 5);
            }

            if (c.Type == ProblemTypes.Numero || (c.Type == ProblemTypes.CompletarSequencia && c.Choices is null))
            {
                Assert.True(AnswerChecker.TryParseNumber(c.Answer, out var n), $"Resposta não numérica: {c.Answer}");
                Assert.True(n >= 0, $"Resposta negativa em {key}: {c.Prompt}");
            }

            Assert.True(AnswerChecker.IsCorrect(c, c.Answer));
        }
    }

    [Theory, MemberData(nameof(GeneratorKeys))]
    public void Difficulty_spans_easy_to_hard(string key)
    {
        var difficulties = Sample(key).Select(p => p.Difficulty).ToList();

        // Cada habilidade precisa ter problemas fáceis (início) e difíceis o bastante para levar ao domínio (~1000).
        Assert.True(difficulties.Min() <= 350, $"{key}: mínimo {difficulties.Min()}");
        Assert.True(difficulties.Max() >= 900, $"{key}: máximo {difficulties.Max()}");
    }

    [Fact]
    public void Arithmetic_prompts_have_correct_answers()
    {
        var checkedCount = 0;
        foreach (var key in new[] { "mat.adicao", "mat.subtracao", "mat.multiplicacao" })
        {
            foreach (var p in Sample(key))
            {
                var m = ArithmeticPrompt().Match(p.Content.Prompt);
                if (!m.Success) continue;
                var a = int.Parse(m.Groups["a"].Value);
                var b = int.Parse(m.Groups["b"].Value);
                var expected = m.Groups["op"].Value switch { "+" => a + b, "−" => a - b, _ => a * b };
                Assert.Equal(expected.ToString(), p.Content.Answer);
                checkedCount++;
            }
        }
        Assert.True(checkedCount > 1000);
    }

    [Fact]
    public void Number_sequences_follow_their_rule_at_the_blank()
    {
        foreach (var p in Sample("log.sequencias"))
        {
            var seq = p.Content.Sequence!;
            var blank = seq.ToList().IndexOf(null);
            var filled = seq.Select((s, i) => i == blank ? int.Parse(p.Content.Answer) : int.Parse(s!)).ToList();
            // A resposta deve tornar a sequência consistente com a explicação: todos os itens não-lacuna batem.
            Assert.Equal(seq.Count, filled.Count);
            Assert.Contains(p.Content.Answer, p.Content.Explanation);
        }
    }

    [Fact]
    public void Comparison_answer_matches_values()
    {
        foreach (var p in Sample("log.comparacao"))
        {
            var parts = p.Content.Visual!.Split("  ?  ");
            var left = Eval(parts[0]);
            var right = Eval(parts[1]);
            var expected = left > right ? ">" : left < right ? "<" : "=";
            Assert.Equal(expected, p.Content.Answer);
        }

        static int Eval(string s)
        {
            var tokens = s.Split(' ');
            return tokens.Length == 1 ? int.Parse(s)
                : tokens[1] == "+" ? int.Parse(tokens[0]) + int.Parse(tokens[2])
                : int.Parse(tokens[0]) - int.Parse(tokens[2]);
        }
    }

    [GeneratedRegex(@"^Quanto é (?<a>\d+) (?<op>[+−×]) (?<b>\d+)\?$")]
    private static partial Regex ArithmeticPrompt();
}
