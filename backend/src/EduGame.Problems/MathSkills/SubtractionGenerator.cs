namespace EduGame.Problems.MathSkills;

/// <summary>Subtração até 100, com e sem "empresta um", e o número que falta.</summary>
public sealed class SubtractionGenerator : ProblemGenerator
{
    public override string Key => "mat.subtracao";
    public override string SkillSlug => "subtracao-ate-100";

    public override GeneratedProblem Generate(Random rng)
    {
        var mode = rng.Next(4);
        if (mode == 3) return MissingAddend(rng);

        int a, b;
        switch (mode)
        {
            case 0: a = Between(rng, 2, 18); b = Between(rng, 1, Math.Min(9, a)); break;
            case 1: a = Between(rng, 20, 99); b = Between(rng, 1, 9); break;
            default: a = Between(rng, 20, 99); b = Between(rng, 10, a - 1); break;
        }

        var diff = a - b;
        var borrow = a % 10 < b % 10;
        var difficulty = mode switch
        {
            0 => a <= 10 ? 180 + a * 10 : 330 + (borrow ? 100 : 0) + a * 3,
            1 => 470 + (borrow ? 200 : 0) + a,
            _ => 600 + (borrow ? 220 : 0) + a,
        };

        string prompt;
        if (mode > 0 && rng.Next(3) == 0)
        {
            var name = Pick(rng, AdditionGenerator.Names);
            var obj = Pick(rng, AdditionGenerator.Objects);
            prompt = $"{name} tinha {a} {obj} e deu {b} para uma amiga. Quantas {obj} sobraram?";
            difficulty += 60;
        }
        else
        {
            prompt = $"Quanto é {a} − {b}?";
        }

        List<string> hints;
        if (a < 20 && b < 10)
        {
            hints = [$"Comece no {a} e conte {b} para trás.", $"Ou pense: quanto falta de {b} para chegar em {a}?"];
        }
        else
        {
            hints = [$"Comece pelas unidades: {a % 10} − {b % 10}."];
            if (borrow)
            {
                hints[0] = $"Nas unidades, {a % 10} é menor que {b % 10}, então não dá para tirar direto.";
                hints.Add($"Pegue emprestada 1 dezena: as unidades viram {a % 10 + 10} e as dezenas ficam {a / 10 - 1}.");
            }
            hints.Add("Depois subtraia as dezenas.");
        }

        var content = new ProblemContent(ProblemTypes.Numero, prompt, diff.ToString(), hints, $"{a} − {b} = {diff}");
        return new(content, difficulty);
    }

    private static GeneratedProblem MissingAddend(Random rng)
    {
        var total = Between(rng, 15, 99);
        var known = Between(rng, 5, total - 5);
        var missing = total - known;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Qual número completa a conta: {known} + ? = {total}",
            missing.ToString(),
            [$"Quanto falta de {known} para chegar em {total}?", $"Você pode fazer a subtração {total} − {known}."],
            $"{total} − {known} = {missing}, então {known} + {missing} = {total}");
        return new(content, 820 + total);
    }
}
