namespace EduGame.Problems.MathSkills;

/// <summary>Adição até 100, com e sem "vai um", incluindo problemas com história.</summary>
public sealed class AdditionGenerator : ProblemGenerator
{
    public override string Key => "mat.adicao";
    public override string SkillSlug => "adicao-ate-100";

    internal static readonly string[] Names = ["Ana", "Bruno", "Carla", "Davi", "Elisa", "Gabriel", "Helena", "Igor", "Júlia", "Lucas", "Marina", "Pedro"];
    internal static readonly string[] Objects = ["figurinhas", "bolinhas de gude", "conchas", "canetas", "pipas", "cartas"];

    public override GeneratedProblem Generate(Random rng)
    {
        var mode = rng.Next(4);
        if (mode == 3) return ThreeAddends(rng);

        int a, b;
        switch (mode)
        {
            case 0: a = Between(rng, 1, 9); b = Between(rng, 1, 9); break;
            case 1: a = Between(rng, 10, 89); b = Between(rng, 1, Math.Min(9, 99 - a)); break;
            default: a = Between(rng, 10, 80); b = Between(rng, 10, 99 - a); break;
        }

        var sum = a + b;
        var carry = a % 10 + b % 10 >= 10;
        var twoDigit = a >= 10 || b >= 10;
        var difficulty = !twoDigit
            ? 150 + sum * 10 + (carry ? 60 : 0)
            : 380 + (a >= 10 && b >= 10 ? 120 : 0) + (carry ? 200 : 0) + sum;

        var story = twoDigit && rng.Next(3) == 0;
        string prompt;
        if (story)
        {
            var name = Pick(rng, Names);
            var obj = Pick(rng, Objects);
            prompt = $"{name} tinha {a} {obj} e ganhou mais {b}. Com quantas {obj} {name} ficou?";
            difficulty += 60;
        }
        else
        {
            prompt = $"Quanto é {a} + {b}?";
        }

        var content = new ProblemContent(ProblemTypes.Numero, prompt, sum.ToString(), Hints(a, b), $"{a} + {b} = {sum}");
        return new(content, difficulty);
    }

    private static List<string> Hints(int a, int b)
    {
        if (a < 10 && b < 10)
        {
            var (big, small) = a >= b ? (a, b) : (b, a);
            return [$"Comece pelo maior número ({big}) e conte mais {small} para a frente.", "Você pode usar os dedos para ajudar!"];
        }

        var u = a % 10 + b % 10;
        var hints = new List<string> { $"Some primeiro as unidades: {a % 10} + {b % 10} = {u}." };
        if (u >= 10) hints.Add($"{u} unidades = 1 dezena e {u - 10} unidades. Leve 1 dezena para a coluna das dezenas (o \"vai um\").");
        hints.Add($"Agora some as dezenas: {a / 10} + {b / 10}{(u >= 10 ? " + 1" : "")}.");
        return hints;
    }

    private static GeneratedProblem ThreeAddends(Random rng)
    {
        int a, b, c;
        do { a = Between(rng, 3, 35); b = Between(rng, 3, 35); c = Between(rng, 3, 30); } while (a + b + c > 99);
        var sum = a + b + c;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Quanto é {a} + {b} + {c}?",
            sum.ToString(),
            [$"Some dois números de cada vez: primeiro {a} + {b} = {a + b}.", $"Depois some o resultado com {c}."],
            $"{a} + {b} = {a + b}; {a + b} + {c} = {sum}");
        return new(content, 720 + sum * 3);
    }
}
