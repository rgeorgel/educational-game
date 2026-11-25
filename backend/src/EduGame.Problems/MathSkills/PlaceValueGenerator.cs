using static EduGame.Problems.Lang;

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
            Tr($"{t} dezenas e {u} unidades formam qual número?", $"{t} tens and {u} ones make which number?", $"¿Qué número forman {t} decenas y {u} unidades?"),
            n.ToString(),
            [
                Tr($"Cada dezena vale 10. {t} dezenas = {t * 10}.", $"Each ten is worth 10. {t} tens = {t * 10}.", $"Cada decena vale 10. {t} decenas = {t * 10}."),
                Tr($"Agora junte as {u} unidades.", $"Now add the {u} ones.", $"Ahora junta las {u} unidades."),
            ],
            Tr($"{t} dezenas = {t * 10}; {t * 10} + {u} = {n}", $"{t} tens = {t * 10}; {t * 10} + {u} = {n}", $"{t} decenas = {t * 10}; {t * 10} + {u} = {n}"));
        return new(content, 280 + (u == 0 ? 60 : 0));
    }

    private static GeneratedProblem HowManyTens(Random rng)
    {
        var n = Between(rng, 11, 99);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"Quantas dezenas inteiras há em {n}?", $"How many whole tens are there in {n}?", $"¿Cuántas decenas enteras hay en {n}?"),
            (n / 10).ToString(),
            [
                Tr("Uma dezena é um grupo de 10.", "A ten is a group of 10.", "Una decena es un grupo de 10."),
                Tr($"Olhe o algarismo da esquerda em {n}: é a casa das dezenas.", $"Look at the left digit in {n}: that's the tens place.", $"Mira la cifra de la izquierda en {n}: es el lugar de las decenas."),
            ],
            Tr($"{n} = {n / 10} dezenas e {n % 10} unidades.", $"{n} = {n / 10} tens and {n % 10} ones.", $"{n} = {n / 10} decenas y {n % 10} unidades."));
        return new(content, 380);
    }

    private static GeneratedProblem DigitInPlace(Random rng)
    {
        var n = Between(rng, 11, 99);
        var units = rng.Next(2) == 0;
        var place = units ? Tr("unidades", "ones", "unidades") : Tr("dezenas", "tens", "decenas");
        var answer = units ? n % 10 : n / 10;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"Qual algarismo está na casa das {place} no número {n}?", $"Which digit is in the {place} place in the number {n}?", $"¿Qué cifra está en el lugar de las {place} en el número {n}?"),
            answer.ToString(),
            [Tr("Num número de dois algarismos, o da direita são as unidades e o da esquerda, as dezenas.", "In a two-digit number, the right digit is the ones and the left digit is the tens.", "En un número de dos cifras, la de la derecha son las unidades y la de la izquierda, las decenas.")],
            Tr($"Em {n}, as dezenas são {n / 10} e as unidades são {n % 10}.", $"In {n}, the tens are {n / 10} and the ones are {n % 10}.", $"En {n}, las decenas son {n / 10} y las unidades son {n % 10}."));
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
                Tr($"Quanto vale o algarismo {t} no número {n}?", $"What is the digit {t} worth in the number {n}?", $"¿Cuánto vale la cifra {t} en el número {n}?"),
                (t * 10).ToString(),
                [
                    Tr($"Em que casa está o {t}: unidades ou dezenas?", $"Which place is the {t} in: ones or tens?", $"¿En qué lugar está el {t}: unidades o decenas?"),
                    Tr($"O {t} está na casa das dezenas. Quanto valem {t} dezenas?", $"The {t} is in the tens place. What are {t} tens worth?", $"El {t} está en el lugar de las decenas. ¿Cuánto valen {t} decenas?"),
                ],
                Tr($"O {t} está na casa das dezenas, então vale {t} × 10 = {t * 10}.", $"The {t} is in the tens place, so it's worth {t} × 10 = {t * 10}.", $"El {t} está en el lugar de las decenas, así que vale {t} × 10 = {t * 10}."))
            { Choices = Choices(rng, (t * 10).ToString(), [t.ToString(), (t * 100).ToString(), n.ToString()]) };
            return new(content, 520);
        }
        else
        {
            var h = Between(rng, 2, 9);
            var n = h * 100 + Between(rng, 10, 99);
            var content = new ProblemContent(
                ProblemTypes.MultiplaEscolha,
                Tr($"Quanto vale o algarismo {h} no número {n}?", $"What is the digit {h} worth in the number {n}?", $"¿Cuánto vale la cifra {h} en el número {n}?"),
                (h * 100).ToString(),
                [
                    Tr("Num número de três algarismos, o primeiro da esquerda está na casa das centenas.", "In a three-digit number, the first digit on the left is in the hundreds place.", "En un número de tres cifras, la primera de la izquierda está en el lugar de las centenas."),
                    Tr("Uma centena vale 100.", "A hundred is worth 100.", "Una centena vale 100."),
                ],
                Tr($"O {h} está na casa das centenas, então vale {h} × 100 = {h * 100}.", $"The {h} is in the hundreds place, so it's worth {h} × 100 = {h * 100}.", $"El {h} está en el lugar de las centenas, así que vale {h} × 100 = {h * 100}."))
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

        var parts = new List<string>
        {
            Tr($"{c} centenas", $"{c} hundreds", $"{c} centenas"),
            Tr($"{t} dezenas", $"{t} tens", $"{t} decenas"),
            Tr($"{u} unidades", $"{u} ones", $"{u} unidades"),
        };
        if (shuffled) parts = [parts[2], parts[0], parts[1]];
        var description = Tr($"{parts[0]}, {parts[1]} e {parts[2]}", $"{parts[0]}, {parts[1]} and {parts[2]}", $"{parts[0]}, {parts[1]} y {parts[2]}");

        var difficulty = 650 + (t == 0 ? 150 : 0) + (shuffled ? 120 : 0);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"{description} formam qual número?", $"{description} make which number?", $"¿Qué número forman {description}?"),
            n.ToString(),
            [Tr("Cada centena vale 100 e cada dezena vale 10.", "Each hundred is worth 100 and each ten is worth 10.", "Cada centena vale 100 y cada decena vale 10."), $"{c * 100} + {t * 10} + {u} = ?"],
            $"{c} × 100 + {t} × 10 + {u} = {n}");
        return new(content, difficulty);
    }

    private static GeneratedProblem TensToNumber(Random rng)
    {
        var t = Between(rng, 11, 25);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"{t} dezenas formam qual número?", $"{t} tens make which number?", $"¿Qué número forman {t} decenas?"),
            (t * 10).ToString(),
            [Tr("Cada dezena vale 10.", "Each ten is worth 10.", "Cada decena vale 10."), Tr($"{t} dezenas = {t} × 10.", $"{t} tens = {t} × 10.", $"{t} decenas = {t} × 10.")],
            $"{t} × 10 = {t * 10}");
        return new(content, 950 + t * 4);
    }
}
