using static EduGame.Problems.Lang;

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
            prompt = Tr(
                $"{name} tinha {a} {obj} e deu {b} para uma amiga. Quantas {obj} sobraram?",
                $"{name} had {a} {obj} and gave {b} to a friend. How many {obj} are left?",
                $"{name} tenía {a} {obj} y le dio {b} a una amiga. ¿Cuántas {obj} le quedaron?");
            difficulty += 60;
        }
        else
        {
            prompt = Tr($"Quanto é {a} − {b}?", $"What is {a} − {b}?", $"¿Cuánto es {a} − {b}?");
        }

        List<string> hints;
        if (a < 20 && b < 10)
        {
            hints =
            [
                Tr($"Comece no {a} e conte {b} para trás.", $"Start at {a} and count {b} back.", $"Empieza en el {a} y cuenta {b} hacia atrás."),
                Tr($"Ou pense: quanto falta de {b} para chegar em {a}?", $"Or think: how much do you need to get from {b} to {a}?", $"O piensa: ¿cuánto falta de {b} para llegar a {a}?"),
            ];
        }
        else
        {
            hints = [Tr($"Comece pelas unidades: {a % 10} − {b % 10}.", $"Start with the ones: {a % 10} − {b % 10}.", $"Empieza por las unidades: {a % 10} − {b % 10}.")];
            if (borrow)
            {
                hints[0] = Tr($"Nas unidades, {a % 10} é menor que {b % 10}, então não dá para tirar direto.", $"In the ones, {a % 10} is less than {b % 10}, so you can't take it away directly.", $"En las unidades, {a % 10} es menor que {b % 10}, así que no se puede quitar directamente.");
                hints.Add(Tr($"Pegue emprestada 1 dezena: as unidades viram {a % 10 + 10} e as dezenas ficam {a / 10 - 1}.", $"Borrow 1 ten: the ones become {a % 10 + 10} and the tens become {a / 10 - 1}.", $"Pide prestada 1 decena: las unidades pasan a ser {a % 10 + 10} y las decenas quedan en {a / 10 - 1}."));
            }
            hints.Add(Tr("Depois subtraia as dezenas.", "Then subtract the tens.", "Después resta las decenas."));
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
            Tr($"Qual número completa a conta: {known} + ? = {total}", $"Which number completes the sum: {known} + ? = {total}", $"¿Qué número completa la cuenta: {known} + ? = {total}"),
            missing.ToString(),
            [
                Tr($"Quanto falta de {known} para chegar em {total}?", $"How much is needed to get from {known} to {total}?", $"¿Cuánto falta de {known} para llegar a {total}?"),
                Tr($"Você pode fazer a subtração {total} − {known}.", $"You can do the subtraction {total} − {known}.", $"Puedes hacer la resta {total} − {known}."),
            ],
            Tr($"{total} − {known} = {missing}, então {known} + {missing} = {total}", $"{total} − {known} = {missing}, so {known} + {missing} = {total}", $"{total} − {known} = {missing}, así que {known} + {missing} = {total}"));
        return new(content, 820 + total);
    }
}
