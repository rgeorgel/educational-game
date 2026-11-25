using static EduGame.Problems.Lang;

namespace EduGame.Problems.MathSkills;

/// <summary>Adição até 100, com e sem "vai um", incluindo problemas com história.</summary>
public sealed class AdditionGenerator : ProblemGenerator
{
    public override string Key => "mat.adicao";
    public override string SkillSlug => "adicao-ate-100";

    // Listas com o mesmo tamanho nos três idiomas: o sorteio (e o desafio do dia) não muda com o idioma.
    private static readonly string[] NamesPt = ["Ana", "Bruno", "Carla", "Davi", "Elisa", "Gabriel", "Helena", "Igor", "Júlia", "Lucas", "Marina", "Pedro"];
    private static readonly string[] NamesEn = ["Amy", "Ben", "Chloe", "David", "Emma", "Gabriel", "Hannah", "Isaac", "Julia", "Luke", "Mia", "Peter"];
    private static readonly string[] NamesEs = ["Ana", "Bruno", "Carla", "Diego", "Elena", "Gabriel", "Hugo", "Inés", "Julia", "Lucas", "Marina", "Pablo"];
    internal static IReadOnlyList<string> Names => Current switch { En => NamesEn, Es => NamesEs, _ => NamesPt };

    // Todos no feminino em português e espanhol ("Quantas…", "Cuántas…").
    private static readonly string[] ObjectsPt = ["figurinhas", "bolinhas de gude", "conchas", "canetas", "pipas", "cartas"];
    private static readonly string[] ObjectsEn = ["stickers", "marbles", "shells", "pens", "kites", "cards"];
    private static readonly string[] ObjectsEs = ["pegatinas", "canicas", "conchas", "fichas", "cometas", "cartas"];
    internal static IReadOnlyList<string> Objects => Current switch { En => ObjectsEn, Es => ObjectsEs, _ => ObjectsPt };

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
            prompt = Tr(
                $"{name} tinha {a} {obj} e ganhou mais {b}. Com quantas {obj} {name} ficou?",
                $"{name} had {a} {obj} and got {b} more. How many {obj} does {name} have now?",
                $"{name} tenía {a} {obj} y le dieron {b} más. ¿Cuántas {obj} tiene ahora {name}?");
            difficulty += 60;
        }
        else
        {
            prompt = Tr($"Quanto é {a} + {b}?", $"What is {a} + {b}?", $"¿Cuánto es {a} + {b}?");
        }

        var content = new ProblemContent(ProblemTypes.Numero, prompt, sum.ToString(), Hints(a, b), $"{a} + {b} = {sum}");
        return new(content, difficulty);
    }

    private static List<string> Hints(int a, int b)
    {
        if (a < 10 && b < 10)
        {
            var (big, small) = a >= b ? (a, b) : (b, a);
            return
            [
                Tr($"Comece pelo maior número ({big}) e conte mais {small} para a frente.", $"Start with the bigger number ({big}) and count {small} more forward.", $"Empieza por el número mayor ({big}) y cuenta {small} más hacia delante."),
                Tr("Você pode usar os dedos para ajudar!", "You can use your fingers to help!", "¡Puedes usar los dedos para ayudarte!"),
            ];
        }

        var u = a % 10 + b % 10;
        var hints = new List<string> { Tr($"Some primeiro as unidades: {a % 10} + {b % 10} = {u}.", $"First add the ones: {a % 10} + {b % 10} = {u}.", $"Suma primero las unidades: {a % 10} + {b % 10} = {u}.") };
        if (u >= 10)
        {
            hints.Add(Tr(
                $"{u} unidades = 1 dezena e {u - 10} unidades. Leve 1 dezena para a coluna das dezenas (o \"vai um\").",
                $"{u} ones = 1 ten and {u - 10} ones. Carry 1 ten over to the tens column.",
                $"{u} unidades = 1 decena y {u - 10} unidades. Lleva 1 decena a la columna de las decenas (la \"llevada\")."));
        }
        var plusOne = u >= 10 ? " + 1" : "";
        hints.Add(Tr($"Agora some as dezenas: {a / 10} + {b / 10}{plusOne}.", $"Now add the tens: {a / 10} + {b / 10}{plusOne}.", $"Ahora suma las decenas: {a / 10} + {b / 10}{plusOne}."));
        return hints;
    }

    private static GeneratedProblem ThreeAddends(Random rng)
    {
        int a, b, c;
        do { a = Between(rng, 3, 35); b = Between(rng, 3, 35); c = Between(rng, 3, 30); } while (a + b + c > 99);
        var sum = a + b + c;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"Quanto é {a} + {b} + {c}?", $"What is {a} + {b} + {c}?", $"¿Cuánto es {a} + {b} + {c}?"),
            sum.ToString(),
            [
                Tr($"Some dois números de cada vez: primeiro {a} + {b} = {a + b}.", $"Add two numbers at a time: first {a} + {b} = {a + b}.", $"Suma dos números cada vez: primero {a} + {b} = {a + b}."),
                Tr($"Depois some o resultado com {c}.", $"Then add {c} to the result.", $"Después suma {c} al resultado."),
            ],
            $"{a} + {b} = {a + b}; {a + b} + {c} = {sum}");
        return new(content, 720 + sum * 3);
    }
}
