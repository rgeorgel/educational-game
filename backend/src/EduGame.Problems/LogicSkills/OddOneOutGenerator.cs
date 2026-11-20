namespace EduGame.Problems.LogicSkills;

/// <summary>"Qual é o diferente?": classificação por categoria ou por propriedade numérica.</summary>
public sealed class OddOneOutGenerator : ProblemGenerator
{
    public override string Key => "log.diferente";
    public override string SkillSlug => "qual-e-diferente";

    private sealed record Category(string Plural, string Singular, string[] Items);

    private static readonly Category Fruits = new("frutas", "uma fruta", ["🍎", "🍌", "🍇", "🍓", "🍊", "🍐", "🍉", "🍒"]);
    private static readonly Category Animals = new("animais", "um animal", ["🐶", "🐱", "🐰", "🦊", "🐻", "🐼", "🐵", "🐮"]);
    private static readonly Category Vehicles = new("veículos", "um veículo", ["🚗", "🚌", "🚲", "✈️", "🚂", "🚀", "🚜", "⛵"]);
    private static readonly Category Vegetables = new("legumes e verduras", "um legume ou verdura", ["🥕", "🥦", "🌽", "🥒", "🥬", "🧅"]);
    private static readonly Category LandAnimals = new("animais que vivem na terra", "um animal que vive na terra", ["🐶", "🐱", "🐰", "🦊", "🐻", "🐮"]);
    private static readonly Category SeaAnimals = new("animais que vivem na água", "um animal que vive na água", ["🐟", "🐬", "🐙", "🐳", "🦈", "🐠"]);

    private static readonly (Category Group, Category Odd, int Difficulty)[] EmojiPairs =
    [
        (Fruits, Vehicles, 200), (Animals, Vehicles, 200), (Vehicles, Fruits, 220), (Fruits, Animals, 250),
        (Fruits, Vegetables, 480), (Vegetables, Fruits, 500), (LandAnimals, SeaAnimals, 560),
    ];

    public override GeneratedProblem Generate(Random rng) => rng.Next(4) switch
    {
        0 => Emoji(rng),
        3 => DifferentSum(rng),
        _ => Numbers(rng),
    };

    /// <summary>Três contas com o mesmo resultado e uma diferente.</summary>
    private static GeneratedProblem DifferentSum(Random rng)
    {
        var total = Between(rng, 6, 15);
        var withSubtraction = rng.Next(2) == 0;
        var splits = Enumerable.Range(1, total - 1).OrderBy(_ => rng.Next()).Take(3)
            .Select(x => $"{x} + {total - x}").ToList();
        if (withSubtraction) splits[0] = $"{total + 3} − 3";

        var offset = rng.Next(2) == 0 ? 1 : -1;
        var y = Between(rng, 1, total - 2);
        var different = $"{y} + {total + offset - y}";

        var content = new ProblemContent(
            ProblemTypes.MultiplaEscolha,
            "Qual conta tem o resultado diferente das outras?",
            different,
            ["Calcule o resultado de cada conta.", $"Três contas dão {total}."],
            $"{different} = {total + offset}; as outras dão {total}.")
        { Choices = Choices(rng, different, splits) };
        return new(content, withSubtraction ? 1000 : 920);
    }

    private static GeneratedProblem Emoji(Random rng)
    {
        var (group, odd, difficulty) = Pick(rng, EmojiPairs);
        var three = group.Items.OrderBy(_ => rng.Next()).Take(3).ToList();
        var different = Pick(rng, odd.Items);

        var content = new ProblemContent(
            ProblemTypes.MultiplaEscolha,
            "Qual destes é diferente dos outros?",
            different,
            ["Pense no que três deles têm em comum.", $"Três deles são {group.Plural}."],
            $"{different} é diferente: os outros são {group.Plural}, e {different} é {odd.Singular}.")
        { Choices = Choices(rng, different, three) };
        return new(content, difficulty);
    }

    private static GeneratedProblem Numbers(Random rng)
    {
        // Números sem múltiplos de 5 para que só uma regra separe o diferente.
        static bool Plain(int n) => n % 5 != 0;

        return rng.Next(4) switch
        {
            0 => Build(rng, "números de um algarismo", "tem dois algarismos",
                    pool: [1, 2, 3, 4, 6, 7, 8, 9], oddPool: [.. Enumerable.Range(11, 89).Where(Plain)], 400,
                    ["Conte quantos algarismos tem cada número."]),
            1 => Build(rng, "múltiplos de 10 (terminam em 0)", "não termina em 0",
                    pool: [20, 30, 40, 50, 60, 70, 80, 90], oddPool: [.. Enumerable.Range(11, 89).Where(Plain)], 600,
                    ["Olhe o último algarismo de cada número."]),
            2 => Parity(rng),
            _ => Build(rng, "números que terminam em 5", "não termina em 5",
                    pool: [15, 25, 35, 45, 55, 65, 75, 85, 95], oddPool: [.. Enumerable.Range(11, 89).Where(n => Plain(n) && n % 2 == 1)], 850,
                    ["Olhe o último algarismo de cada número."]),
        };
    }

    private static GeneratedProblem Parity(Random rng)
    {
        var evensAreGroup = rng.Next(2) == 0;
        int[] evens = [.. Enumerable.Range(11, 89).Where(n => n % 2 == 0 && n % 5 != 0)];
        int[] odds = [.. Enumerable.Range(11, 89).Where(n => n % 2 == 1 && n % 5 != 0)];
        return evensAreGroup
            ? Build(rng, "números pares", "é ímpar", evens, odds, 720, ["Pense em pares e ímpares.", "Números pares terminam em 0, 2, 4, 6 ou 8."])
            : Build(rng, "números ímpares", "é par", odds, evens, 740, ["Pense em pares e ímpares.", "Números ímpares terminam em 1, 3, 5, 7 ou 9."]);
    }

    private static GeneratedProblem Build(Random rng, string groupName, string oddDescription, int[] pool, int[] oddPool, int difficulty, List<string> hints)
    {
        var three = pool.OrderBy(_ => rng.Next()).Take(3).Select(n => n.ToString()).ToList();
        var different = Pick(rng, oddPool).ToString();
        hints.Add($"Três deles são {groupName}.");

        var content = new ProblemContent(
            ProblemTypes.MultiplaEscolha,
            "Qual número é diferente dos outros?",
            different,
            hints,
            $"{different} é diferente: os outros são {groupName}, e {different} {oddDescription}.")
        { Choices = Choices(rng, different, three) };
        return new(content, difficulty);
    }
}
