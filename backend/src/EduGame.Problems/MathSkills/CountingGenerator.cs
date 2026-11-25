using static EduGame.Problems.Lang;

namespace EduGame.Problems.MathSkills;

/// <summary>Contagem e comparação de números: contar objetos, antecessor/sucessor, maior/menor.</summary>
public sealed class CountingGenerator : ProblemGenerator
{
    public override string Key => "mat.contagem";
    public override string SkillSlug => "contagem-comparacao";

    // O gênero vale para o português e o espanhol (maçãs/manzanas, peixes/peces…).
    private static readonly (string Emoji, Text Name, bool Feminine)[] Things =
    [
        ("🍎", new("maçãs", "apples", "manzanas"), true), ("⭐", new("estrelas", "stars", "estrellas"), true), ("🐟", new("peixes", "fish", "peces"), false),
        ("🎈", new("balões", "balloons", "globos"), false), ("🌸", new("flores", "flowers", "flores"), true), ("⚽", new("bolas", "balls", "pelotas"), true),
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
            Tr($"Quantos números inteiros existem entre {a} e {b}? (sem contar o {a} nem o {b})",
               $"How many whole numbers are there between {a} and {b}? (not counting {a} or {b})",
               $"¿Cuántos números enteros hay entre {a} y {b}? (sin contar el {a} ni el {b})"),
            count.ToString(),
            [
                Tr($"Comece a contar pelo número logo depois de {a}: {a + 1}, {a + 2}…", $"Start counting from the number right after {a}: {a + 1}, {a + 2}…", $"Empieza a contar por el número justo después de {a}: {a + 1}, {a + 2}…"),
                Tr($"Pare no número logo antes de {b}, que é {b - 1}.", $"Stop at the number right before {b}, which is {b - 1}.", $"Para en el número justo antes de {b}, que es {b - 1}."),
            ],
            Tr($"Os números vão de {a + 1} até {b - 1}: são {count} números.", $"The numbers go from {a + 1} to {b - 1}: that's {count} numbers.", $"Los números van de {a + 1} a {b - 1}: son {count} números."))
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
            Tr($"{(feminine ? "Quantas" : "Quantos")} {name} há?", $"How many {name} are there?", $"¿{(feminine ? "Cuántas" : "Cuántos")} {name} hay?"),
            n.ToString(),
            [
                Tr("Conte um por um, apontando com o dedo.", "Count them one by one, pointing with your finger.", "Cuenta uno por uno, señalando con el dedo."),
                Tr("Os itens estão em grupos de 5. Conte os grupos de 5 e depois os que sobram.", "The items are in groups of 5. Count the groups of 5 and then the ones left over.", "Los objetos están en grupos de 5. Cuenta los grupos de 5 y después los que sobran."),
            ],
            Tr($"Há {n} {name}.", $"There are {n} {name}.", $"Hay {n} {name}."))
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

        List<string> hints =
        [
            after
                ? Tr("O número que vem depois é sempre 1 a mais.", "The number that comes after is always 1 more.", "El número que viene después siempre es 1 más.")
                : Tr("O número que vem antes é sempre 1 a menos.", "The number that comes before is always 1 less.", "El número que viene antes siempre es 1 menos."),
        ];
        if (crossesTen)
        {
            hints.Add(after
                ? Tr("Quando as unidades passam de 9, elas voltam para 0 e as dezenas aumentam 1.", "When the ones go past 9, they go back to 0 and the tens go up by 1.", "Cuando las unidades pasan de 9, vuelven a 0 y las decenas aumentan 1.")
                : Tr("Quando as unidades estão em 0, voltar 1 deixa as unidades em 9 e diminui 1 dezena.", "When the ones are at 0, going back 1 makes the ones 9 and takes away 1 ten.", "Cuando las unidades están en 0, retroceder 1 deja las unidades en 9 y quita 1 decena."));
        }

        var content = new ProblemContent(
            ProblemTypes.Numero,
            after
                ? Tr($"Qual número vem logo depois de {n}?", $"Which number comes right after {n}?", $"¿Qué número viene justo después de {n}?")
                : Tr($"Qual número vem logo antes de {n}?", $"Which number comes right before {n}?", $"¿Qué número viene justo antes de {n}?"),
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
        var word = bigger ? Tr("maior", "biggest", "mayor") : Tr("menor", "smallest", "menor");
        var content = new ProblemContent(
            ProblemTypes.MultiplaEscolha,
            Tr($"Qual é o {word} número?", $"Which is the {word} number?", $"¿Cuál es el número {word}?"),
            answer.ToString(),
            [
                Tr("Compare primeiro o algarismo mais à esquerda (o de maior valor).", "First compare the leftmost digit (the one with the greatest value).", "Compara primero la cifra de más a la izquierda (la de mayor valor)."),
                Tr("Se ele for igual, compare o próximo algarismo.", "If it's the same, compare the next digit.", "Si es igual, compara la siguiente cifra."),
            ],
            Tr($"O {word} número é {answer}.", $"The {word} number is {answer}.", $"El número {word} es {answer}."))
        { Choices = [.. numbers.OrderBy(_ => rng.Next()).Select(n => n.ToString())] };
        return new(content, difficulty);
    }
}
