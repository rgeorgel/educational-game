namespace EduGame.Problems.MathSkills;

/// <summary>Contagem e comparação de números: contar objetos, antecessor/sucessor, maior/menor.</summary>
public sealed class CountingGenerator : ProblemGenerator
{
    public override string Key => "mat.contagem";
    public override string SkillSlug => "contagem-comparacao";

    private static readonly (string Emoji, string Name, bool Feminine)[] Things =
    [
        ("🍎", "maçãs", true), ("⭐", "estrelas", true), ("🐟", "peixes", false), ("🎈", "balões", false), ("🌸", "flores", true), ("⚽", "bolas", true),
    ];

    public override GeneratedProblem Generate(Random rng) => rng.Next(5) switch
    {
        0 => CountObjects(rng),
        1 => Neighbor(rng, after: true),
        2 => Neighbor(rng, after: false),
        3 => Biggest(rng),
        _ => NumbersBetween(rng),
    };

    private static GeneratedProblem NumbersBetween(Random rng)
    {
        var a = Between(rng, 5, 80);
        var b = a + Between(rng, 3, 15);
        var count = b - a - 1;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Quantos números inteiros existem entre {a} e {b}? (sem contar o {a} nem o {b})",
            count.ToString(),
            [$"Comece a contar pelo número logo depois de {a}: {a + 1}, {a + 2}…", $"Pare no número logo antes de {b}, que é {b - 1}."],
            $"Os números vão de {a + 1} até {b - 1}: são {count} números.")
        ;
        return new(content, 880 + count * 10);
    }

    private static GeneratedProblem CountObjects(Random rng)
    {
        var n = Between(rng, 3, 15);
        var (emoji, name, feminine) = Pick(rng, Things);
        // Agrupa de 5 em 5 para facilitar a contagem.
        var groups = Enumerable.Range(0, (n + 4) / 5).Select(i => Repeat(emoji, Math.Min(5, n - i * 5)));
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"{(feminine ? "Quantas" : "Quantos")} {name} há?",
            n.ToString(),
            ["Conte um por um, apontando com o dedo.", "Os itens estão em grupos de 5. Conte os grupos de 5 e depois os que sobram."],
            $"Há {n} {name}.")
        { Visual = string.Join(' ', groups) };
        return new(content, 100 + n * 12);
    }

    private static GeneratedProblem Neighbor(Random rng, bool after)
    {
        var max = Pick(rng, [10, 20, 100, 1000]);
        var n = Between(rng, 2, max - 1);
        // Às vezes força as "viradas" de dezena/centena, que são as partes difíceis.
        if (max >= 100 && rng.Next(3) == 0) n = after ? n - n % 10 + 9 : n - n % 10;
        if (n < 2) n = 10;

        var answer = after ? n + 1 : n - 1;
        var difficulty = max switch { 10 => 150, 20 => 220, 100 => 330, _ => 520 };
        var crossesTen = after ? n % 10 == 9 : n % 10 == 0;
        var crossesHundred = after ? n % 100 == 99 : n % 100 == 0;
        if (crossesTen) difficulty += 150;
        if (crossesHundred && max == 1000) difficulty += 120;
        if (!after) difficulty += 60;

        List<string> hints = [after ? "O número que vem depois é sempre 1 a mais." : "O número que vem antes é sempre 1 a menos."];
        if (crossesTen)
        {
            hints.Add(after
                ? "Quando as unidades passam de 9, elas voltam para 0 e as dezenas aumentam 1."
                : "Quando as unidades estão em 0, voltar 1 deixa as unidades em 9 e diminui 1 dezena.");
        }

        var content = new ProblemContent(
            ProblemTypes.Numero,
            after ? $"Qual número vem logo depois de {n}?" : $"Qual número vem logo antes de {n}?",
            answer.ToString(),
            hints,
            after ? $"{n} + 1 = {answer}" : $"{n} − 1 = {answer}");
        return new(content, difficulty);
    }

    private static GeneratedProblem Biggest(Random rng)
    {
        var bigger = rng.Next(2) == 0;
        int[] numbers;
        int difficulty;

        if (rng.Next(2) == 0)
        {
            // Dois algarismos, incluindo o número com os algarismos trocados (47 × 74).
            int a, b;
            do { a = Between(rng, 1, 9); b = Between(rng, 1, 9); } while (a == b);
            var c = Between(rng, 10, 99);
            numbers = [a * 10 + b, b * 10 + a, c];
            difficulty = 400;
        }
        else
        {
            // Três algarismos com a mesma centena (e às vezes a mesma dezena).
            var h = Between(rng, 1, 9);
            var sameTens = rng.Next(2) == 0;
            var t = Between(rng, 0, 9);
            numbers = [.. Enumerable.Range(0, 3).Select(_ => h * 100 + (sameTens ? t : Between(rng, 0, 9)) * 10 + Between(rng, 0, 9))];
            difficulty = sameTens ? 800 : 650;
        }

        numbers = [.. numbers.Distinct()];
        while (numbers.Length < 3) numbers = [.. numbers, numbers.Max() + 1];
        if (!bigger) difficulty += 50;

        var answer = bigger ? numbers.Max() : numbers.Min();
        var word = bigger ? "maior" : "menor";
        var content = new ProblemContent(
            ProblemTypes.MultiplaEscolha,
            $"Qual é o {word} número?",
            answer.ToString(),
            [
                "Compare primeiro o algarismo mais à esquerda (o de maior valor).",
                "Se ele for igual, compare o próximo algarismo.",
            ],
            $"O {word} número é {answer}.")
        { Choices = [.. numbers.OrderBy(_ => rng.Next()).Select(n => n.ToString())] };
        return new(content, difficulty);
    }
}
