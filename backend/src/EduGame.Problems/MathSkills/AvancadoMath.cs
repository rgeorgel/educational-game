using EduGame.Problems.Tools;
using static EduGame.Problems.Tools.MathTools;

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
        const string rule = "Sinais iguais na multiplicação/divisão dão positivo; sinais diferentes dão negativo.";
        return rng.Next(6) switch
        {
            0 => Make($"Quanto é (−{a}) + {b}?", -a + b, ["Imagine a reta numérica: comece em −" + a + " e ande " + b + " para a direita."], 1450),
            1 => Make($"Quanto é −{a} − {b}?", -a - b, ["Já está abaixo de zero e vai descer mais.", $"Some as distâncias: {a} + {b}, e o resultado é negativo."], 1550),
            2 => Make($"Quanto é (−{a}) × (−{Math.Min(b, 9)})?", a * Math.Min(b, 9), [rule], 1650),
            3 => Make($"Quanto é {a * b} ÷ (−{b})?", -a, [rule], 1650),
            4 => Temperature(rng),
            _ => Make($"Quanto é −{a} + (−{Math.Min(b, 6)}) × 3?", -a - Math.Min(b, 6) * 3, ["Primeiro a multiplicação.", rule], 1900),
        };

        static GeneratedProblem Make(string prompt, int value, List<string> hints, int difficulty) =>
            new(Numeric(prompt, value, hints, $"O resultado é {Minus(value)}.", negatives: true), difficulty);
    }

    private static GeneratedProblem Temperature(Random rng)
    {
        int start = -Between(rng, 1, 10), change = Between(rng, 3, 15);
        var up = rng.Next(2) == 0;
        var end = up ? start + change : start - change;
        return new(Numeric($"De madrugada, a temperatura era {Minus(start)} °C. Depois ela {(up ? "subiu" : "caiu")} {change} °C. Qual é a nova temperatura, em °C?", end,
            ["Use a reta numérica (como um termômetro).", up ? "Subir é somar." : "Cair é subtrair."], $"{Minus(start)} {(up ? "+" : "−")} {change} = {Minus(end)} °C", negatives: true), 1500);
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
                return new(Numeric($"Quanto é {b}{Superscript(e)}?", v, [$"{b}{Superscript(e)} é {b} multiplicado por ele mesmo {e} vezes.", string.Join(" × ", Enumerable.Repeat(b, e))], $"{string.Join(" × ", Enumerable.Repeat(b, e))} = {v}"), 1450 + e * 40);
            }
            case 1:
            {
                var r = Between(rng, 4, 15);
                return new(Numeric($"Quanto é √{r * r}?", r, ["A raiz quadrada é o número que, multiplicado por ele mesmo, dá o valor de dentro.", $"? × ? = {r * r}"], $"{r} × {r} = {r * r}, então √{r * r} = {r}"), 1400 + r * 15);
            }
            case 2:
            {
                int a = Between(rng, 2, 5), b = Between(rng, 2, 4);
                var v = a * a * a + b * b;
                return new(Numeric($"Quanto é {a}³ + {b}²?", v, [$"{a}³ = {a} × {a} × {a}.", $"{b}² = {b} × {b}."], $"{a * a * a} + {b * b} = {v}"), 1700);
            }
            case 3:
            {
                int r = Between(rng, 2, 9), e = Between(rng, 2, 3);
                var v = r * (int)Math.Pow(2, e);
                return new(Numeric($"Quanto é √{r * r} × 2{Superscript(e)}?", v, [$"√{r * r} = {r}.", $"2{Superscript(e)} = {(int)Math.Pow(2, e)}."], $"{r} × {(int)Math.Pow(2, e)} = {v}"), 1950);
            }
            default:
            {
                var r = Between(rng, 11, 20);
                return new(Numeric($"Qual número positivo, elevado ao quadrado, dá {r * r}?", r, ["Procure um número que multiplicado por ele mesmo dê o valor.", $"Ele está entre 10 e 20. Teste o último algarismo: que número ao quadrado termina em {r * r % 10}?"], $"{r}² = {r * r}"), 1650 + (r - 11) * 25);
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
            0 => new(Numeric($"Qual é o MDC (máximo divisor comum) de {a} e {b}?", g,
                ["Liste os divisores de cada número.", $"Qual é o maior número que divide {a} e {b} sem sobrar resto?"], $"MDC({a}, {b}) = {g}"), 1500 + g * 20),
            1 => new(Numeric($"Qual é o MMC (mínimo múltiplo comum) de {a} e {b}?", Lcm(a, b),
                ["Liste os múltiplos do maior número até achar um que também seja múltiplo do outro.", $"MMC × MDC = {a} × {b}."], $"MMC({a}, {b}) = {Lcm(a, b)}"), 1600 + Math.Min(300, Lcm(a, b))),
            2 => Buses(rng),
            _ => new(Numeric($"Uma professora tem {a} lápis e {b} borrachas. Ela quer montar kits iguais, sem sobrar nada, com o maior número possível de kits. Quantos kits ela consegue montar?", g,
                ["O número de kits precisa dividir os lápis e as borrachas.", "O maior divisor comum é a resposta."], $"MDC({a}, {b}) = {g} kits"), 1800),
        };
    }

    private static GeneratedProblem Buses(Random rng)
    {
        var (a, b) = Pick(rng, [(12, 18), (10, 15), (8, 12), (15, 20), (6, 10), (9, 12), (20, 30)]);
        var l = Lcm(a, b);
        return new(Numeric($"Um ônibus passa no ponto a cada {a} minutos e outro a cada {b} minutos. Os dois passaram juntos agora. Daqui a quantos minutos vão passar juntos de novo?", l,
            ["Você precisa de um número que seja múltiplo de " + a + " e de " + b + ".", "É o menor múltiplo comum (MMC)."], $"MMC({a}, {b}) = {l} minutos"), 1950);
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
                return new(MultipleChoice(rng, $"Quanto é {f1} + {f2}?", r.ToString(),
                    [new Fraction(f1.Num + f2.Num, f1.Den + f2.Den).ToString(), (f1 * f2).ToString(), new Fraction(f1.Num + f2.Num, f1.Den * f2.Den).Simplify().ToString(), (f1 - f2).ToString()],
                    ["Não some os denominadores! Primeiro deixe as frações com o mesmo denominador.", $"Use o MMC de {f1.Den} e {f2.Den}: {MathTools.Lcm(f1.Den, f2.Den)}."],
                    $"{f1} + {f2} = {r} (já simplificada)"), 1500 + (f1.Den * f2.Den > 20 ? 150 : 0));
            }
            case 2:
            {
                var r = f1 * f2;
                return new(MultipleChoice(rng, $"Quanto é {f1} × {f2}?", r.ToString(),
                    [(f1 + f2).ToString(), new Fraction(f1.Num * f2.Den, f1.Den * f2.Num).Simplify().ToString(), new Fraction(f1.Num + f2.Num, f1.Den * f2.Den).Simplify().ToString()],
                    ["Na multiplicação, multiplique numerador com numerador e denominador com denominador.", "Simplifique no final."],
                    $"{f1} × {f2} = {r}"), 1650);
            }
            case 3:
            {
                var r = f1 / f2;
                return new(MultipleChoice(rng, $"Quanto é {f1} ÷ {f2}?", r.ToString(),
                    [(f1 * f2).ToString(), new Fraction(f2.Num * f1.Den, f2.Den * f1.Num).Simplify().ToString(), (f1 - f2).ToString()],
                    ["Dividir por uma fração é multiplicar pelo inverso dela.", $"O inverso de {f2} é {f2.Den}/{f2.Num}."],
                    $"{f1} × {f2.Den}/{f2.Num} = {r}"), 1950);
            }
            default:
            {
                var simple = Proper(rng).Simplify();
                var k = Between(rng, 2, 6);
                var big = new Fraction(simple.Num * k, simple.Den * k);
                return new(MultipleChoice(rng, $"Qual é a forma simplificada de {big}?", simple.ToString(),
                    [new Fraction(simple.Num * 2, simple.Den * 2).ToString(), new Fraction(simple.Num + 1, simple.Den).ToString(), new Fraction(simple.Num, simple.Den + 1).ToString()],
                    ["Divida o numerador e o denominador pelo mesmo número.", $"Os dois são divisíveis por {k}."],
                    $"{big.Num} ÷ {k} = {simple.Num} e {big.Den} ÷ {k} = {simple.Den}"), 1450 + k * 20);
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
                return new(Numeric($"{n1} cadernos custam R$ {n1 * price}. Quanto custam {n2} cadernos, em reais?", n2 * price,
                    ["Descubra quanto custa 1 caderno.", $"{n1 * price} ÷ {n1} = {price}."], $"1 caderno = R$ {price}; {n2} × {price} = R$ {n2 * price}") with { AcceptsDecimal = true }, 1500);
            }
            case 1:
            {
                var (w1, d1, w2) = Pick(rng, [(4, 6, 8), (3, 8, 6), (2, 12, 6), (5, 6, 10), (6, 10, 4), (4, 9, 6)]);
                var d2 = w1 * d1 / w2;
                return new(Numeric($"{w1} pintores pintam uma casa em {d1} dias. Trabalhando no mesmo ritmo, em quantos dias {w2} pintores pintam a mesma casa?", d2,
                    ["Mais pintores → menos dias (grandezas inversamente proporcionais).", $"O trabalho total é {w1} × {d1} = {w1 * d1} 'dias de pintor'."], $"{w1 * d1} ÷ {w2} = {d2} dias") with { AcceptsDecimal = true }, 1800);
            }
            case 2:
            {
                var p = Pick(rng, [15, 30, 40, 35, 60]);
                var total = 20 * Between(rng, 2, 20);
                return new(Numeric($"Quanto é {p}% de {total}?", total * p / 100m,
                    ["Calcule 10% (dividir por 10) e use para montar a porcentagem.", $"{p}% = {p} ÷ 100."], $"{total} × {p} ÷ 100 = {Num(total * p / 100m)}", decimals: true), 1600);
            }
            case 3:
            {
                var p = Pick(rng, [10, 20, 25, 50]);
                var price = 20 * Between(rng, 2, 15);
                var end = price + price * p / 100;
                return new(Numeric($"Um produto custava R$ {price} e teve um aumento de {p}%. Qual é o novo preço, em reais?", end,
                    [$"Calcule {p}% de {price}.", "Some o aumento ao preço antigo."], $"{p}% de {price} = {price * p / 100}; {price} + {price * p / 100} = R$ {end}") with { AcceptsDecimal = true }, 1750);
            }
            default:
            {
                var p = Pick(rng, [15, 30, 35]);
                var price = 20 * Between(rng, 2, 10);
                var end = price - price * p / 100m;
                return new(Numeric($"Uma bicicleta de R$ {price} está com {p}% de desconto. Quanto ela custa com o desconto, em reais?", end,
                    [$"Calcule {p}% de {price}.", "Tire o desconto do preço."], $"{p}% de {price} = {Num(price * p / 100m)}; {price} − {Num(price * p / 100m)} = R$ {Num(end)}", decimals: true), 1900);
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
        const string balance = "Faça a mesma operação dos dois lados para isolar o x.";

        return rng.Next(6) switch
        {
            0 => Make($"x + {b} = {x + b}", 1400, [balance, $"Tire {b} dos dois lados."]),
            1 => Make($"{a}x = {a * x}", 1450, [balance, $"Divida os dois lados por {a}."]),
            2 => Make($"{a}x + {b} = {a * x + b}", 1650, [balance, $"Primeiro tire {b} dos dois lados; depois divida por {a}."]),
            3 => Make($"{a + c}x − {b} = {c}x {Plus(a * x - b)}", 1900, [balance, $"Passe {c}x para o lado esquerdo: {a}x − {b} = {a * x - b}."]),
            4 => Make($"x/{c} + {b} = {x / c + b}", 1800, [balance, $"Tire {b} dos dois lados; depois multiplique por {c}."], requireDivisible: c),
            _ => Story(rng),
        };

        GeneratedProblem Make(string equation, int difficulty, List<string> hints, int requireDivisible = 1)
        {
            if (x % requireDivisible != 0)
            {
                x = requireDivisible * Between(rng, 1, 5);
                equation = $"x/{c} + {b} = {x / c + b}";
            }
            return new(Numeric($"Resolva a equação: {equation.Replace("-", "−")}. Quanto vale x?", x, hints, $"x = {Minus(x)}", negatives: true), difficulty + (x < 0 ? 80 : 0));
        }
    }

    private static string Plus(int v) => v < 0 ? $"− {-v}" : $"+ {v}";

    private static GeneratedProblem Story(Random rng)
    {
        var n = Between(rng, 3, 25);
        var k = Between(rng, 2, 20);
        return new(Numeric($"O dobro de um número, mais {k}, é igual a {2 * n + k}. Que número é esse?", n,
            [$"Escreva a equação: 2x + {k} = {2 * n + k}.", $"Tire {k} dos dois lados e divida por 2."], $"2x = {2 * n}, então x = {n}", negatives: true), 1750);
    }
}
