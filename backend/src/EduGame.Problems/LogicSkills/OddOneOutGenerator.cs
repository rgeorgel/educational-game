using static EduGame.Problems.Lang;

namespace EduGame.Problems.LogicSkills;

/// <summary>"Qual é o diferente?": classificação por categoria ou por propriedade numérica.</summary>
public sealed class OddOneOutGenerator : ProblemGenerator
{
    public override string Key => "log.diferente";
    public override string SkillSlug => "qual-e-diferente";

    private sealed record Category(Text Plural, Text Singular, string[] Items);

    private static readonly Category Fruits = new(new("frutas", "fruits", "frutas"), new("uma fruta", "a fruit", "una fruta"), ["🍎", "🍌", "🍇", "🍓", "🍊", "🍐", "🍉", "🍒"]);
    private static readonly Category Animals = new(new("animais", "animals", "animales"), new("um animal", "an animal", "un animal"), ["🐶", "🐱", "🐰", "🦊", "🐻", "🐼", "🐵", "🐮"]);
    private static readonly Category Vehicles = new(new("veículos", "vehicles", "vehículos"), new("um veículo", "a vehicle", "un vehículo"), ["🚗", "🚌", "🚲", "✈️", "🚂", "🚀", "🚜", "⛵"]);
    private static readonly Category Vegetables = new(new("legumes e verduras", "vegetables", "verduras y hortalizas"), new("um legume ou verdura", "a vegetable", "una verdura u hortaliza"), ["🥕", "🥦", "🌽", "🥒", "🥬", "🧅"]);
    private static readonly Category LandAnimals = new(new("animais que vivem na terra", "animals that live on land", "animales que viven en la tierra"), new("um animal que vive na terra", "an animal that lives on land", "un animal que vive en la tierra"), ["🐶", "🐱", "🐰", "🦊", "🐻", "🐮"]);
    private static readonly Category SeaAnimals = new(new("animais que vivem na água", "animals that live in water", "animales que viven en el agua"), new("um animal que vive na água", "an animal that lives in water", "un animal que vive en el agua"), ["🐟", "🐬", "🐙", "🐳", "🦈", "🐠"]);

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
            Tr("Qual conta tem o resultado diferente das outras?", "Which sum has a different result from the others?", "¿Qué cuenta tiene un resultado diferente de las otras?"),
            different,
            [
                Tr("Calcule o resultado de cada conta.", "Work out the result of each sum.", "Calcula el resultado de cada cuenta."),
                Tr($"Três contas dão {total}.", $"Three of them equal {total}.", $"Tres cuentas dan {total}."),
            ],
            Tr($"{different} = {total + offset}; as outras dão {total}.", $"{different} = {total + offset}; the others equal {total}.", $"{different} = {total + offset}; las otras dan {total}."))
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
            Tr("Qual destes é diferente dos outros?", "Which of these is different from the others?", "¿Cuál de estos es diferente de los otros?"),
            different,
            [
                Tr("Pense no que três deles têm em comum.", "Think about what three of them have in common.", "Piensa en qué tienen en común tres de ellos."),
                Tr($"Três deles são {group.Plural}.", $"Three of them are {group.Plural}.", $"Tres de ellos son {group.Plural}."),
            ],
            Tr($"{different} é diferente: os outros são {group.Plural}, e {different} é {odd.Singular}.",
               $"{different} is different: the others are {group.Plural}, and {different} is {odd.Singular}.",
               $"{different} es diferente: los otros son {group.Plural}, y {different} es {odd.Singular}."))
        { Choices = Choices(rng, different, three) };
        return new(content, difficulty);
    }

    private static GeneratedProblem Numbers(Random rng)
    {
        // Números sem múltiplos de 5 para que só uma regra separe o diferente.
        static bool Plain(int n) => n % 5 != 0;

        return rng.Next(4) switch
        {
            0 => Build(rng, new("números de um algarismo", "one-digit numbers", "números de una cifra"), new("tem dois algarismos", "has two digits", "tiene dos cifras"),
                    pool: [1, 2, 3, 4, 6, 7, 8, 9], oddPool: [.. Enumerable.Range(11, 89).Where(Plain)], 400,
                    [Tr("Conte quantos algarismos tem cada número.", "Count how many digits each number has.", "Cuenta cuántas cifras tiene cada número.")]),
            1 => Build(rng, new("múltiplos de 10 (terminam em 0)", "multiples of 10 (they end in 0)", "múltiplos de 10 (terminan en 0)"), new("não termina em 0", "doesn't end in 0", "no termina en 0"),
                    pool: [20, 30, 40, 50, 60, 70, 80, 90], oddPool: [.. Enumerable.Range(11, 89).Where(Plain)], 600,
                    [LastDigit()]),
            2 => Parity(rng),
            _ => Build(rng, new("números que terminam em 5", "numbers that end in 5", "números que terminan en 5"), new("não termina em 5", "doesn't end in 5", "no termina en 5"),
                    pool: [15, 25, 35, 45, 55, 65, 75, 85, 95], oddPool: [.. Enumerable.Range(11, 89).Where(n => Plain(n) && n % 2 == 1)], 850,
                    [LastDigit()]),
        };
    }

    private static string LastDigit() => Tr("Olhe o último algarismo de cada número.", "Look at the last digit of each number.", "Mira la última cifra de cada número.");

    private static string EvenOdd() => Tr("Pense em pares e ímpares.", "Think about even and odd numbers.", "Piensa en pares e impares.");

    private static GeneratedProblem Parity(Random rng)
    {
        var evensAreGroup = rng.Next(2) == 0;
        int[] evens = [.. Enumerable.Range(11, 89).Where(n => n % 2 == 0 && n % 5 != 0)];
        int[] odds = [.. Enumerable.Range(11, 89).Where(n => n % 2 == 1 && n % 5 != 0)];
        return evensAreGroup
            ? Build(rng, new("números pares", "even numbers", "números pares"), new("é ímpar", "is odd", "es impar"), evens, odds, 720,
                [EvenOdd(), Tr("Números pares terminam em 0, 2, 4, 6 ou 8.", "Even numbers end in 0, 2, 4, 6 or 8.", "Los números pares terminan en 0, 2, 4, 6 u 8.")])
            : Build(rng, new("números ímpares", "odd numbers", "números impares"), new("é par", "is even", "es par"), odds, evens, 740,
                [EvenOdd(), Tr("Números ímpares terminam em 1, 3, 5, 7 ou 9.", "Odd numbers end in 1, 3, 5, 7 or 9.", "Los números impares terminan en 1, 3, 5, 7 o 9.")]);
    }

    private static GeneratedProblem Build(Random rng, Text groupName, Text oddDescription, int[] pool, int[] oddPool, int difficulty, List<string> hints)
    {
        var three = pool.OrderBy(_ => rng.Next()).Take(3).Select(n => n.ToString()).ToList();
        var different = Pick(rng, oddPool).ToString();
        hints.Add(Tr($"Três deles são {groupName}.", $"Three of them are {groupName}.", $"Tres de ellos son {groupName}."));

        var content = new ProblemContent(
            ProblemTypes.MultiplaEscolha,
            Tr("Qual número é diferente dos outros?", "Which number is different from the others?", "¿Qué número es diferente de los otros?"),
            different,
            hints,
            Tr($"{different} é diferente: os outros são {groupName}, e {different} {oddDescription}.",
               $"{different} is different: the others are {groupName}, and {different} {oddDescription}.",
               $"{different} es diferente: los otros son {groupName}, y {different} {oddDescription}."))
        { Choices = Choices(rng, different, three) };
        return new(content, difficulty);
    }
}
