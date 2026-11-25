using static EduGame.Problems.Lang;

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
        List<string> hints =
        [
            Tr($"{a} × {b} é o mesmo que {b} × {a}. Use a tabuada que você conhece melhor.", $"{a} × {b} is the same as {b} × {a}. Use the times table you know best.", $"{a} × {b} es lo mismo que {b} × {a}. Usa la tabla que mejor conozcas."),
            Tr($"Comece de um fato que você sabe: {a} × {b - 1} = {a * (b - 1)}. Agora some mais {a}.", $"Start from a fact you know: {a} × {b - 1} = {a * (b - 1)}. Now add {a} more.", $"Empieza por algo que sabes: {a} × {b - 1} = {a * (b - 1)}. Ahora suma {a} más."),
        ];

        return rng.Next(3) switch
        {
            0 => new(Numeric(Tr($"Qual número completa: {a} × ? = {product}", $"Which number completes: {a} × ? = {product}", $"¿Qué número completa: {a} × ? = {product}"), b,
                [
                    Tr($"Qual número vezes {a} dá {product}?", $"Which number times {a} makes {product}?", $"¿Qué número por {a} da {product}?"),
                    Tr($"Você também pode dividir: {product} ÷ {a}.", $"You can also divide: {product} ÷ {a}.", $"También puedes dividir: {product} ÷ {a}."),
                ], $"{a} × {b} = {product}"), difficulty + 200),
            1 => new(Numeric(Tr($"Uma sala tem {a} fileiras com {b} cadeiras cada. Quantas cadeiras há?", $"A room has {a} rows with {b} chairs each. How many chairs are there?", $"Una sala tiene {a} filas con {b} sillas cada una. ¿Cuántas sillas hay?"), product, hints, $"{a} × {b} = {product}"), difficulty + 80),
            _ => new(Numeric(Tr($"Quanto é {a} × {b}?", $"What is {a} × {b}?", $"¿Cuánto es {a} × {b}?"), product, hints, $"{a} × {b} = {product}"), difficulty),
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
                return new(Numeric(Tr($"Quanto é {a} ÷ {b}?", $"What is {a} ÷ {b}?", $"¿Cuánto es {a} ÷ {b}?"), q,
                    [
                        Tr($"Pense na tabuada do {b}: {b} × ? = {a}.", $"Think of the {b} times table: {b} × ? = {a}.", $"Piensa en la tabla del {b}: {b} × ? = {a}."),
                        Tr("Dividir é repartir em partes iguais.", "Dividing is sharing into equal parts.", "Dividir es repartir en partes iguales."),
                    ],
                    Tr($"{a} ÷ {b} = {q}, porque {b} × {q} = {a}", $"{a} ÷ {b} = {q}, because {b} × {q} = {a}", $"{a} ÷ {b} = {q}, porque {b} × {q} = {a}")), 920 + hard);
            }
            case 1:
            {
                var r = Between(rng, 1, b - 1);
                var a = b * q + r;
                return new(Numeric(Tr($"Qual é o resto de {a} ÷ {b}?", $"What is the remainder of {a} ÷ {b}?", $"¿Cuál es el resto de {a} ÷ {b}?"), r,
                    [
                        Tr($"Qual é o maior múltiplo de {b} que cabe em {a}?", $"What is the biggest multiple of {b} that fits in {a}?", $"¿Cuál es el mayor múltiplo de {b} que cabe en {a}?"),
                        Tr($"{b} × {q} = {b * q}. Quanto sobra até {a}?", $"{b} × {q} = {b * q}. How much is left to reach {a}?", $"{b} × {q} = {b * q}. ¿Cuánto sobra hasta {a}?"),
                    ],
                    Tr($"{a} = {b} × {q} + {r}, então o resto é {r}.", $"{a} = {b} × {q} + {r}, so the remainder is {r}.", $"{a} = {b} × {q} + {r}, así que el resto es {r}.")), 1150 + hard);
            }
            case 2:
            {
                var r = Between(rng, 1, b - 1);
                var a = b * q + r;
                return new(Numeric(Tr($"{a} balas vão ser divididas igualmente entre {b} crianças. Quantas balas inteiras cada uma recebe?",
                                      $"{a} candies will be shared equally among {b} children. How many whole candies does each one get?",
                                      $"{a} caramelos se van a repartir por igual entre {b} niños. ¿Cuántos caramelos enteros recibe cada uno?"), q,
                    [
                        Tr("Divida as balas em partes iguais; o que sobrar não entra.", "Split the candies into equal parts; whatever is left over doesn't count.", "Reparte los caramelos en partes iguales; lo que sobre no cuenta."),
                        Tr($"{b} × {q} = {b * q}, e sobram {r}.", $"{b} × {q} = {b * q}, and {r} are left over.", $"{b} × {q} = {b * q}, y sobran {r}."),
                    ],
                    Tr($"{a} ÷ {b} = {q} e sobram {r} balas.", $"{a} ÷ {b} = {q} with {r} candies left over.", $"{a} ÷ {b} = {q} y sobran {r} caramelos.")), 1250 + hard);
            }
            default:
            {
                var seats = Pick(rng, [20, 30, 40, 45, 50]);
                var buses = Between(rng, 2, 6);
                var people = seats * (buses - 1) + Between(rng, 1, seats - 1);
                return new(Numeric(Tr($"Cada ônibus leva {seats} pessoas. Quantos ônibus são necessários para levar {people} pessoas?",
                                      $"Each bus carries {seats} people. How many buses are needed to carry {people} people?",
                                      $"Cada autobús lleva {seats} personas. ¿Cuántos autobuses hacen falta para llevar a {people} personas?"), buses,
                    [
                        Tr($"{people} ÷ {seats} dá um número com resto. As pessoas que sobram também precisam de lugar!", $"{people} ÷ {seats} has a remainder. The people left over need a seat too!", $"{people} ÷ {seats} da un número con resto. ¡Las personas que sobran también necesitan sitio!"),
                        Tr("Quando sobra alguém, é preciso mais um ônibus.", "When someone is left over, you need one more bus.", "Cuando sobra alguien, hace falta un autobús más."),
                    ],
                    Tr($"{seats} × {buses - 1} = {seats * (buses - 1)} não é suficiente; com {buses} ônibus cabem {seats * buses}.",
                       $"{seats} × {buses - 1} = {seats * (buses - 1)} isn't enough; {buses} buses fit {seats * buses}.",
                       $"{seats} × {buses - 1} = {seats * (buses - 1)} no alcanza; en {buses} autobuses caben {seats * buses}.")), 1500 + hard / 2);
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
        var orderHint = Tr("Primeiro multiplicações e divisões, depois somas e subtrações.", "First multiplication and division, then addition and subtraction.", "Primero multiplicaciones y divisiones, después sumas y restas.");
        return rng.Next(6) switch
        {
            0 => Make($"{a} + {b} × {c}", a + b * c, [orderHint, Tr($"{b} × {c} = {b * c}. Agora some {a}.", $"{b} × {c} = {b * c}. Now add {a}.", $"{b} × {c} = {b * c}. Ahora suma {a}.")], $"{b} × {c} = {b * c}; {a} + {b * c} = {a + b * c}", 1100),
            1 => Make($"({a} + {b}) × {c}", (a + b) * c, [Tr("O que está entre parênteses vem primeiro.", "What's inside the parentheses comes first.", "Lo que está entre paréntesis va primero."), $"{a} + {b} = {a + b}."], $"({a} + {b}) = {a + b}; {a + b} × {c} = {(a + b) * c}", 1050),
            2 => MakeDifferenceOfProducts(rng, orderHint),
            3 => Make($"{a * b} ÷ {b} + {c}", a + c, [orderHint, $"{a * b} ÷ {b} = {a}."], $"{a * b} ÷ {b} = {a}; {a} + {c} = {a + c}", 1150),
            4 => MakeNested(rng),
            _ => MakeLeftToRight(rng, orderHint),
        };

        static GeneratedProblem Make(string expr, int value, List<string> hints, string explanation, int difficulty) =>
            new(new ProblemContent(ProblemTypes.Numero, HowMuch(expr), value.ToString(), hints, explanation), difficulty);
    }

    private static GeneratedProblem MakeDifferenceOfProducts(Random rng, string orderHint)
    {
        int a, b, c, d;
        do { a = Between(rng, 3, 9); b = Between(rng, 3, 9); c = Between(rng, 2, 6); d = Between(rng, 2, 6); } while (a * b <= c * d);
        return new(new ProblemContent(ProblemTypes.Numero, HowMuch($"{a} × {b} − {c} × {d}"), (a * b - c * d).ToString(),
            [orderHint, Tr($"{a} × {b} = {a * b} e {c} × {d} = {c * d}.", $"{a} × {b} = {a * b} and {c} × {d} = {c * d}.", $"{a} × {b} = {a * b} y {c} × {d} = {c * d}.")], $"{a * b} − {c * d} = {a * b - c * d}"), 1300);
    }

    private static GeneratedProblem MakeNested(Random rng)
    {
        int a = Between(rng, 2, 6), b = Between(rng, 6, 12), c = Between(rng, 1, 5), d = Between(rng, 1, 15);
        var value = a * (b - c) + d;
        return new(new ProblemContent(ProblemTypes.Numero, HowMuch($"{a} × ({b} − {c}) + {d}"), value.ToString(),
            [
                Tr("Comece pelos parênteses.", "Start with the parentheses.", "Empieza por los paréntesis."),
                Tr($"{b} − {c} = {b - c}; depois {a} × {b - c} = {a * (b - c)}.", $"{b} − {c} = {b - c}; then {a} × {b - c} = {a * (b - c)}.", $"{b} − {c} = {b - c}; después {a} × {b - c} = {a * (b - c)}."),
            ], $"{a} × {b - c} + {d} = {a * (b - c)} + {d} = {value}"), 1450);
    }

    private static GeneratedProblem MakeLeftToRight(Random rng, string orderHint)
    {
        // a − b ÷ c × d, com divisão exata e resultado positivo
        int c = Between(rng, 2, 5), q = Between(rng, 2, 6), d = Between(rng, 2, 4);
        var b = c * q;
        var a = q * d + Between(rng, 1, 30);
        var value = a - q * d;
        return new(new ProblemContent(ProblemTypes.Numero, HowMuch($"{a} − {b} ÷ {c} × {d}"), value.ToString(),
            [orderHint, Tr("Multiplicação e divisão juntas: resolva da esquerda para a direita.", "Multiplication and division together: work from left to right.", "Multiplicación y división juntas: resuelve de izquierda a derecha."), $"{b} ÷ {c} = {q}; {q} × {d} = {q * d}."],
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
            0 => new(Numeric(Tr($"Um retângulo tem {w} cm de comprimento e {h} cm de largura. Qual é a área, em cm²?", $"A rectangle is {w} cm long and {h} cm wide. What is its area, in cm²?", $"Un rectángulo mide {w} cm de largo y {h} cm de ancho. ¿Cuál es el área, en cm²?"), w * h,
                [Tr("Área é o espaço dentro da figura.", "Area is the space inside the shape.", "El área es el espacio dentro de la figura."), Tr("Área do retângulo = comprimento × largura.", "Area of a rectangle = length × width.", "Área del rectángulo = largo × ancho.")], $"{w} × {h} = {w * h} cm²"), 950),
            1 => new(Numeric(Tr($"Um retângulo tem {w} cm de comprimento e {h} cm de largura. Qual é o perímetro, em cm?", $"A rectangle is {w} cm long and {h} cm wide. What is its perimeter, in cm?", $"Un rectángulo mide {w} cm de largo y {h} cm de ancho. ¿Cuál es el perímetro, en cm?"), 2 * (w + h),
                [Tr("Perímetro é a volta inteira da figura.", "The perimeter is the whole way around the shape.", "El perímetro es la vuelta entera a la figura."), Tr("Some os quatro lados: dois de cada medida.", "Add the four sides: two of each length.", "Suma los cuatro lados: dos de cada medida.")], $"{w} + {h} + {w} + {h} = {2 * (w + h)} cm"), 1050),
            2 => Square(rng),
            3 => new(Numeric(Tr($"Um retângulo tem área de {w * h} cm² e {w} cm de comprimento. Qual é a largura, em cm?", $"A rectangle has an area of {w * h} cm² and is {w} cm long. How wide is it, in cm?", $"Un rectángulo tiene un área de {w * h} cm² y {w} cm de largo. ¿Cuánto mide de ancho, en cm?"), h,
                [Tr("Área = comprimento × largura.", "Area = length × width.", "Área = largo × ancho."), $"{w} × ? = {w * h}"], $"{w * h} ÷ {w} = {h} cm"), 1350),
            _ => Composite(rng),
        };
    }

    private static GeneratedProblem Square(Random rng)
    {
        var side = Between(rng, 3, 15);
        return new(Numeric(Tr($"O perímetro de um quadrado é {4 * side} cm. Qual é a medida de cada lado, em cm?", $"The perimeter of a square is {4 * side} cm. How long is each side, in cm?", $"El perímetro de un cuadrado es {4 * side} cm. ¿Cuánto mide cada lado, en cm?"), side,
            [Tr("O quadrado tem 4 lados iguais.", "A square has 4 equal sides.", "El cuadrado tiene 4 lados iguales."), $"{4 * side} ÷ 4 = ?"], $"{4 * side} ÷ 4 = {side} cm"), 1200);
    }

    private static GeneratedProblem Composite(Random rng)
    {
        int w1 = Between(rng, 4, 10), h1 = Between(rng, 3, 8), w2 = Between(rng, 2, 6), h2 = Between(rng, 2, 6);
        var area = w1 * h1 + w2 * h2;
        return new(Numeric(Tr($"Uma figura é formada por dois retângulos que não se sobrepõem: um de {w1} cm × {h1} cm e outro de {w2} cm × {h2} cm. Qual é a área total, em cm²?",
                              $"A shape is made of two rectangles that don't overlap: one is {w1} cm × {h1} cm and the other is {w2} cm × {h2} cm. What is the total area, in cm²?",
                              $"Una figura está formada por dos rectángulos que no se superponen: uno de {w1} cm × {h1} cm y otro de {w2} cm × {h2} cm. ¿Cuál es el área total, en cm²?"), area,
            [
                Tr("Calcule a área de cada retângulo separadamente.", "Work out the area of each rectangle separately.", "Calcula el área de cada rectángulo por separado."),
                Tr($"{w1} × {h1} = {w1 * h1} e {w2} × {h2} = {w2 * h2}.", $"{w1} × {h1} = {w1 * h1} and {w2} × {h2} = {w2 * h2}.", $"{w1} × {h1} = {w1 * h1} y {w2} × {h2} = {w2 * h2}."),
            ], $"{w1 * h1} + {w2 * h2} = {area} cm²"), 1550);
    }
}
