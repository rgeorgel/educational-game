namespace EduGame.Problems.MathSkills;

/// <summary>Noção de multiplicação: grupos iguais, tabuada até 5, dobro e triplo.</summary>
public sealed class MultiplicationGroupsGenerator : ProblemGenerator
{
    public override string Key => "mat.multiplicacao";
    public override string SkillSlug => "multiplicacao-grupos";

    private static readonly (string Emoji, string Name, bool Feminine)[] Things =
    [
        ("🍪", "biscoitos", false), ("🍓", "morangos", false), ("🐞", "joaninhas", true), ("🧁", "bolinhos", false), ("🌟", "estrelas", true),
    ];

    public override GeneratedProblem Generate(Random rng) => rng.Next(4) switch
    {
        0 => VisualGroups(rng),
        1 => TimesTable(rng),
        2 => DoubleTriple(rng),
        _ => Story(rng),
    };

    private static GeneratedProblem VisualGroups(Random rng)
    {
        var g = Between(rng, 2, 5);
        var k = Between(rng, 2, 5);
        var (emoji, name, feminine) = Pick(rng, Things);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"São {g} grupos com {k} {name} cada. {(feminine ? "Quantas" : "Quantos")} {name} há ao todo?",
            (g * k).ToString(),
            GroupHints(g, k),
            $"{g} × {k} = {g * k}")
        { Visual = string.Join(' ', Enumerable.Repeat(Repeat(emoji, k), g)) };
        return new(content, 300 + g * k * 8);
    }

    private static GeneratedProblem TimesTable(Random rng)
    {
        var a = Between(rng, 2, 5);
        var b = Between(rng, 1, 10);
        var easy = a == 2 || b <= 2 || b == 10;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Quanto é {a} × {b}?",
            (a * b).ToString(),
            GroupHints(a, b),
            $"{a} × {b} = {a * b}");
        return new(content, 500 + a * b * 7 - (easy ? 80 : 0));
    }

    private static GeneratedProblem DoubleTriple(Random rng)
    {
        if (rng.Next(2) == 0)
        {
            var n = Between(rng, 2, 20);
            var content = new ProblemContent(
                ProblemTypes.Numero,
                $"Qual é o dobro de {n}?",
                (2 * n).ToString(),
                ["O dobro é 2 vezes o número.", $"{n} + {n} = ?"],
                $"2 × {n} = {2 * n}");
            return new(content, 450 + n * 10);
        }
        else
        {
            var n = Between(rng, 2, 10);
            var content = new ProblemContent(
                ProblemTypes.Numero,
                $"Qual é o triplo de {n}?",
                (3 * n).ToString(),
                ["O triplo é 3 vezes o número.", $"{n} + {n} + {n} = ?"],
                $"3 × {n} = {3 * n}");
            return new(content, 650 + n * 20);
        }
    }

    private static GeneratedProblem Story(Random rng)
    {
        var g = Between(rng, 2, 5);
        var k = Between(rng, 2, 10);
        var (container, item, feminine) = Pick(rng, [("caixa", "lápis", false), ("pacote", "balas", true), ("saquinho", "bolinhas", true), ("prateleira", "livros", false)]);
        var plural = container switch { "caixa" => "caixas", "pacote" => "pacotes", "saquinho" => "saquinhos", _ => "prateleiras" };
        var article = container is "caixa" or "prateleira" ? "Uma" : "Um";
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"{article} {container} tem {k} {item}. {(feminine ? "Quantas" : "Quantos")} {item} há em {g} {plural}?",
            (g * k).ToString(),
            GroupHints(g, k),
            $"{g} × {k} = {g * k}");
        return new(content, 750 + g * k * 5);
    }

    private static List<string> GroupHints(int groups, int each)
    {
        var sequence = string.Join(", ", Enumerable.Range(1, Math.Min(groups, 3)).Select(i => i * each));
        return
        [
            $"Multiplicar é somar parcelas iguais: {string.Join(" + ", Enumerable.Repeat(each, groups))}.",
            $"Conte de {each} em {each}: {sequence}…",
        ];
    }
}
