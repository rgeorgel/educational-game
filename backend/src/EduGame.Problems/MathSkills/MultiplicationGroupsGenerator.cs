using static EduGame.Problems.Lang;

namespace EduGame.Problems.MathSkills;

/// <summary>Noção de multiplicação: grupos iguais, tabuada até 5, dobro e triplo.</summary>
public sealed class MultiplicationGroupsGenerator : ProblemGenerator
{
    public override string Key => "mat.multiplicacao";
    public override string SkillSlug => "multiplicacao-grupos";

    // Gênero em português e em espanhol (para "Quantos/Quantas", "Cuántos/Cuántas").
    private static readonly (string Emoji, Text Name, bool FemininePt, bool FeminineEs)[] Things =
    [
        ("🍪", new("biscoitos", "cookies", "galletas"), false, true), ("🍓", new("morangos", "strawberries", "fresas"), false, true),
        ("🐞", new("joaninhas", "ladybugs", "mariquitas"), true, true), ("🧁", new("bolinhos", "cupcakes", "magdalenas"), false, true),
        ("🌟", new("estrelas", "stars", "estrellas"), true, true),
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
        var (emoji, name, femininePt, feminineEs) = Pick(rng, Things);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"São {g} grupos com {k} {name} cada. {(femininePt ? "Quantas" : "Quantos")} {name} há ao todo?",
               $"There are {g} groups with {k} {name} each. How many {name} are there in all?",
               $"Hay {g} grupos con {k} {name} cada uno. ¿{(feminineEs ? "Cuántas" : "Cuántos")} {name} hay en total?"),
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
            Tr($"Quanto é {a} × {b}?", $"What is {a} × {b}?", $"¿Cuánto es {a} × {b}?"),
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
                Tr($"Qual é o dobro de {n}?", $"What is double {n}?", $"¿Cuál es el doble de {n}?"),
                (2 * n).ToString(),
                [Tr("O dobro é 2 vezes o número.", "Double means 2 times the number.", "El doble es 2 veces el número."), $"{n} + {n} = ?"],
                $"2 × {n} = {2 * n}");
            return new(content, 450 + n * 10);
        }
        else
        {
            var n = Between(rng, 2, 10);
            var content = new ProblemContent(
                ProblemTypes.Numero,
                Tr($"Qual é o triplo de {n}?", $"What is triple {n}?", $"¿Cuál es el triple de {n}?"),
                (3 * n).ToString(),
                [Tr("O triplo é 3 vezes o número.", "Triple means 3 times the number.", "El triple es 3 veces el número."), $"{n} + {n} + {n} = ?"],
                $"3 × {n} = {3 * n}");
            return new(content, 650 + n * 20);
        }
    }

    private static GeneratedProblem Story(Random rng)
    {
        var g = Between(rng, 2, 5);
        var k = Between(rng, 2, 10);
        var i = rng.Next(4);
        string[] pt =
        [
            $"Uma caixa tem {k} lápis. Quantos lápis há em {g} caixas?",
            $"Um pacote tem {k} balas. Quantas balas há em {g} pacotes?",
            $"Um saquinho tem {k} bolinhas. Quantas bolinhas há em {g} saquinhos?",
            $"Uma prateleira tem {k} livros. Quantos livros há em {g} prateleiras?",
        ];
        string[] en =
        [
            $"A box has {k} pencils. How many pencils are there in {g} boxes?",
            $"A bag has {k} candies. How many candies are there in {g} bags?",
            $"A pouch has {k} marbles. How many marbles are there in {g} pouches?",
            $"A shelf has {k} books. How many books are there on {g} shelves?",
        ];
        string[] es =
        [
            $"Una caja tiene {k} lápices. ¿Cuántos lápices hay en {g} cajas?",
            $"Un paquete tiene {k} caramelos. ¿Cuántos caramelos hay en {g} paquetes?",
            $"Una bolsita tiene {k} canicas. ¿Cuántas canicas hay en {g} bolsitas?",
            $"Un estante tiene {k} libros. ¿Cuántos libros hay en {g} estantes?",
        ];
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr(pt[i], en[i], es[i]),
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
            Tr($"Multiplicar é somar parcelas iguais: {string.Join(" + ", Enumerable.Repeat(each, groups))}.",
               $"Multiplying is adding equal groups: {string.Join(" + ", Enumerable.Repeat(each, groups))}.",
               $"Multiplicar es sumar sumandos iguales: {string.Join(" + ", Enumerable.Repeat(each, groups))}."),
            Tr($"Conte de {each} em {each}: {sequence}…", $"Count by {each}s: {sequence}…", $"Cuenta de {each} en {each}: {sequence}…"),
        ];
    }
}
