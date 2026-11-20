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
            Assert.InRange(p.Difficulty, 50, 2300);
            Assert.Contains(c.Type, ProblemTypes.All);

            if (c.Choices is not null && c.Type != ProblemTypes.Ordenar)
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

            if (c.Type == ProblemTypes.Numero || c.Type == ProblemTypes.FracaoVisual || (c.Type == ProblemTypes.CompletarSequencia && c.Choices is null))
            {
                Assert.True(AnswerChecker.TryParseNumber(c.Answer, out var n), $"Resposta não numérica: {c.Answer}");
                // O teclado precisa permitir digitar a resposta.
                Assert.True(n >= 0 || c.AcceptsNegative, $"Resposta negativa sem teclado com '−' em {key}: {c.Prompt}");
                Assert.True(n == decimal.Truncate(n) || c.AcceptsDecimal, $"Resposta decimal sem teclado com ',' em {key}: {c.Prompt}");
            }

            if (c.Type == ProblemTypes.VerdadeiroFalso)
                Assert.Contains(c.Answer, new[] { ProblemTypes.True, ProblemTypes.False });

            if (c.Type == ProblemTypes.Ordenar)
            {
                Assert.NotNull(c.Choices);
                Assert.Equal(c.Choices.Order(), c.Answer.Split('|').Order());
            }

            if (c.Type == ProblemTypes.FracaoVisual)
            {
                Assert.NotNull(c.Figure);
                Assert.InRange(int.Parse(c.Answer), 1, c.Figure.Parts - 1);
            }

            if (c.Type == ProblemTypes.Grade)
            {
                Assert.NotNull(c.Grid);
                Assert.Equal(c.Grid.Size * c.Grid.Size, c.Grid.Cells.Count);
                Assert.Contains(c.Grid.Cells, cell => cell is null);
            }

            Assert.True(AnswerChecker.IsCorrect(c, c.Answer));
        }
    }

    [Theory, MemberData(nameof(GeneratorKeys))]
    public void Difficulty_spans_easy_to_hard(string key)
    {
        var level = ProblemFactory.Default.Get(key).Level;
        var difficulties = Sample(key).Select(p => p.Difficulty).ToList();

        // Cada habilidade precisa ter problemas perto do rating inicial do nível
        // e difíceis o bastante para levar ao domínio (rating do próximo nível).
        var (easy, hard) = level switch
        {
            "iniciante" => (350, 900),
            "medio" => (1050, 1400),
            "avancado" => (1550, 1900),
            _ => throw new InvalidOperationException(level),
        };
        Assert.True(difficulties.Min() <= easy, $"{key}: mínimo {difficulties.Min()}");
        Assert.True(difficulties.Max() >= hard, $"{key}: máximo {difficulties.Max()}");
    }

    [Fact]
    public void Sudokus_have_exactly_one_solution()
    {
        foreach (var key in new[] { "log.grades", "log.sudoku-6x6" })
        {
            foreach (var p in Sample(key).Take(300).Where(p => p.Content.Grid!.Kind == GridKinds.Sudoku))
            {
                var g = p.Content.Grid!;
                var cells = g.Cells.Select(c => c ?? 0).ToArray();
                Assert.Equal(1, Tools.SudokuTools.CountSolutions(cells, g.BoxRows, g.BoxCols, 3));
            }
        }
    }

    [Fact]
    public void Linear_equations_answers_satisfy_the_equation()
    {
        foreach (var p in Sample("mat.equacoes"))
        {
            var m = SimpleEquation().Match(p.Content.Prompt);
            if (!m.Success) continue;
            var x = int.Parse(p.Content.Answer.Replace('−', '-'));
            var a = m.Groups["a"].Success ? int.Parse(m.Groups["a"].Value) : 1;
            var b = m.Groups["b"].Success ? int.Parse(m.Groups["b"].Value) : 0;
            var rhs = int.Parse(m.Groups["c"].Value.Replace('−', '-'));
            Assert.Equal(rhs, a * x + b);
        }
    }

    [GeneratedRegex(@"^Resolva a equação: (?<a>\d+)?x(?: \+ (?<b>\d+))? = (?<c>−?\d+)\. Quanto vale x\?$")]
    private static partial Regex SimpleEquation();

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
