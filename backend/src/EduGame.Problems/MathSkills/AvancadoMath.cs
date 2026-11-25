using EduGame.Problems.Tools;
using static EduGame.Problems.Tools.MathTools;
using static EduGame.Problems.Lang;

namespace EduGame.Problems.MathSkills;

/// <summary>Números inteiros (negativos): as quatro operações e situações do dia a dia.</summary>
public sealed class IntegersGenerator : ProblemGenerator
{
    public override string Key => "mat.inteiros";
    public override string SkillSlug => "numeros-inteiros";
    public override string Level => "avancado";

    public override GeneratedProblem Generate(Random rng)
    {
        int a = Between(rng, 2, 12), b = Between(rng, 2, 12);
        var rule = Tr("Sinais iguais na multiplicação/divisão dão positivo; sinais diferentes dão negativo.",
                      "In multiplication/division, matching signs give a positive result; different signs give a negative one.",
                      "En la multiplicación/división, signos iguales dan positivo; signos distintos dan negativo.");
        return rng.Next(6) switch
        {
            0 => Make(HowMuch($"(−{a}) + {b}"), -a + b, [Tr($"Imagine a reta numérica: comece em −{a} e ande {b} para a direita.", $"Picture the number line: start at −{a} and move {b} to the right.", $"Imagina la recta numérica: empieza en −{a} y avanza {b} hacia la derecha.")], 1450),
            1 => Make(HowMuch($"−{a} − {b}"), -a - b, [Tr("Já está abaixo de zero e vai descer mais.", "It's already below zero and goes down even more.", "Ya está bajo cero y va a bajar más."), Tr($"Some as distâncias: {a} + {b}, e o resultado é negativo.", $"Add the distances: {a} + {b}, and the result is negative.", $"Suma las distancias: {a} + {b}, y el resultado es negativo.")], 1550),
            2 => Make(HowMuch($"(−{a}) × (−{Math.Min(b, 9)})"), a * Math.Min(b, 9), [rule], 1650),
            3 => Make(HowMuch($"{a * b} ÷ (−{b})"), -a, [rule], 1650),
            4 => Temperature(rng),
            _ => Make(HowMuch($"−{a} + (−{Math.Min(b, 6)}) × 3"), -a - Math.Min(b, 6) * 3, [Tr("Primeiro a multiplicação.", "Multiplication first.", "Primero la multiplicación."), rule], 1900),
        };

        static GeneratedProblem Make(string prompt, int value, List<string> hints, int difficulty) =>
            new(Numeric(prompt, value, hints, Tr($"O resultado é {Minus(value)}.", $"The result is {Minus(value)}.", $"El resultado es {Minus(value)}."), negatives: true), difficulty);
    }

    private static GeneratedProblem Temperature(Random rng)
    {
        int start = -Between(rng, 1, 10), change = Between(rng, 3, 15);
        var up = rng.Next(2) == 0;
        var end = up ? start + change : start - change;
        var moved = up ? Tr("subiu", "rose", "subió") : Tr("caiu", "dropped", "bajó");
        return new(Numeric(Tr($"De madrugada, a temperatura era {Minus(start)} °C. Depois ela {moved} {change} °C. Qual é a nova temperatura, em °C?",
                              $"Before dawn, the temperature was {Minus(start)} °C. Then it {moved} {change} °C. What is the new temperature, in °C?",
                              $"De madrugada, la temperatura era de {Minus(start)} °C. Después {moved} {change} °C. ¿Cuál es la nueva temperatura, en °C?"), end,
            [
                Tr("Use a reta numérica (como um termômetro).", "Use the number line (like a thermometer).", "Usa la recta numérica (como un termómetro)."),
                up ? Tr("Subir é somar.", "Rising means adding.", "Subir es sumar.") : Tr("Cair é subtrair.", "Dropping means subtracting.", "Bajar es restar."),
            ], $"{Minus(start)} {(up ? "+" : "−")} {change} = {Minus(end)} °C", negatives: true), 1500);
    }
}

/// <summary>Potências e raízes quadradas.</summary>
public sealed class PowersRootsGenerator : ProblemGenerator
{
    public override string Key => "mat.potencias-raizes";
    public override string SkillSlug => "potencias-raizes";
    public override string Level => "avancado";

    public override GeneratedProblem Generate(Random rng)
    {
        switch (rng.Next(5))
        {
            case 0:
            {
                var (b, e) = Pick(rng, [(2, 3), (2, 4), (2, 5), (3, 2), (3, 3), (4, 2), (5, 2), (5, 3), (10, 3), (2, 6), (3, 4)]);
                var v = (int)Math.Pow(b, e);
                return new(Numeric(HowMuch($"{b}{Superscript(e)}"), v,
                    [
                        Tr($"{b}{Superscript(e)} é {b} multiplicado por ele mesmo {e} vezes.", $"{b}{Superscript(e)} is {b} multiplied by itself {e} times.", $"{b}{Superscript(e)} es {b} multiplicado por sí mismo {e} veces."),
                        string.Join(" × ", Enumerable.Repeat(b, e)),
                    ], $"{string.Join(" × ", Enumerable.Repeat(b, e))} = {v}"), 1450 + e * 40);
            }
            case 1:
            {
                var r = Between(rng, 4, 15);
                return new(Numeric(HowMuch($"√{r * r}"), r,
                    [
                        Tr("A raiz quadrada é o número que, multiplicado por ele mesmo, dá o valor de dentro.", "The square root is the number that, multiplied by itself, gives the value inside.", "La raíz cuadrada es el número que, multiplicado por sí mismo, da el valor de dentro."),
                        $"? × ? = {r * r}",
                    ], Tr($"{r} × {r} = {r * r}, então √{r * r} = {r}", $"{r} × {r} = {r * r}, so √{r * r} = {r}", $"{r} × {r} = {r * r}, así que √{r * r} = {r}")), 1400 + r * 15);
            }
            case 2:
            {
                int a = Between(rng, 2, 5), b = Between(rng, 2, 4);
                var v = a * a * a + b * b;
                return new(Numeric(HowMuch($"{a}³ + {b}²"), v, [$"{a}³ = {a} × {a} × {a}.", $"{b}² = {b} × {b}."], $"{a * a * a} + {b * b} = {v}"), 1700);
            }
            case 3:
            {
                int r = Between(rng, 2, 9), e = Between(rng, 2, 3);
                var v = r * (int)Math.Pow(2, e);
                return new(Numeric(HowMuch($"√{r * r} × 2{Superscript(e)}"), v, [$"√{r * r} = {r}.", $"2{Superscript(e)} = {(int)Math.Pow(2, e)}."], $"{r} × {(int)Math.Pow(2, e)} = {v}"), 1950);
            }
            default:
            {
                var r = Between(rng, 11, 20);
                return new(Numeric(Tr($"Qual número positivo, elevado ao quadrado, dá {r * r}?", $"Which positive number, squared, gives {r * r}?", $"¿Qué número positivo, elevado al cuadrado, da {r * r}?"), r,
                    [
                        Tr("Procure um número que multiplicado por ele mesmo dê o valor.", "Look for a number that, multiplied by itself, gives the value.", "Busca un número que multiplicado por sí mismo dé el valor."),
                        Tr($"Ele está entre 10 e 20. Teste o último algarismo: que número ao quadrado termina em {r * r % 10}?", $"It's between 10 and 20. Test the last digit: which number squared ends in {r * r % 10}?", $"Está entre 10 y 20. Prueba la última cifra: ¿qué número al cuadrado termina en {r * r % 10}?"),
                    ], $"{r}² = {r * r}"), 1650 + (r - 11) * 25);
            }
        }
    }
}

/// <summary>MMC e MDC, com problemas de aplicação.</summary>
public sealed class GcdLcmGenerator : ProblemGenerator
{
    public override string Key => "mat.mmc-mdc";
    public override string SkillSlug => "mmc-mdc";
    public override string Level => "avancado";

    public override GeneratedProblem Generate(Random rng)
    {
        var g = Between(rng, 2, 9);
        int x, y;
        do { x = Between(rng, 2, 7); y = Between(rng, 2, 7); } while (x == y || Gcd(x, y) != 1);
        int a = g * x, b = g * y;

        return rng.Next(4) switch
        {
            0 => new(Numeric(Tr($"Qual é o MDC (máximo divisor comum) de {a} e {b}?", $"What is the GCD (greatest common divisor) of {a} and {b}?", $"¿Cuál es el MCD (máximo común divisor) de {a} y {b}?"), g,
                [
                    Tr("Liste os divisores de cada número.", "List the divisors of each number.", "Haz la lista de los divisores de cada número."),
                    Tr($"Qual é o maior número que divide {a} e {b} sem sobrar resto?", $"What is the biggest number that divides {a} and {b} with no remainder?", $"¿Cuál es el mayor número que divide a {a} y a {b} sin que sobre resto?"),
                ], $"{GcdName()}({a}, {b}) = {g}"), 1500 + g * 20),
            1 => new(Numeric(Tr($"Qual é o MMC (mínimo múltiplo comum) de {a} e {b}?", $"What is the LCM (least common multiple) of {a} and {b}?", $"¿Cuál es el MCM (mínimo común múltiplo) de {a} y {b}?"), Lcm(a, b),
                [
                    Tr("Liste os múltiplos do maior número até achar um que também seja múltiplo do outro.", "List the multiples of the bigger number until you find one that's also a multiple of the other.", "Haz la lista de los múltiplos del número mayor hasta encontrar uno que también sea múltiplo del otro."),
                    $"{LcmName()} × {GcdName()} = {a} × {b}.",
                ], $"{LcmName()}({a}, {b}) = {Lcm(a, b)}"), 1600 + Math.Min(300, Lcm(a, b))),
            2 => Buses(rng),
            _ => new(Numeric(Tr($"Uma professora tem {a} lápis e {b} borrachas. Ela quer montar kits iguais, sem sobrar nada, com o maior número possível de kits. Quantos kits ela consegue montar?",
                                $"A teacher has {a} pencils and {b} erasers. She wants to make identical kits, with nothing left over, and as many kits as possible. How many kits can she make?",
                                $"Una profesora tiene {a} lápices y {b} gomas. Quiere armar kits iguales, sin que sobre nada, con el mayor número posible de kits. ¿Cuántos kits puede armar?"), g,
                [
                    Tr("O número de kits precisa dividir os lápis e as borrachas.", "The number of kits has to divide both the pencils and the erasers.", "El número de kits tiene que dividir los lápices y las gomas."),
                    Tr("O maior divisor comum é a resposta.", "The greatest common divisor is the answer.", "El máximo común divisor es la respuesta."),
                ], $"{GcdName()}({a}, {b}) = {g} kits"), 1800),
        };
    }

    private static string GcdName() => Tr("MDC", "GCD", "MCD");

    private static string LcmName() => Tr("MMC", "LCM", "MCM");

    private static GeneratedProblem Buses(Random rng)
    {
        var (a, b) = Pick(rng, [(12, 18), (10, 15), (8, 12), (15, 20), (6, 10), (9, 12), (20, 30)]);
        var l = Lcm(a, b);
        return new(Numeric(Tr($"Um ônibus passa no ponto a cada {a} minutos e outro a cada {b} minutos. Os dois passaram juntos agora. Daqui a quantos minutos vão passar juntos de novo?",
                              $"One bus stops here every {a} minutes and another every {b} minutes. They just came by together. In how many minutes will they come by together again?",
                              $"Un autobús pasa por la parada cada {a} minutos y otro cada {b} minutos. Los dos acaban de pasar juntos. ¿Dentro de cuántos minutos volverán a pasar juntos?"), l,
            [
                Tr($"Você precisa de um número que seja múltiplo de {a} e de {b}.", $"You need a number that is a multiple of both {a} and {b}.", $"Necesitas un número que sea múltiplo de {a} y de {b}."),
                Tr("É o menor múltiplo comum (MMC).", "It's the least common multiple (LCM).", "Es el mínimo común múltiplo (MCM)."),
            ], Tr($"MMC({a}, {b}) = {l} minutos", $"LCM({a}, {b}) = {l} minutes", $"MCM({a}, {b}) = {l} minutos")), 1950);
    }
}

/// <summary>Operações com frações de denominadores diferentes e simplificação.</summary>
public sealed class FractionOperationsGenerator : ProblemGenerator
{
    public override string Key => "mat.fracoes-operacoes";
    public override string SkillSlug => "fracoes-operacoes";
    public override string Level => "avancado";

    public override GeneratedProblem Generate(Random rng)
    {
        var f1 = Proper(rng);
        var f2 = Proper(rng);
        while (f2.Den == f1.Den) f2 = Proper(rng);

        switch (rng.Next(5))
        {
            case 0:
            case 1:
            {
                var r = f1 + f2;
                return new(MultipleChoice(rng, HowMuch($"{f1} + {f2}"), r.ToString(),
                    [new Fraction(f1.Num + f2.Num, f1.Den + f2.Den).ToString(), (f1 * f2).ToString(), new Fraction(f1.Num + f2.Num, f1.Den * f2.Den).Simplify().ToString(), (f1 - f2).ToString()],
                    [
                        Tr("Não some os denominadores! Primeiro deixe as frações com o mesmo denominador.", "Don't add the denominators! First give the fractions the same denominator.", "¡No sumes los denominadores! Primero pon las fracciones con el mismo denominador."),
                        Tr($"Use o MMC de {f1.Den} e {f2.Den}: {MathTools.Lcm(f1.Den, f2.Den)}.", $"Use the LCM of {f1.Den} and {f2.Den}: {MathTools.Lcm(f1.Den, f2.Den)}.", $"Usa el MCM de {f1.Den} y {f2.Den}: {MathTools.Lcm(f1.Den, f2.Den)}."),
                    ],
                    Tr($"{f1} + {f2} = {r} (já simplificada)", $"{f1} + {f2} = {r} (already simplified)", $"{f1} + {f2} = {r} (ya simplificada)")), 1500 + (f1.Den * f2.Den > 20 ? 150 : 0));
            }
            case 2:
            {
                var r = f1 * f2;
                return new(MultipleChoice(rng, HowMuch($"{f1} × {f2}"), r.ToString(),
                    [(f1 + f2).ToString(), new Fraction(f1.Num * f2.Den, f1.Den * f2.Num).Simplify().ToString(), new Fraction(f1.Num + f2.Num, f1.Den * f2.Den).Simplify().ToString()],
                    [
                        Tr("Na multiplicação, multiplique numerador com numerador e denominador com denominador.", "To multiply, multiply numerator by numerator and denominator by denominator.", "En la multiplicación, multiplica numerador por numerador y denominador por denominador."),
                        Tr("Simplifique no final.", "Simplify at the end.", "Simplifica al final."),
                    ],
                    $"{f1} × {f2} = {r}"), 1650);
            }
            case 3:
            {
                var r = f1 / f2;
                return new(MultipleChoice(rng, HowMuch($"{f1} ÷ {f2}"), r.ToString(),
                    [(f1 * f2).ToString(), new Fraction(f2.Num * f1.Den, f2.Den * f1.Num).Simplify().ToString(), (f1 - f2).ToString()],
                    [
                        Tr("Dividir por uma fração é multiplicar pelo inverso dela.", "Dividing by a fraction is multiplying by its reciprocal.", "Dividir entre una fracción es multiplicar por su inversa."),
                        Tr($"O inverso de {f2} é {f2.Den}/{f2.Num}.", $"The reciprocal of {f2} is {f2.Den}/{f2.Num}.", $"La inversa de {f2} es {f2.Den}/{f2.Num}."),
                    ],
                    $"{f1} × {f2.Den}/{f2.Num} = {r}"), 1950);
            }
            default:
            {
                var simple = Proper(rng).Simplify();
                var k = Between(rng, 2, 6);
                var big = new Fraction(simple.Num * k, simple.Den * k);
                return new(MultipleChoice(rng, Tr($"Qual é a forma simplificada de {big}?", $"What is the simplest form of {big}?", $"¿Cuál es la forma simplificada de {big}?"), simple.ToString(),
                    [new Fraction(simple.Num * 2, simple.Den * 2).ToString(), new Fraction(simple.Num + 1, simple.Den).ToString(), new Fraction(simple.Num, simple.Den + 1).ToString()],
                    [
                        Tr("Divida o numerador e o denominador pelo mesmo número.", "Divide the numerator and the denominator by the same number.", "Divide el numerador y el denominador entre el mismo número."),
                        Tr($"Os dois são divisíveis por {k}.", $"Both are divisible by {k}.", $"Los dos son divisibles por {k}."),
                    ],
                    Tr($"{big.Num} ÷ {k} = {simple.Num} e {big.Den} ÷ {k} = {simple.Den}", $"{big.Num} ÷ {k} = {simple.Num} and {big.Den} ÷ {k} = {simple.Den}", $"{big.Num} ÷ {k} = {simple.Num} y {big.Den} ÷ {k} = {simple.Den}")), 1450 + k * 20);
            }
        }
    }

    private static Fraction Proper(Random rng)
    {
        var den = Between(rng, 2, 9);
        return new Fraction(Between(rng, 1, den - 1), den).Simplify();
    }
}

/// <summary>Razão, proporção (regra de três) e porcentagem.</summary>
public sealed class ProportionGenerator : ProblemGenerator
{
    public override string Key => "mat.proporcao";
    public override string SkillSlug => "proporcao-porcentagem";
    public override string Level => "avancado";

    public override GeneratedProblem Generate(Random rng)
    {
        switch (rng.Next(5))
        {
            case 0:
            {
                int n1 = Between(rng, 2, 5), price = Between(rng, 2, 9), n2 = Between(rng, 6, 15);
                return new(Numeric(Tr($"{n1} cadernos custam R$ {n1 * price}. Quanto custam {n2} cadernos, em reais?", $"{n1} notebooks cost ${n1 * price}. How much do {n2} notebooks cost, in dollars?", $"{n1} cuadernos cuestan ${n1 * price}. ¿Cuánto cuestan {n2} cuadernos?"), n2 * price,
                    [Tr("Descubra quanto custa 1 caderno.", "Find out how much 1 notebook costs.", "Averigua cuánto cuesta 1 cuaderno."), $"{n1 * price} ÷ {n1} = {price}."],
                    Tr($"1 caderno = R$ {price}; {n2} × {price} = R$ {n2 * price}", $"1 notebook = ${price}; {n2} × {price} = ${n2 * price}", $"1 cuaderno = ${price}; {n2} × {price} = ${n2 * price}")) with { AcceptsDecimal = true }, 1500);
            }
            case 1:
            {
                var (w1, d1, w2) = Pick(rng, [(4, 6, 8), (3, 8, 6), (2, 12, 6), (5, 6, 10), (6, 10, 4), (4, 9, 6)]);
                var d2 = w1 * d1 / w2;
                return new(Numeric(Tr($"{w1} pintores pintam uma casa em {d1} dias. Trabalhando no mesmo ritmo, em quantos dias {w2} pintores pintam a mesma casa?",
                                      $"{w1} painters paint a house in {d1} days. Working at the same pace, in how many days do {w2} painters paint the same house?",
                                      $"{w1} pintores pintan una casa en {d1} días. Trabajando al mismo ritmo, ¿en cuántos días pintan {w2} pintores la misma casa?"), d2,
                    [
                        Tr("Mais pintores → menos dias (grandezas inversamente proporcionais).", "More painters → fewer days (inversely proportional quantities).", "Más pintores → menos días (magnitudes inversamente proporcionales)."),
                        Tr($"O trabalho total é {w1} × {d1} = {w1 * d1} 'dias de pintor'.", $"The total work is {w1} × {d1} = {w1 * d1} 'painter-days'.", $"El trabajo total es {w1} × {d1} = {w1 * d1} 'días de pintor'."),
                    ], Tr($"{w1 * d1} ÷ {w2} = {d2} dias", $"{w1 * d1} ÷ {w2} = {d2} days", $"{w1 * d1} ÷ {w2} = {d2} días")) with { AcceptsDecimal = true }, 1800);
            }
            case 2:
            {
                var p = Pick(rng, [15, 30, 40, 35, 60]);
                var total = 20 * Between(rng, 2, 20);
                return new(Numeric(Tr($"Quanto é {p}% de {total}?", $"What is {p}% of {total}?", $"¿Cuánto es el {p} % de {total}?"), total * p / 100m,
                    [
                        Tr("Calcule 10% (dividir por 10) e use para montar a porcentagem.", "Work out 10% (divide by 10) and use it to build the percentage.", "Calcula el 10 % (dividir entre 10) y úsalo para armar el porcentaje."),
                        $"{p}% = {p} ÷ 100.",
                    ], $"{total} × {p} ÷ 100 = {Num(total * p / 100m)}", decimals: true), 1600);
            }
            case 3:
            {
                var p = Pick(rng, [10, 20, 25, 50]);
                var price = 20 * Between(rng, 2, 15);
                var end = price + price * p / 100;
                return new(Numeric(Tr($"Um produto custava R$ {price} e teve um aumento de {p}%. Qual é o novo preço, em reais?",
                                      $"A product cost ${price} and its price went up {p}%. What is the new price, in dollars?",
                                      $"Un producto costaba ${price} y subió un {p} %. ¿Cuál es el nuevo precio?"), end,
                    [
                        Tr($"Calcule {p}% de {price}.", $"Work out {p}% of {price}.", $"Calcula el {p} % de {price}."),
                        Tr("Some o aumento ao preço antigo.", "Add the increase to the old price.", "Suma el aumento al precio anterior."),
                    ],
                    Tr($"{p}% de {price} = {price * p / 100}; {price} + {price * p / 100} = R$ {end}", $"{p}% of {price} = {price * p / 100}; {price} + {price * p / 100} = ${end}", $"{p} % de {price} = {price * p / 100}; {price} + {price * p / 100} = ${end}")) with { AcceptsDecimal = true }, 1750);
            }
            default:
            {
                var p = Pick(rng, [15, 30, 35]);
                var price = 20 * Between(rng, 2, 10);
                var end = price - price * p / 100m;
                return new(Numeric(Tr($"Uma bicicleta de R$ {price} está com {p}% de desconto. Quanto ela custa com o desconto, em reais?",
                                      $"A ${price} bike is {p}% off. How much does it cost with the discount, in dollars?",
                                      $"Una bicicleta de ${price} tiene un {p} % de descuento. ¿Cuánto cuesta con el descuento?"), end,
                    [
                        Tr($"Calcule {p}% de {price}.", $"Work out {p}% of {price}.", $"Calcula el {p} % de {price}."),
                        Tr("Tire o desconto do preço.", "Take the discount off the price.", "Quita el descuento del precio."),
                    ],
                    Tr($"{p}% de {price} = {Num(price * p / 100m)}; {price} − {Num(price * p / 100m)} = R$ {Num(end)}", $"{p}% of {price} = {Num(price * p / 100m)}; {price} − {Num(price * p / 100m)} = ${Num(end)}", $"{p} % de {price} = {Num(price * p / 100m)}; {price} − {Num(price * p / 100m)} = ${Num(end)}"), decimals: true), 1900);
            }
        }
    }
}

/// <summary>Equações do 1º grau.</summary>
public sealed class LinearEquationsGenerator : ProblemGenerator
{
    public override string Key => "mat.equacoes";
    public override string SkillSlug => "equacoes-1-grau";
    public override string Level => "avancado";

    public override GeneratedProblem Generate(Random rng)
    {
        var x = Between(rng, -6, 12);
        if (x == 0) x = 7;
        int a = Between(rng, 2, 6), b = Between(rng, 1, 15), c = Between(rng, 2, 4);
        var balance = Tr("Faça a mesma operação dos dois lados para isolar o x.", "Do the same operation on both sides to get x by itself.", "Haz la misma operación en los dos lados para despejar la x.");

        return rng.Next(6) switch
        {
            0 => Make($"x + {b} = {x + b}", 1400, [balance, Tr($"Tire {b} dos dois lados.", $"Take {b} away from both sides.", $"Quita {b} de los dos lados.")]),
            1 => Make($"{a}x = {a * x}", 1450, [balance, Tr($"Divida os dois lados por {a}.", $"Divide both sides by {a}.", $"Divide los dos lados entre {a}.")]),
            2 => Make($"{a}x + {b} = {a * x + b}", 1650, [balance, Tr($"Primeiro tire {b} dos dois lados; depois divida por {a}.", $"First take {b} away from both sides; then divide by {a}.", $"Primero quita {b} de los dos lados; después divide entre {a}.")]),
            3 => Make($"{a + c}x − {b} = {c}x {Plus(a * x - b)}", 1900, [balance, Tr($"Passe {c}x para o lado esquerdo: {a}x − {b} = {a * x - b}.", $"Move {c}x to the left side: {a}x − {b} = {a * x - b}.", $"Pasa {c}x al lado izquierdo: {a}x − {b} = {a * x - b}.")]),
            4 => Make($"x/{c} + {b} = {x / c + b}", 1800, [balance, Tr($"Tire {b} dos dois lados; depois multiplique por {c}.", $"Take {b} away from both sides; then multiply by {c}.", $"Quita {b} de los dos lados; después multiplica por {c}.")], requireDivisible: c),
            _ => Story(rng),
        };

        GeneratedProblem Make(string equation, int difficulty, List<string> hints, int requireDivisible = 1)
        {
            if (x % requireDivisible != 0)
            {
                x = requireDivisible * Between(rng, 1, 5);
                equation = $"x/{c} + {b} = {x / c + b}";
            }
            var eq = equation.Replace("-", "−");
            return new(Numeric(Tr($"Resolva a equação: {eq}. Quanto vale x?", $"Solve the equation: {eq}. What is x?", $"Resuelve la ecuación: {eq}. ¿Cuánto vale x?"), x, hints, $"x = {Minus(x)}", negatives: true), difficulty + (x < 0 ? 80 : 0));
        }
    }

    private static string Plus(int v) => v < 0 ? $"− {-v}" : $"+ {v}";

    private static GeneratedProblem Story(Random rng)
    {
        var n = Between(rng, 3, 25);
        var k = Between(rng, 2, 20);
        return new(Numeric(Tr($"O dobro de um número, mais {k}, é igual a {2 * n + k}. Que número é esse?", $"Double a number, plus {k}, equals {2 * n + k}. What is the number?", $"El doble de un número, más {k}, es igual a {2 * n + k}. ¿Qué número es?"), n,
            [
                Tr($"Escreva a equação: 2x + {k} = {2 * n + k}.", $"Write the equation: 2x + {k} = {2 * n + k}.", $"Escribe la ecuación: 2x + {k} = {2 * n + k}."),
                Tr($"Tire {k} dos dois lados e divida por 2.", $"Take {k} away from both sides and divide by 2.", $"Quita {k} de los dos lados y divide entre 2."),
            ], Tr($"2x = {2 * n}, então x = {n}", $"2x = {2 * n}, so x = {n}", $"2x = {2 * n}, así que x = {n}"), negatives: true), 1750);
    }
}
