namespace EduGame.Problems.LogicSkills;

/// <summary>Maior, menor ou igual: escolher o sinal (&gt;, &lt;, =) entre números e expressões.</summary>
public sealed class ComparisonGenerator : ProblemGenerator
{
    public override string Key => "log.comparacao";
    public override string SkillSlug => "maior-menor-igual";

    private static readonly string[] Signs = [">", "<", "="];

    private sealed record Side(string Text, int Value)
    {
        public bool IsExpression => Text.Contains(' ');
    }

    public override GeneratedProblem Generate(Random rng)
    {
        var (left, right, difficulty) = rng.Next(6) switch
        {
            0 => Simple(rng, 1, 9, 200),
            1 => TwoDigits(rng),
            2 => Simple(rng, 100, 999, 550),
            3 => ExpressionVsNumber(rng),
            4 => ExpressionVsExpression(rng, 2, 9, 850),
            _ => ExpressionVsExpression(rng, 10, 50, 1050),
        };

        var sign = left.Value > right.Value ? ">" : left.Value < right.Value ? "<" : "=";
        if (sign == "=") difficulty += 30;

        var hints = new List<string> { "A \"boca\" do sinal fica aberta para o lado maior: 7 > 5 e 3 < 8." };
        if (left.IsExpression || right.IsExpression)
            hints.Insert(0, "Calcule primeiro o valor de cada lado.");

        var content = new ProblemContent(
            ProblemTypes.MultiplaEscolha,
            "Qual sinal completa a comparação?",
            sign,
            hints,
            $"{Describe(left)} e {Describe(right)}, então {left.Text} {sign} {right.Text}.")
        {
            Visual = $"{left.Text}  ?  {right.Text}",
            Choices = Signs,
        };
        return new(content, difficulty);
    }

    private static string Describe(Side s) => s.IsExpression ? $"{s.Text} = {s.Value}" : $"o número {s.Text}";

    private static Side Num(int n) => new(n.ToString(), n);

    private static (Side, Side, int) Simple(Random rng, int min, int max, int difficulty)
    {
        var a = Between(rng, min, max);
        var b = rng.Next(6) == 0 ? a : Between(rng, min, max);
        return (Num(a), Num(b), difficulty);
    }

    private static (Side, Side, int) TwoDigits(Random rng)
    {
        int t = Between(rng, 1, 9), u = Between(rng, 1, 9);
        return rng.Next(3) switch
        {
            0 when t != u => (Num(t * 10 + u), Num(u * 10 + t), 450), // algarismos trocados
            1 => (Num(t * 10 + u), Num(t * 10 + Between(rng, 0, 9)), 400), // mesma dezena
            _ => (Num(Between(rng, 10, 99)), Num(Between(rng, 10, 99)), 320),
        };
    }

    private static (Side, Side, int) ExpressionVsNumber(Random rng)
    {
        var a = Between(rng, 1, 9);
        var b = Between(rng, 1, 9);
        var expression = new Side($"{a} + {b}", a + b);
        var number = Num(a + b + Between(rng, -2, 2));
        return rng.Next(2) == 0 ? (expression, number, 650) : (number, expression, 650);
    }

    private static (Side, Side, int) ExpressionVsExpression(Random rng, int min, int max, int difficulty)
    {
        Side Make()
        {
            var a = Between(rng, min, max);
            var b = Between(rng, min, max);
            return rng.Next(2) == 0 || a == b
                ? new($"{a} + {b}", a + b)
                : new($"{Math.Max(a, b)} − {Math.Min(a, b)}", Math.Abs(a - b));
        }

        var left = Make();
        var right = Make();
        // Às vezes força igualdade com contas diferentes (ex.: 2 + 5 e 3 + 4).
        if (rng.Next(4) == 0 && left.Value > 1)
        {
            var x = Between(rng, 1, left.Value - 1);
            right = new($"{x} + {left.Value - x}", left.Value);
        }
        var hasSubtraction = left.Text.Contains('−') || right.Text.Contains('−');
        return (left, right, difficulty + (hasSubtraction ? 80 : 0));
    }
}
