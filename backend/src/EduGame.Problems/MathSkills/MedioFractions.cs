using System.Globalization;
using EduGame.Problems.Tools;

using static EduGame.Problems.Lang;

namespace EduGame.Problems.MathSkills;

/// <summary>Frações: pintar, ler, comparar, somar com mesmo denominador e fração de quantidade.</summary>
public sealed class FractionsGenerator : ProblemGenerator
{
    public override string Key => "mat.fracoes";
    public override string SkillSlug => "fracoes-basicas";
    public override string Level => "medio";

    /// <summary>Opção "são iguais" das comparações.</summary>
    internal static string Equal => Tr("São iguais", "They're equal", "Son iguales");

    public override GeneratedProblem Generate(Random rng) => rng.Next(6) switch
    {
        0 => Paint(rng),
        1 => Read(rng),
        2 => CompareSameDenominator(rng),
        3 => CompareHarder(rng),
        4 => SumSameDenominator(rng),
        _ => OfQuantity(rng),
    };

    private static GeneratedProblem Paint(Random rng)
    {
        var den = Between(rng, 2, 8);
        var num = Between(rng, 1, den - 1);
        var doubled = den <= 5 && rng.Next(2) == 0; // pintar 1/2 numa figura com 4 partes, por exemplo
        var parts = doubled ? den * 2 : den;
        var answer = doubled ? num * 2 : num;
        var shape = rng.Next(2) == 0 ? "barra" : "circulo";
        var content = new ProblemContent(ProblemTypes.FracaoVisual,
            Tr($"Pinte {num}/{den} da figura.", $"Shade {num}/{den} of the shape.", $"Pinta {num}/{den} de la figura."),
            answer.ToString(),
            doubled
                ?
                [
                    Tr($"A figura tem {parts} partes, não {den}.", $"The shape has {parts} parts, not {den}.", $"La figura tiene {parts} partes, no {den}."),
                    Tr($"Cada 1/{den} da figura são 2 partes. Pinte {num} × 2 partes.", $"Each 1/{den} of the shape is 2 parts. Shade {num} × 2 parts.", $"Cada 1/{den} de la figura son 2 partes. Pinta {num} × 2 partes."),
                ]
                :
                [
                    Tr($"O número de baixo ({den}) diz em quantas partes iguais a figura está dividida.", $"The bottom number ({den}) tells you how many equal parts the shape is divided into.", $"El número de abajo ({den}) dice en cuántas partes iguales está dividida la figura."),
                    Tr($"Pinte {num} partes.", $"Shade {num} parts.", $"Pinta {num} partes."),
                ],
            doubled
                ? Tr($"{num}/{den} = {answer}/{parts}: pinte {answer} das {parts} partes.", $"{num}/{den} = {answer}/{parts}: shade {answer} of the {parts} parts.", $"{num}/{den} = {answer}/{parts}: pinta {answer} de las {parts} partes.")
                : Tr($"Pinte {num} das {den} partes.", $"Shade {num} of the {den} parts.", $"Pinta {num} de las {den} partes."))
        { Figure = new FractionFigure(shape, parts, 0) };
        return new(content, doubled ? 1150 : 900);
    }

    private static GeneratedProblem Read(Random rng)
    {
        var den = Between(rng, 3, 10);
        var num = Between(rng, 1, den - 1);
        var answer = $"{num}/{den}";
        var content = MultipleChoice(rng, Tr("Que fração da figura está pintada?", "What fraction of the shape is shaded?", "¿Qué fracción de la figura está pintada?"), answer,
            [$"{den - num}/{den}", $"{num}/{den - num}", $"{den}/{num}", $"{num}/{den + 1}"],
            [
                Tr("Conte quantas partes a figura tem ao todo: esse é o número de baixo.", "Count how many parts the shape has in all: that's the bottom number.", "Cuenta cuántas partes tiene la figura en total: ese es el número de abajo."),
                Tr("Conte as partes pintadas: esse é o número de cima.", "Count the shaded parts: that's the top number.", "Cuenta las partes pintadas: ese es el número de arriba."),
            ],
            Tr($"{num} de {den} partes estão pintadas: {num}/{den}.", $"{num} of {den} parts are shaded: {num}/{den}.", $"{num} de {den} partes están pintadas: {num}/{den}.")) with { Figure = new FractionFigure(rng.Next(2) == 0 ? "barra" : "circulo", den, num) };
        return new(content, 950);
    }

    private static GeneratedProblem CompareSameDenominator(Random rng)
    {
        var den = Between(rng, 4, 12);
        int a, b;
        do { a = Between(rng, 1, den - 1); b = Between(rng, 1, den - 1); } while (a == b);
        var bigger = Math.Max(a, b);
        var content = MultipleChoice(rng, WhichBigger($"{a}/{den}", $"{b}/{den}"), $"{bigger}/{den}",
            [$"{Math.Min(a, b)}/{den}", Equal],
            [
                Tr("As duas frações têm o mesmo denominador (partes do mesmo tamanho).", "Both fractions have the same denominator (parts of the same size).", "Las dos fracciones tienen el mismo denominador (partes del mismo tamaño)."),
                Tr("Com partes do mesmo tamanho, ganha quem tem mais partes.", "With parts of the same size, the one with more parts wins.", "Con partes del mismo tamaño, gana la que tiene más partes."),
            ],
            Tr($"Com o mesmo denominador, compare os numeradores: {bigger}/{den} é maior.", $"With the same denominator, compare the numerators: {bigger}/{den} is bigger.", $"Con el mismo denominador, compara los numeradores: {bigger}/{den} es mayor."), count: 3);
        return new(content, 1000);
    }

    private static string WhichBigger(string x, string y) =>
        Tr($"Qual fração é maior: {x} ou {y}?", $"Which fraction is bigger: {x} or {y}?", $"¿Qué fracción es mayor: {x} o {y}?");

    private static GeneratedProblem CompareHarder(Random rng)
    {
        if (rng.Next(3) == 0)
        {
            // Mesmo numerador: 1/3 > 1/5 (pegadinha comum)
            var num = Between(rng, 1, 3);
            int d1 = Between(rng, num + 1, 9), d2;
            do { d2 = Between(rng, num + 1, 9); } while (d2 == d1);
            var answer = $"{num}/{Math.Min(d1, d2)}";
            var content = MultipleChoice(rng, WhichBigger($"{num}/{d1}", $"{num}/{d2}"), answer,
                [$"{num}/{Math.Max(d1, d2)}", Equal],
                [
                    Tr("Quanto mais partes a figura é dividida, menor fica cada parte.", "The more parts a shape is divided into, the smaller each part gets.", "Cuantas más partes tiene la figura, más pequeña es cada parte."),
                    Tr($"1/{Math.Min(d1, d2)} é maior que 1/{Math.Max(d1, d2)}.", $"1/{Math.Min(d1, d2)} is bigger than 1/{Math.Max(d1, d2)}.", $"1/{Math.Min(d1, d2)} es mayor que 1/{Math.Max(d1, d2)}."),
                ],
                Tr($"Com o mesmo numerador, a fração com o menor denominador é a maior: {answer}.", $"With the same numerator, the fraction with the smaller denominator is bigger: {answer}.", $"Con el mismo numerador, la fracción con el menor denominador es la mayor: {answer}."), count: 3);
            return new(content, 1200);
        }

        var f1 = new Fraction(Between(rng, 1, 4), Between(rng, 2, 6));
        if (f1.Num >= f1.Den) f1 = new Fraction(1, f1.Den);
        var k = Between(rng, 2, 3);
        var equal = rng.Next(3) == 0;
        var f2 = equal ? new Fraction(f1.Num * k, f1.Den * k) : new Fraction(Between(rng, 1, 5), Between(rng, 2, 8));
        if (f2.Num >= f2.Den || (!equal && f2.Value == f1.Value)) f2 = new Fraction(1, f1.Den + 1);

        var same = f1.Value == f2.Value;
        var correct = same ? Equal : f1.Value > f2.Value ? f1.ToString() : f2.ToString();
        var c = MultipleChoice(rng, WhichBigger(f1.ToString(), f2.ToString()), correct,
            [f1.ToString(), f2.ToString(), Equal],
            [
                Tr("Transforme as duas frações para o mesmo denominador.", "Rewrite both fractions with the same denominator.", "Convierte las dos fracciones al mismo denominador."),
                Tr($"Multiplique em cruz: {f1.Num} × {f2.Den} = {f1.Num * f2.Den} e {f2.Num} × {f1.Den} = {f2.Num * f1.Den}.", $"Cross-multiply: {f1.Num} × {f2.Den} = {f1.Num * f2.Den} and {f2.Num} × {f1.Den} = {f2.Num * f1.Den}.", $"Multiplica en cruz: {f1.Num} × {f2.Den} = {f1.Num * f2.Den} y {f2.Num} × {f1.Den} = {f2.Num * f1.Den}."),
            ],
            same
                ? Tr($"{f1} e {f2} representam a mesma quantidade.", $"{f1} and {f2} represent the same amount.", $"{f1} y {f2} representan la misma cantidad.")
                : Tr($"{correct} é maior ({f1.Num * f2.Den} contra {f2.Num * f1.Den} quando comparadas com o mesmo denominador).", $"{correct} is bigger ({f1.Num * f2.Den} versus {f2.Num * f1.Den} when compared with the same denominator).", $"{correct} es mayor ({f1.Num * f2.Den} contra {f2.Num * f1.Den} al compararlas con el mismo denominador)."),
            count: 3);
        return new(c, equal ? 1350 : 1450);
    }

    private static GeneratedProblem SumSameDenominator(Random rng)
    {
        var den = Between(rng, 3, 12);
        var a = Between(rng, 1, den - 2);
        var b = Between(rng, 1, den - 1 - a);
        var answer = $"{a + b}/{den}";
        var content = MultipleChoice(rng, HowMuch($"{a}/{den} + {b}/{den}"), answer,
            [$"{a + b}/{den * 2}", $"{a * b}/{den}", $"{Math.Max(1, a + b - 1)}/{den}", $"{a + b + 1}/{den}"],
            [
                Tr("Com o mesmo denominador, o tamanho das partes não muda.", "With the same denominator, the size of the parts doesn't change.", "Con el mismo denominador, el tamaño de las partes no cambia."),
                Tr("Some só os numeradores e mantenha o denominador.", "Add only the numerators and keep the denominator.", "Suma solo los numeradores y mantén el denominador."),
            ],
            $"{a}/{den} + {b}/{den} = {answer}");
        return new(content, 1100);
    }

    private static GeneratedProblem OfQuantity(Random rng)
    {
        var den = Pick(rng, [2, 3, 4, 5, 10]);
        var num = Between(rng, 1, den - 1);
        var total = den * Between(rng, 2, 12);
        var answer = total / den * num;
        return new(Numeric(Tr($"Quanto é {num}/{den} de {total}?", $"What is {num}/{den} of {total}?", $"¿Cuánto es {num}/{den} de {total}?"), answer,
            [
                Tr($"Primeiro descubra quanto é 1/{den} de {total}: {total} ÷ {den}.", $"First find out what 1/{den} of {total} is: {total} ÷ {den}.", $"Primero averigua cuánto es 1/{den} de {total}: {total} ÷ {den}."),
                Tr($"Depois multiplique por {num}.", $"Then multiply by {num}.", $"Después multiplica por {num}."),
            ],
            $"{total} ÷ {den} = {total / den}; {total / den} × {num} = {answer}"), num == 1 ? 1250 : 1450);
    }
}

/// <summary>Números decimais e porcentagens simples.</summary>
public sealed class DecimalsPercentGenerator : ProblemGenerator
{
    public override string Key => "mat.decimais-porcentagem";
    public override string SkillSlug => "decimais-porcentagem";
    public override string Level => "medio";

    public override GeneratedProblem Generate(Random rng) => rng.Next(5) switch
    {
        0 => Percent(rng),
        1 => CompareDecimals(rng),
        2 => OrderDecimals(rng),
        3 => AddDecimals(rng),
        _ => Percent(rng),
    };

    private static GeneratedProblem Percent(Random rng)
    {
        var (p, difficulty) = Pick(rng, [(50, 950), (10, 1050), (25, 1100), (20, 1250), (75, 1350)]);
        var unit = 100 / MathTools.Gcd(p, 100);
        var total = unit * Between(rng, 1, 20);
        if (total < 20) total *= 4;
        var answer = total * p / 100m;
        var hint = p switch
        {
            50 => Tr("50% é a metade.", "50% is half.", "El 50 % es la mitad."),
            25 => Tr("25% é a quarta parte (metade da metade).", "25% is a quarter (half of half).", "El 25 % es la cuarta parte (la mitad de la mitad)."),
            10 => Tr("10% é dividir por 10.", "10% means dividing by 10.", "El 10 % es dividir entre 10."),
            20 => Tr("20% é o dobro de 10%.", "20% is double 10%.", "El 20 % es el doble del 10 %."),
            _ => Tr("75% é 50% + 25%.", "75% is 50% + 25%.", "El 75 % es 50 % + 25 %."),
        };
        return new(Numeric(Tr($"Quanto é {p}% de {total}?", $"What is {p}% of {total}?", $"¿Cuánto es el {p} % de {total}?"), answer,
            [hint, Tr($"{p}% significa {p} de cada 100.", $"{p}% means {p} out of every 100.", $"{p} % significa {p} de cada 100.")],
            Tr($"{p}% de {total} = {Num(answer)}", $"{p}% of {total} = {Num(answer)}", $"{p} % de {total} = {Num(answer)}"), decimals: true), difficulty);
    }

    private static GeneratedProblem CompareDecimals(Random rng)
    {
        // Pegadinha clássica: 0,7 é maior que 0,65 (mais algarismos não quer dizer maior).
        var a = Between(rng, 1, 9);
        var b = Between(rng, 10, 99);
        while (b / 10 == a) b = Between(rng, 10, 99);
        var x = a / 10m;
        var y = b / 100m;
        var answer = Num(Math.Max(x, y));
        var content = MultipleChoice(rng, Tr($"Qual número é maior: {Num(x)} ou {Num(y)}?", $"Which number is bigger: {Num(x)} or {Num(y)}?", $"¿Qué número es mayor: {Num(x)} o {Num(y)}?"), answer,
            [Num(Math.Min(x, y)), FractionsGenerator.Equal],
            [
                Tr("Escreva os dois com a mesma quantidade de casas decimais.", "Write both with the same number of decimal places.", "Escribe los dos con la misma cantidad de decimales."),
                Tr($"{Num(x)} = {Num(x)}0 (ou seja, {a * 10} centésimos).", $"{Num(x)} = {Num(x)}0 (that is, {a * 10} hundredths).", $"{Num(x)} = {Num(x)}0 (o sea, {a * 10} centésimas)."),
            ],
            Tr($"{Num(x)} = {a * 10} centésimos e {Num(y)} = {b} centésimos, então {answer} é maior.", $"{Num(x)} = {a * 10} hundredths and {Num(y)} = {b} hundredths, so {answer} is bigger.", $"{Num(x)} = {a * 10} centésimas y {Num(y)} = {b} centésimas, así que {answer} es mayor."), count: 3);
        return new(content, 1150);
    }

    private static GeneratedProblem OrderDecimals(Random rng)
    {
        var count = rng.Next(2) == 0 ? 3 : 4;
        var values = new HashSet<decimal>();
        while (values.Count < count)
        {
            values.Add(rng.Next(3) switch
            {
                0 => Between(rng, 1, 9) / 10m,
                1 => Between(rng, 1, 99) / 100m,
                _ => Between(rng, 1, 30) / 10m,
            });
        }
        var sorted = values.OrderBy(v => v).Select(Num).ToList();
        var content = new ProblemContent(ProblemTypes.Ordenar,
            Tr("Coloque os números em ordem, do menor para o maior.", "Put the numbers in order, from smallest to biggest.", "Ordena los números, de menor a mayor."),
            string.Join('|', sorted),
            [
                Tr("Compare primeiro a parte inteira (antes da vírgula).", "First compare the whole part (before the decimal point).", "Compara primero la parte entera (antes de la coma)."),
                Tr("Depois compare os décimos, e então os centésimos.", "Then compare the tenths, and then the hundredths.", "Después compara las décimas, y luego las centésimas."),
            ],
            Tr($"A ordem é: {string.Join(" < ", sorted)}.", $"The order is: {string.Join(" < ", sorted)}.", $"El orden es: {string.Join(" < ", sorted)}."))
        { Choices = [.. sorted.OrderBy(_ => rng.Next())] };
        return new(content, count == 3 ? 1100 : 1300);
    }

    private static GeneratedProblem AddDecimals(Random rng)
    {
        var a = Between(rng, 5, 99) / 10m;
        var b = Between(rng, 5, 199) / 100m;
        var sum = a + b;
        var twoPlaces = a.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', Lang.DecimalSeparator);
        return new(Numeric(HowMuch($"{Num(a)} + {Num(b)}"), sum,
            [
                Tr("Alinhe as vírgulas, uma embaixo da outra.", "Line up the decimal points, one under the other.", "Alinea las comas, una debajo de la otra."),
                Tr($"Escreva {Num(a)} como {twoPlaces} para os dois números terem duas casas.", $"Write {Num(a)} as {twoPlaces} so both numbers have two decimal places.", $"Escribe {Num(a)} como {twoPlaces} para que los dos números tengan dos decimales."),
            ],
            $"{Num(a)} + {Num(b)} = {Num(sum)}", decimals: true), 1400);
    }
}
