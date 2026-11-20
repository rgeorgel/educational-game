using System.Globalization;
using EduGame.Problems.Tools;

namespace EduGame.Problems.MathSkills;

/// <summary>Frações: pintar, ler, comparar, somar com mesmo denominador e fração de quantidade.</summary>
public sealed class FractionsGenerator : ProblemGenerator
{
    public override string Key => "mat.fracoes";
    public override string SkillSlug => "fracoes-basicas";
    public override string Level => "medio";

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
            $"Pinte {num}/{den} da figura.",
            answer.ToString(),
            doubled
                ? [$"A figura tem {parts} partes, não {den}.", $"Cada 1/{den} da figura são 2 partes. Pinte {num} × 2 partes."]
                : [$"O número de baixo ({den}) diz em quantas partes iguais a figura está dividida.", $"Pinte {num} partes."],
            doubled ? $"{num}/{den} = {answer}/{parts}: pinte {answer} das {parts} partes." : $"Pinte {num} das {den} partes.")
        { Figure = new FractionFigure(shape, parts, 0) };
        return new(content, doubled ? 1150 : 900);
    }

    private static GeneratedProblem Read(Random rng)
    {
        var den = Between(rng, 3, 10);
        var num = Between(rng, 1, den - 1);
        var answer = $"{num}/{den}";
        var content = MultipleChoice(rng, "Que fração da figura está pintada?", answer,
            [$"{den - num}/{den}", $"{num}/{den - num}", $"{den}/{num}", $"{num}/{den + 1}"],
            ["Conte quantas partes a figura tem ao todo: esse é o número de baixo.", "Conte as partes pintadas: esse é o número de cima."],
            $"{num} de {den} partes estão pintadas: {num}/{den}.") with { Figure = new FractionFigure(rng.Next(2) == 0 ? "barra" : "circulo", den, num) };
        return new(content, 950);
    }

    private static GeneratedProblem CompareSameDenominator(Random rng)
    {
        var den = Between(rng, 4, 12);
        int a, b;
        do { a = Between(rng, 1, den - 1); b = Between(rng, 1, den - 1); } while (a == b);
        var bigger = Math.Max(a, b);
        var content = MultipleChoice(rng, $"Qual fração é maior: {a}/{den} ou {b}/{den}?", $"{bigger}/{den}",
            [$"{Math.Min(a, b)}/{den}", "São iguais"],
            ["As duas frações têm o mesmo denominador (partes do mesmo tamanho).", "Com partes do mesmo tamanho, ganha quem tem mais partes."],
            $"Com o mesmo denominador, compare os numeradores: {bigger}/{den} é maior.", count: 3);
        return new(content, 1000);
    }

    private static GeneratedProblem CompareHarder(Random rng)
    {
        if (rng.Next(3) == 0)
        {
            // Mesmo numerador: 1/3 > 1/5 (pegadinha comum)
            var num = Between(rng, 1, 3);
            int d1 = Between(rng, num + 1, 9), d2;
            do { d2 = Between(rng, num + 1, 9); } while (d2 == d1);
            var answer = $"{num}/{Math.Min(d1, d2)}";
            var content = MultipleChoice(rng, $"Qual fração é maior: {num}/{d1} ou {num}/{d2}?", answer,
                [$"{num}/{Math.Max(d1, d2)}", "São iguais"],
                ["Quanto mais partes a figura é dividida, menor fica cada parte.", $"1/{Math.Min(d1, d2)} é maior que 1/{Math.Max(d1, d2)}."],
                $"Com o mesmo numerador, a fração com o menor denominador é a maior: {answer}.", count: 3);
            return new(content, 1200);
        }

        var f1 = new Fraction(Between(rng, 1, 4), Between(rng, 2, 6));
        if (f1.Num >= f1.Den) f1 = new Fraction(1, f1.Den);
        var k = Between(rng, 2, 3);
        var equal = rng.Next(3) == 0;
        var f2 = equal ? new Fraction(f1.Num * k, f1.Den * k) : new Fraction(Between(rng, 1, 5), Between(rng, 2, 8));
        if (f2.Num >= f2.Den || (!equal && f2.Value == f1.Value)) f2 = new Fraction(1, f1.Den + 1);

        var correct = f1.Value == f2.Value ? "São iguais" : f1.Value > f2.Value ? f1.ToString() : f2.ToString();
        var c = MultipleChoice(rng, $"Qual fração é maior: {f1} ou {f2}?", correct,
            [f1.ToString(), f2.ToString(), "São iguais"],
            ["Transforme as duas frações para o mesmo denominador.", $"Multiplique em cruz: {f1.Num} × {f2.Den} = {f1.Num * f2.Den} e {f2.Num} × {f1.Den} = {f2.Num * f1.Den}."],
            correct == "São iguais" ? $"{f1} e {f2} representam a mesma quantidade." : $"{correct} é maior ({f1.Num * f2.Den} contra {f2.Num * f1.Den} quando comparadas com o mesmo denominador).",
            count: 3);
        return new(c, equal ? 1350 : 1450);
    }

    private static GeneratedProblem SumSameDenominator(Random rng)
    {
        var den = Between(rng, 3, 12);
        var a = Between(rng, 1, den - 2);
        var b = Between(rng, 1, den - 1 - a);
        var answer = $"{a + b}/{den}";
        var content = MultipleChoice(rng, $"Quanto é {a}/{den} + {b}/{den}?", answer,
            [$"{a + b}/{den * 2}", $"{a * b}/{den}", $"{Math.Max(1, a + b - 1)}/{den}", $"{a + b + 1}/{den}"],
            ["Com o mesmo denominador, o tamanho das partes não muda.", "Some só os numeradores e mantenha o denominador."],
            $"{a}/{den} + {b}/{den} = {answer}");
        return new(content, 1100);
    }

    private static GeneratedProblem OfQuantity(Random rng)
    {
        var den = Pick(rng, [2, 3, 4, 5, 10]);
        var num = Between(rng, 1, den - 1);
        var total = den * Between(rng, 2, 12);
        var answer = total / den * num;
        return new(Numeric($"Quanto é {num}/{den} de {total}?", answer,
            [$"Primeiro descubra quanto é 1/{den} de {total}: {total} ÷ {den}.", $"Depois multiplique por {num}."],
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
            50 => "50% é a metade.",
            25 => "25% é a quarta parte (metade da metade).",
            10 => "10% é dividir por 10.",
            20 => "20% é o dobro de 10%.",
            _ => "75% é 50% + 25%.",
        };
        return new(Numeric($"Quanto é {p}% de {total}?", answer, [hint, $"{p}% significa {p} de cada 100."], $"{p}% de {total} = {Num(answer)}", decimals: true), difficulty);
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
        var content = MultipleChoice(rng, $"Qual número é maior: {Num(x)} ou {Num(y)}?", answer,
            [Num(Math.Min(x, y)), "São iguais"],
            ["Escreva os dois com a mesma quantidade de casas decimais.", $"{Num(x)} = {Num(x)}0 (ou seja, {a * 10} centésimos)."],
            $"{Num(x)} = {a * 10} centésimos e {Num(y)} = {b} centésimos, então {answer} é maior.", count: 3);
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
            "Coloque os números em ordem, do menor para o maior.",
            string.Join('|', sorted),
            ["Compare primeiro a parte inteira (antes da vírgula).", "Depois compare os décimos, e então os centésimos."],
            $"A ordem é: {string.Join(" < ", sorted)}.")
        { Choices = [.. sorted.OrderBy(_ => rng.Next())] };
        return new(content, count == 3 ? 1100 : 1300);
    }

    private static GeneratedProblem AddDecimals(Random rng)
    {
        var a = Between(rng, 5, 99) / 10m;
        var b = Between(rng, 5, 199) / 100m;
        var sum = a + b;
        return new(Numeric($"Quanto é {Num(a)} + {Num(b)}?", sum,
            ["Alinhe as vírgulas, uma embaixo da outra.", $"Escreva {Num(a)} como {a.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',')} para os dois números terem duas casas."],
            $"{Num(a)} + {Num(b)} = {Num(sum)}", decimals: true), 1400);
    }
}
