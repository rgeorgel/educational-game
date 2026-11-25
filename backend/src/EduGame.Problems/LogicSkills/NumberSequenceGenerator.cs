using static EduGame.Problems.Lang;

namespace EduGame.Problems.LogicSkills;

/// <summary>Sequências numéricas: aritméticas, decrescentes, alternadas, dobro, passos crescentes.</summary>
public sealed class NumberSequenceGenerator : ProblemGenerator
{
    public override string Key => "log.sequencias";
    public override string SkillSlug => "sequencias-simples";

    private sealed record Rule(Text Description, int Difficulty, bool Linear, Func<Random, int[]> Build);

    private static int[] Arithmetic(int start, int step, int count = 7) =>
        [.. Enumerable.Range(0, count).Select(i => start + i * step)];

    private static readonly Rule[] Rules =
    [
        new(new("somar 1 a cada passo", "add 1 each step", "sumar 1 en cada paso"), 150, true, r => Arithmetic(Between(r, 1, 20), 1)),
        new(new("somar 2 a cada passo", "add 2 each step", "sumar 2 en cada paso"), 250, true, r => Arithmetic(Between(r, 0, 10), 2)),
        new(new("somar 5 a cada passo", "add 5 each step", "sumar 5 en cada paso"), 300, true, r => Arithmetic(5 * Between(r, 0, 4), 5)),
        new(new("somar 10 a cada passo", "add 10 each step", "sumar 10 en cada paso"), 350, true, r => Arithmetic(Between(r, 1, 20), 10)),
        new(new("diminuir 1 a cada passo", "subtract 1 each step", "restar 1 en cada paso"), 350, true, r => Arithmetic(Between(r, 10, 30), -1)),
        new(new("somar 3 a cada passo", "add 3 each step", "sumar 3 en cada paso"), 450, true, r => Arithmetic(Between(r, 0, 10), 3)),
        new(new("diminuir 2 a cada passo", "subtract 2 each step", "restar 2 en cada paso"), 450, true, r => Arithmetic(Between(r, 14, 30), -2)),
        new(new("somar 4 a cada passo", "add 4 each step", "sumar 4 en cada paso"), 520, true, r => Arithmetic(Between(r, 0, 8), 4)),
        new(new("diminuir 5 a cada passo", "subtract 5 each step", "restar 5 en cada paso"), 500, true, r => Arithmetic(5 * Between(r, 7, 12), -5)),
        new(new("diminuir 3 a cada passo", "subtract 3 each step", "restar 3 en cada paso"), 600, true, r => Arithmetic(Between(r, 20, 35), -3)),
        new(new("somar 1, depois somar 2, e repetir", "add 1, then add 2, and repeat", "sumar 1, después sumar 2, y repetir"), 750, false, r => Alternating(Between(r, 1, 10), 1, 2)),
        new(new("somar 2, depois tirar 1, e repetir", "add 2, then take away 1, and repeat", "sumar 2, después quitar 1, y repetir"), 850, false, r => Alternating(Between(r, 1, 10), 2, -1)),
        new(new("dobrar o número a cada passo", "double the number each step", "duplicar el número en cada paso"), 800, false, r => Doubling(Between(r, 1, 3))),
        new(new("somar 1, depois 2, depois 3… (o passo cresce)", "add 1, then 2, then 3… (the step grows)", "sumar 1, después 2, después 3… (el paso crece)"), 950, false, r => Growing(Between(r, 1, 5))),
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
            Tr("Qual número completa a sequência?", "Which number completes the sequence?", "¿Qué número completa la secuencia?"),
            answer.ToString(),
            [
                Tr("Compare cada número com o seguinte: quanto ele aumenta ou diminui?", "Compare each number with the next one: how much does it go up or down?", "Compara cada número con el siguiente: ¿cuánto aumenta o disminuye?"),
                Tr($"A regra é: {rule.Description}.", $"The rule is: {rule.Description}.", $"La regla es: {rule.Description}."),
            ],
            Tr($"A regra é {rule.Description}, então o número que falta é {answer}.", $"The rule is {rule.Description}, so the missing number is {answer}.", $"La regla es {rule.Description}, así que el número que falta es {answer}."))
        { Sequence = sequence };
        return new(content, rule.Difficulty + (blankInMiddle ? 80 : 0));
    }
}
