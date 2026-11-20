namespace EduGame.Problems.MathSkills;

/// <summary>Valor posicional: unidades, dezenas e centenas.</summary>
public sealed class PlaceValueGenerator : ProblemGenerator
{
    public override string Key => "mat.valor-posicional";
    public override string SkillSlug => "dezenas-unidades";

    public override GeneratedProblem Generate(Random rng) => rng.Next(6) switch
    {
        0 => ComposeTwoDigits(rng),
        1 => HowManyTens(rng),
        2 => DigitInPlace(rng),
        3 => ValueOfDigit(rng),
        4 => ComposeThreeDigits(rng),
        _ => TensToNumber(rng),
    };

    private static GeneratedProblem ComposeTwoDigits(Random rng)
    {
        var t = Between(rng, 1, 9);
        var u = Between(rng, 0, 9);
        var n = t * 10 + u;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"{t} dezenas e {u} unidades formam qual número?",
            n.ToString(),
            [$"Cada dezena vale 10. {t} dezenas = {t * 10}.", $"Agora junte as {u} unidades."],
            $"{t} dezenas = {t * 10}; {t * 10} + {u} = {n}");
        return new(content, 280 + (u == 0 ? 60 : 0));
    }

    private static GeneratedProblem HowManyTens(Random rng)
    {
        var n = Between(rng, 11, 99);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Quantas dezenas inteiras há em {n}?",
            (n / 10).ToString(),
            ["Uma dezena é um grupo de 10.", $"Olhe o algarismo da esquerda em {n}: é a casa das dezenas."],
            $"{n} = {n / 10} dezenas e {n % 10} unidades.");
        return new(content, 380);
    }

    private static GeneratedProblem DigitInPlace(Random rng)
    {
        var n = Between(rng, 11, 99);
        var units = rng.Next(2) == 0;
        var place = units ? "unidades" : "dezenas";
        var answer = units ? n % 10 : n / 10;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Qual algarismo está na casa das {place} no número {n}?",
            answer.ToString(),
            ["Num número de dois algarismos, o da direita são as unidades e o da esquerda, as dezenas."],
            $"Em {n}, as dezenas são {n / 10} e as unidades são {n % 10}.");
        return new(content, units ? 300 : 330);
    }

    private static GeneratedProblem ValueOfDigit(Random rng)
    {
        if (rng.Next(2) == 0)
        {
            int t, u;
            do { t = Between(rng, 2, 9); u = Between(rng, 1, 9); } while (t == u);
            var n = t * 10 + u;
            var content = new ProblemContent(
                ProblemTypes.MultiplaEscolha,
                $"Quanto vale o algarismo {t} no número {n}?",
                (t * 10).ToString(),
                [$"Em que casa está o {t}: unidades ou dezenas?", $"O {t} está na casa das dezenas. Quanto valem {t} dezenas?"],
                $"O {t} está na casa das dezenas, então vale {t} × 10 = {t * 10}.")
            { Choices = Choices(rng, (t * 10).ToString(), [t.ToString(), (t * 100).ToString(), n.ToString()]) };
            return new(content, 520);
        }
        else
        {
            var h = Between(rng, 2, 9);
            var n = h * 100 + Between(rng, 10, 99);
            var content = new ProblemContent(
                ProblemTypes.MultiplaEscolha,
                $"Quanto vale o algarismo {h} no número {n}?",
                (h * 100).ToString(),
                ["Num número de três algarismos, o primeiro da esquerda está na casa das centenas.", "Uma centena vale 100."],
                $"O {h} está na casa das centenas, então vale {h} × 100 = {h * 100}.")
            { Choices = Choices(rng, (h * 100).ToString(), [h.ToString(), (h * 10).ToString(), n.ToString()]) };
            return new(content, 700);
        }
    }

    private static GeneratedProblem ComposeThreeDigits(Random rng)
    {
        var c = Between(rng, 1, 9);
        var t = rng.Next(3) == 0 ? 0 : Between(rng, 1, 9);
        var u = Between(rng, 1, 9);
        var n = c * 100 + t * 10 + u;
        var shuffled = rng.Next(3) == 0;

        var parts = new List<string> { $"{c} centenas", $"{t} dezenas", $"{u} unidades" };
        if (shuffled) parts = [parts[2], parts[0], parts[1]];
        var description = $"{parts[0]}, {parts[1]} e {parts[2]}";

        var difficulty = 650 + (t == 0 ? 150 : 0) + (shuffled ? 120 : 0);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"{description} formam qual número?",
            n.ToString(),
            ["Cada centena vale 100 e cada dezena vale 10.", $"{c * 100} + {t * 10} + {u} = ?"],
            $"{c} × 100 + {t} × 10 + {u} = {n}");
        return new(content, difficulty);
    }

    private static GeneratedProblem TensToNumber(Random rng)
    {
        var t = Between(rng, 11, 25);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"{t} dezenas formam qual número?",
            (t * 10).ToString(),
            ["Cada dezena vale 10.", $"{t} dezenas = {t} × 10."],
            $"{t} × 10 = {t * 10}");
        return new(content, 950 + t * 4);
    }
}
