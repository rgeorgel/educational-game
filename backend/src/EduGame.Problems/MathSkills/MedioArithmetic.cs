namespace EduGame.Problems.MathSkills;

/// <summary>Tabuada completa (até 10 × 10), fator que falta e problemas com história.</summary>
public sealed class TimesTableGenerator : ProblemGenerator
{
    public override string Key => "mat.tabuada";
    public override string SkillSlug => "tabuada-completa";
    public override string Level => "medio";

    private static readonly int[] Hard = [6, 7, 8, 9];

    public override GeneratedProblem Generate(Random rng)
    {
        var a = Between(rng, 2, 10);
        var b = Between(rng, 2, 10);
        var product = a * b;
        var hardness = (Hard.Contains(a) ? 150 : 0) + (Hard.Contains(b) ? 150 : 0) + Math.Min(a, b) * 10;
        var difficulty = 880 + hardness;
        List<string> hints = [$"{a} × {b} é o mesmo que {b} × {a}. Use a tabuada que você conhece melhor.", $"Comece de um fato que você sabe: {a} × {b - 1} = {a * (b - 1)}. Agora some mais {a}."];

        return rng.Next(3) switch
        {
            0 => new(Numeric($"Qual número completa: {a} × ? = {product}", b, [$"Qual número vezes {a} dá {product}?", $"Você também pode dividir: {product} ÷ {a}."], $"{a} × {b} = {product}"), difficulty + 200),
            1 => new(Numeric($"Uma sala tem {a} fileiras com {b} cadeiras cada. Quantas cadeiras há?", product, hints, $"{a} × {b} = {product}"), difficulty + 80),
            _ => new(Numeric($"Quanto é {a} × {b}?", product, hints, $"{a} × {b} = {product}"), difficulty),
        };
    }
}

/// <summary>Divisão exata e com resto, incluindo arredondar para cima em situações reais.</summary>
public sealed class DivisionGenerator : ProblemGenerator
{
    public override string Key => "mat.divisao";
    public override string SkillSlug => "divisao";
    public override string Level => "medio";

    public override GeneratedProblem Generate(Random rng)
    {
        var b = Between(rng, 2, 10);
        var q = Between(rng, 2, 10);
        var hard = (b >= 6 ? 120 : 0) + (q >= 6 ? 80 : 0);

        switch (rng.Next(4))
        {
            case 0:
            {
                var a = b * q;
                return new(Numeric($"Quanto é {a} ÷ {b}?", q, [$"Pense na tabuada do {b}: {b} × ? = {a}.", "Dividir é repartir em partes iguais."], $"{a} ÷ {b} = {q}, porque {b} × {q} = {a}"), 920 + hard);
            }
            case 1:
            {
                var r = Between(rng, 1, b - 1);
                var a = b * q + r;
                return new(Numeric($"Qual é o resto de {a} ÷ {b}?", r, [$"Qual é o maior múltiplo de {b} que cabe em {a}?", $"{b} × {q} = {b * q}. Quanto sobra até {a}?"], $"{a} = {b} × {q} + {r}, então o resto é {r}."), 1150 + hard);
            }
            case 2:
            {
                var r = Between(rng, 1, b - 1);
                var a = b * q + r;
                return new(Numeric($"{a} balas vão ser divididas igualmente entre {b} crianças. Quantas balas inteiras cada uma recebe?", q, ["Divida as balas em partes iguais; o que sobrar não entra.", $"{b} × {q} = {b * q}, e sobram {r}."], $"{a} ÷ {b} = {q} e sobram {r} balas."), 1250 + hard);
            }
            default:
            {
                var seats = Pick(rng, [20, 30, 40, 45, 50]);
                var buses = Between(rng, 2, 6);
                var people = seats * (buses - 1) + Between(rng, 1, seats - 1);
                return new(Numeric($"Cada ônibus leva {seats} pessoas. Quantos ônibus são necessários para levar {people} pessoas?", buses, [$"{people} ÷ {seats} dá um número com resto. As pessoas que sobram também precisam de lugar!", "Quando sobra alguém, é preciso mais um ônibus."], $"{seats} × {buses - 1} = {seats * (buses - 1)} não é suficiente; com {buses} ônibus cabem {seats * buses}."), 1500 + hard / 2);
            }
        }
    }
}

/// <summary>Expressões com as quatro operações e ordem das operações.</summary>
public sealed class ExpressionsGenerator : ProblemGenerator
{
    public override string Key => "mat.expressoes";
    public override string SkillSlug => "expressoes";
    public override string Level => "medio";

    public override GeneratedProblem Generate(Random rng)
    {
        int a = Between(rng, 2, 12), b = Between(rng, 2, 9), c = Between(rng, 2, 9), d = Between(rng, 2, 9);
        var orderHint = "Primeiro multiplicações e divisões, depois somas e subtrações.";
        return rng.Next(6) switch
        {
            0 => Make($"{a} + {b} × {c}", a + b * c, [orderHint, $"{b} × {c} = {b * c}. Agora some {a}."], $"{b} × {c} = {b * c}; {a} + {b * c} = {a + b * c}", 1100),
            1 => Make($"({a} + {b}) × {c}", (a + b) * c, ["O que está entre parênteses vem primeiro.", $"{a} + {b} = {a + b}."], $"({a} + {b}) = {a + b}; {a + b} × {c} = {(a + b) * c}", 1050),
            2 => MakeDifferenceOfProducts(rng, orderHint),
            3 => Make($"{a * b} ÷ {b} + {c}", a + c, [orderHint, $"{a * b} ÷ {b} = {a}."], $"{a * b} ÷ {b} = {a}; {a} + {c} = {a + c}", 1150),
            4 => MakeNested(rng),
            _ => MakeLeftToRight(rng, orderHint),
        };

        static GeneratedProblem Make(string expr, int value, List<string> hints, string explanation, int difficulty) =>
            new(new ProblemContent(ProblemTypes.Numero, $"Quanto é {expr}?", value.ToString(), hints, explanation), difficulty);
    }

    private static GeneratedProblem MakeDifferenceOfProducts(Random rng, string orderHint)
    {
        int a, b, c, d;
        do { a = Between(rng, 3, 9); b = Between(rng, 3, 9); c = Between(rng, 2, 6); d = Between(rng, 2, 6); } while (a * b <= c * d);
        return new(new ProblemContent(ProblemTypes.Numero, $"Quanto é {a} × {b} − {c} × {d}?", (a * b - c * d).ToString(),
            [orderHint, $"{a} × {b} = {a * b} e {c} × {d} = {c * d}."], $"{a * b} − {c * d} = {a * b - c * d}"), 1300);
    }

    private static GeneratedProblem MakeNested(Random rng)
    {
        int a = Between(rng, 2, 6), b = Between(rng, 6, 12), c = Between(rng, 1, 5), d = Between(rng, 1, 15);
        var value = a * (b - c) + d;
        return new(new ProblemContent(ProblemTypes.Numero, $"Quanto é {a} × ({b} − {c}) + {d}?", value.ToString(),
            ["Comece pelos parênteses.", $"{b} − {c} = {b - c}; depois {a} × {b - c} = {a * (b - c)}."], $"{a} × {b - c} + {d} = {a * (b - c)} + {d} = {value}"), 1450);
    }

    private static GeneratedProblem MakeLeftToRight(Random rng, string orderHint)
    {
        // a − b ÷ c × d, com divisão exata e resultado positivo
        int c = Between(rng, 2, 5), q = Between(rng, 2, 6), d = Between(rng, 2, 4);
        var b = c * q;
        var a = q * d + Between(rng, 1, 30);
        var value = a - q * d;
        return new(new ProblemContent(ProblemTypes.Numero, $"Quanto é {a} − {b} ÷ {c} × {d}?", value.ToString(),
            [orderHint, "Multiplicação e divisão juntas: resolva da esquerda para a direita.", $"{b} ÷ {c} = {q}; {q} × {d} = {q * d}."],
            $"{b} ÷ {c} × {d} = {q * d}; {a} − {q * d} = {value}"), 1600);
    }
}

/// <summary>Perímetro e área de retângulos e figuras compostas.</summary>
public sealed class PerimeterAreaGenerator : ProblemGenerator
{
    public override string Key => "mat.perimetro-area";
    public override string SkillSlug => "perimetro-area";
    public override string Level => "medio";

    public override GeneratedProblem Generate(Random rng)
    {
        int w = Between(rng, 2, 12), h = Between(rng, 2, 9);
        return rng.Next(5) switch
        {
            0 => new(Numeric($"Um retângulo tem {w} cm de comprimento e {h} cm de largura. Qual é a área, em cm²?", w * h,
                ["Área é o espaço dentro da figura.", "Área do retângulo = comprimento × largura."], $"{w} × {h} = {w * h} cm²"), 950),
            1 => new(Numeric($"Um retângulo tem {w} cm de comprimento e {h} cm de largura. Qual é o perímetro, em cm?", 2 * (w + h),
                ["Perímetro é a volta inteira da figura.", "Some os quatro lados: dois de cada medida."], $"{w} + {h} + {w} + {h} = {2 * (w + h)} cm"), 1050),
            2 => Square(rng),
            3 => new(Numeric($"Um retângulo tem área de {w * h} cm² e {w} cm de comprimento. Qual é a largura, em cm?", h,
                ["Área = comprimento × largura.", $"{w} × ? = {w * h}"], $"{w * h} ÷ {w} = {h} cm"), 1350),
            _ => Composite(rng),
        };
    }

    private static GeneratedProblem Square(Random rng)
    {
        var side = Between(rng, 3, 15);
        return new(Numeric($"O perímetro de um quadrado é {4 * side} cm. Qual é a medida de cada lado, em cm?", side,
            ["O quadrado tem 4 lados iguais.", $"{4 * side} ÷ 4 = ?"], $"{4 * side} ÷ 4 = {side} cm"), 1200);
    }

    private static GeneratedProblem Composite(Random rng)
    {
        int w1 = Between(rng, 4, 10), h1 = Between(rng, 3, 8), w2 = Between(rng, 2, 6), h2 = Between(rng, 2, 6);
        var area = w1 * h1 + w2 * h2;
        return new(Numeric($"Uma figura é formada por dois retângulos que não se sobrepõem: um de {w1} cm × {h1} cm e outro de {w2} cm × {h2} cm. Qual é a área total, em cm²?", area,
            ["Calcule a área de cada retângulo separadamente.", $"{w1} × {h1} = {w1 * h1} e {w2} × {h2} = {w2 * h2}."], $"{w1 * h1} + {w2 * h2} = {area} cm²"), 1550);
    }
}
