namespace EduGame.Problems.LogicSkills;

/// <summary>Sequências numéricas: aritméticas, decrescentes, alternadas, dobro, passos crescentes.</summary>
public sealed class NumberSequenceGenerator : ProblemGenerator
{
    public override string Key => "log.sequencias";
    public override string SkillSlug => "sequencias-simples";

    private sealed record Rule(string Description, int Difficulty, bool Linear, Func<Random, int[]> Build);

    private static int[] Arithmetic(int start, int step, int count = 7) =>
        [.. Enumerable.Range(0, count).Select(i => start + i * step)];

    private static readonly Rule[] Rules =
    [
        new("somar 1 a cada passo", 150, true, r => Arithmetic(Between(r, 1, 20), 1)),
        new("somar 2 a cada passo", 250, true, r => Arithmetic(Between(r, 0, 10), 2)),
        new("somar 5 a cada passo", 300, true, r => Arithmetic(5 * Between(r, 0, 4), 5)),
        new("somar 10 a cada passo", 350, true, r => Arithmetic(Between(r, 1, 20), 10)),
        new("diminuir 1 a cada passo", 350, true, r => Arithmetic(Between(r, 10, 30), -1)),
        new("somar 3 a cada passo", 450, true, r => Arithmetic(Between(r, 0, 10), 3)),
        new("diminuir 2 a cada passo", 450, true, r => Arithmetic(Between(r, 14, 30), -2)),
        new("somar 4 a cada passo", 520, true, r => Arithmetic(Between(r, 0, 8), 4)),
        new("diminuir 5 a cada passo", 500, true, r => Arithmetic(5 * Between(r, 7, 12), -5)),
        new("diminuir 3 a cada passo", 600, true, r => Arithmetic(Between(r, 20, 35), -3)),
        new("somar 1, depois somar 2, e repetir", 750, false, r => Alternating(Between(r, 1, 10), 1, 2)),
        new("somar 2, depois tirar 1, e repetir", 850, false, r => Alternating(Between(r, 1, 10), 2, -1)),
        new("dobrar o número a cada passo", 800, false, r => Doubling(Between(r, 1, 3))),
        new("somar 1, depois 2, depois 3… (o passo cresce)", 950, false, r => Growing(Between(r, 1, 5))),
    ];

    private static int[] Alternating(int start, int a, int b)
    {
        var list = new List<int> { start };
        for (var i = 1; i < 7; i++) list.Add(list[^1] + (i % 2 == 1 ? a : b));
        return [.. list];
    }

    private static int[] Doubling(int start) => [.. Enumerable.Range(0, 6).Select(i => start << i)];

    private static int[] Growing(int start)
    {
        var list = new List<int> { start };
        for (var i = 1; i < 7; i++) list.Add(list[^1] + i);
        return [.. list];
    }

    public override GeneratedProblem Generate(Random rng)
    {
        var rule = Pick(rng, Rules);
        var numbers = rule.Build(rng);

        // Lacuna no fim (padrão) ou no meio (só para regras lineares, fica um pouco mais difícil).
        var blankInMiddle = rule.Linear && rng.Next(3) == 0;
        var shown = Math.Min(numbers.Length, 6);
        var blank = blankInMiddle ? Between(rng, 2, shown - 2) : shown - 1;
        var answer = numbers[blank];
        var sequence = numbers.Take(shown).Select((n, i) => i == blank ? null : n.ToString()).ToList();

        var content = new ProblemContent(
            ProblemTypes.CompletarSequencia,
            "Qual número completa a sequência?",
            answer.ToString(),
            ["Compare cada número com o seguinte: quanto ele aumenta ou diminui?", $"A regra é: {rule.Description}."],
            $"A regra é {rule.Description}, então o número que falta é {answer}.")
        { Sequence = sequence };
        return new(content, rule.Difficulty + (blankInMiddle ? 80 : 0));
    }
}
