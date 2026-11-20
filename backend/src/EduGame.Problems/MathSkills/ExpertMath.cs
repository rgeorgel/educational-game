using EduGame.Problems.Tools;

namespace EduGame.Problems.MathSkills;

/// <summary>Sistemas de duas equações do 1º grau, com problemas de aplicação.</summary>
public sealed class SystemsGenerator : ProblemGenerator
{
    public override string Key => "mat.sistemas";
    public override string SkillSlug => "sistemas-equacoes";
    public override string Level => "expert";

    public override GeneratedProblem Generate(Random rng)
    {
        int x = Between(rng, 2, 15), y = Between(rng, 1, 12);
        const string addHint = "Some ou subtraia as duas equações para eliminar uma das letras.";

        switch (rng.Next(5))
        {
            case 0:
                return new(Numeric($"Resolva o sistema: x + y = {x + y} e x − y = {x - y}. Quanto vale x?", x,
                    [addHint, $"Somando: 2x = {2 * x}."], $"2x = {x + y} + {x - y} = {2 * x}, então x = {x} (e y = {y}).", negatives: true), 2050);
            case 1:
            {
                var a = Between(rng, 2, 4);
                return new(Numeric($"Resolva o sistema: {a}x + y = {a * x + y} e x + y = {x + y}. Quanto vale y?", y,
                    [addHint, $"Subtraindo a segunda da primeira: {a - 1}x = {(a - 1) * x}."], $"x = {x}; y = {x + y} − {x} = {y}", negatives: true), 2150);
            }
            case 2:
            {
                int cars = Between(rng, 3, 20), bikes = Between(rng, 3, 20);
                return new(Numeric($"Num estacionamento há carros e motos: {cars + bikes} veículos e {4 * cars + 2 * bikes} rodas ao todo. Quantas motos há?", bikes,
                    ["Chame de c os carros e de m as motos: c + m = " + (cars + bikes) + " e 4c + 2m = " + (4 * cars + 2 * bikes) + ".", "Se todos fossem motos, haveria " + 2 * (cars + bikes) + " rodas. Cada carro acrescenta 2 rodas."],
                    $"{cars} carros e {bikes} motos: {cars} × 4 + {bikes} × 2 = {4 * cars + 2 * bikes} rodas."), 2300);
            }
            case 3:
            {
                int kid = Between(rng, 5, 15), diff = Between(rng, 20, 35);
                var parent = kid + diff;
                return new(Numeric($"A soma das idades de uma mãe e de sua filha é {kid + parent} anos. A mãe tem {diff} anos a mais que a filha. Quantos anos tem a filha?", kid,
                    ["Monte duas equações: m + f = " + (kid + parent) + " e m − f = " + diff + ".", "Subtraia: 2f = " + (kid + parent - diff) + "."], $"f = ({kid + parent} − {diff}) ÷ 2 = {kid}"), 2350);
            }
            default:
            {
                int p1 = Between(rng, 2, 9), p2 = Between(rng, 10, 20), n1 = Between(rng, 2, 6), n2 = Between(rng, 2, 6);
                return new(Numeric($"Na cantina, {n1} sucos e {n2} sanduíches custam R$ {n1 * p1 + n2 * p2}. Um sanduíche custa R$ {p2 - p1} a mais que um suco. Quanto custa um suco, em reais?", p1,
                    [$"Escreva o sanduíche como (suco + {p2 - p1}).", $"{n1}s + {n2}(s + {p2 - p1}) = {n1 * p1 + n2 * p2}."], $"{n1 + n2}s = {n1 * p1 + n2 * p2} − {n2 * (p2 - p1)} = {(n1 + n2) * p1}, então s = R$ {p1}", decimals: true), 2450);
            }
        }
    }
}

/// <summary>Equações do 2º grau com raízes inteiras (fatoração, soma e produto).</summary>
public sealed class QuadraticGenerator : ProblemGenerator
{
    public override string Key => "mat.equacoes-2-grau";
    public override string SkillSlug => "equacoes-2-grau";
    public override string Level => "expert";

    public override GeneratedProblem Generate(Random rng)
    {
        int r1 = Between(rng, -9, 9), r2 = Between(rng, -9, 9);
        if (r1 == 0) r1 = 3;
        if (r2 == 0 || r2 == r1) r2 = r1 > 0 ? -r1 - 1 : -r1 + 1;
        int b = -(r1 + r2), c = r1 * r2;
        var eq = $"x² {Term(b)}x {Term(c)} = 0".Replace(" + 1x", " + x").Replace(" − 1x", " − x").Replace(" + 0x", "");
        const string factor = "Procure dois números com soma igual a −b e produto igual a c.";

        switch (rng.Next(5))
        {
            case 0:
            {
                var r = Between(rng, 3, 15);
                return new(Numeric($"Qual é a solução positiva de x² = {r * r}?", r, ["Que número ao quadrado dá esse valor?"], $"{r}² = {r * r} (a outra solução é −{r})."), 2050);
            }
            case 1:
                return new(Numeric($"Qual é a maior raiz de {eq}?", Math.Max(r1, r2), [factor, $"Os números são {MathTools.Minus(r1)} e {MathTools.Minus(r2)}."], $"{eq} → (x {Term(-r1)})(x {Term(-r2)}) = 0", negatives: true), 2250);
            case 2:
                return new(Numeric($"Qual é a menor raiz de {eq}?", Math.Min(r1, r2), [factor, $"Os números são {MathTools.Minus(r1)} e {MathTools.Minus(r2)}."], $"As raízes são {MathTools.Minus(r1)} e {MathTools.Minus(r2)}.", negatives: true), 2300);
            case 3:
                return new(Numeric($"Qual é a soma das raízes de {eq}?", r1 + r2, ["Numa equação x² + bx + c = 0, a soma das raízes é −b.", "Não precisa achar as raízes!"], $"Soma = −({MathTools.Minus(b)}) = {MathTools.Minus(r1 + r2)}", negatives: true), 2350);
            default:
                return new(Numeric($"Qual é o produto das raízes de {eq}?", c, ["Numa equação x² + bx + c = 0, o produto das raízes é c.", "Não precisa achar as raízes!"], $"Produto = {MathTools.Minus(c)}", negatives: true), 2400);
        }

        static string Term(int v) => v < 0 ? $"− {-v}" : $"+ {v}";
    }
}

/// <summary>Juros simples e compostos, aumentos e descontos sucessivos.</summary>
public sealed class InterestGenerator : ProblemGenerator
{
    public override string Key => "mat.juros";
    public override string SkillSlug => "juros-porcentagem";
    public override string Level => "expert";

    public override GeneratedProblem Generate(Random rng)
    {
        switch (rng.Next(4))
        {
            case 0:
            {
                int c = 100 * Between(rng, 2, 20), i = Pick(rng, [2, 3, 5, 10]), t = Between(rng, 2, 6);
                var j = c * i * t / 100m;
                return new(Numeric($"Um capital de R$ {c} é aplicado a juros simples de {i}% ao mês durante {t} meses. Quanto rende de juros, em reais?", j,
                    ["Juros simples: o mesmo valor a cada mês.", $"Por mês: {i}% de {c} = {Num(c * i / 100m)}."], $"{Num(c * i / 100m)} × {t} = R$ {Num(j)}", decimals: true), 2050);
            }
            case 1:
            {
                int c = 1000 * Between(rng, 1, 5), i = Pick(rng, [10, 20]);
                var m = c * (1 + i / 100m) * (1 + i / 100m);
                return new(Numeric($"R$ {c} são aplicados a juros compostos de {i}% ao ano. Qual é o valor depois de 2 anos, em reais?", m,
                    ["Juros compostos: os juros do 2º ano incidem sobre o valor já aumentado.", $"Depois de 1 ano: {Num(c * (1 + i / 100m))}."], $"{c} × {Num(1 + i / 100m)} × {Num(1 + i / 100m)} = R$ {Num(m)}", decimals: true), 2250);
            }
            case 2:
            {
                var p = Pick(rng, [10, 20, 25, 50]);
                var end = 100m * (1 + p / 100m) * (1 - p / 100m);
                return new(Numeric($"Um produto de R$ 100 teve aumento de {p}% e depois desconto de {p}%. Qual é o preço final, em reais?", end,
                    ["O desconto é calculado sobre o preço já aumentado, não sobre os R$ 100.", $"Depois do aumento: R$ {Num(100 + p)}."], $"100 × {Num(1 + p / 100m)} × {Num(1 - p / 100m)} = R$ {Num(end)} (menos que 100!)", decimals: true), 2350);
            }
            default:
            {
                var p = Pick(rng, [10, 20, 30]);
                var total = ((1 + p / 100m) * (1 + p / 100m) - 1) * 100;
                return new(Numeric($"Um preço sofreu dois aumentos seguidos de {p}%. Qual foi o aumento total, em porcentagem?", total,
                    ["Não é só somar as porcentagens.", $"Comece com 100: depois do 1º aumento fica {100 + p}."], $"100 → {100 + p} → {Num(100 * (1 + p / 100m) * (1 + p / 100m))}: aumento de {Num(total)}%", decimals: true), 2450);
            }
        }
    }
}

/// <summary>Teorema de Pitágoras e distância entre pontos.</summary>
public sealed class PythagorasGenerator : ProblemGenerator
{
    public override string Key => "mat.pitagoras";
    public override string SkillSlug => "geometria-pitagoras";
    public override string Level => "expert";

    private static readonly (int A, int B, int C)[] Triples = [(3, 4, 5), (5, 12, 13), (8, 15, 17), (7, 24, 25), (20, 21, 29)];

    public override GeneratedProblem Generate(Random rng)
    {
        var (a0, b0, c0) = Pick(rng, Triples);
        var k = a0 == 3 ? Between(rng, 1, 5) : 1;
        int a = a0 * k, b = b0 * k, c = c0 * k;
        const string rule = "Num triângulo retângulo: (cateto)² + (cateto)² = (hipotenusa)².";

        switch (rng.Next(5))
        {
            case 0:
                return new(Numeric($"Os catetos de um triângulo retângulo medem {a} cm e {b} cm. Quanto mede a hipotenusa, em cm?", c,
                    [rule, $"{a}² + {b}² = {a * a + b * b}."], $"√{a * a + b * b} = {c} cm"), 2050);
            case 1:
                return new(Numeric($"A hipotenusa de um triângulo retângulo mede {c} cm e um cateto mede {a} cm. Quanto mede o outro cateto, em cm?", b,
                    [rule, $"{c}² − {a}² = {c * c - a * a}."], $"√{c * c - a * a} = {b} cm"), 2150);
            case 2:
                return new(Numeric($"Uma escada de {c} m está apoiada numa parede, com o pé a {a} m da parede. A que altura ela toca a parede, em metros?", b,
                    ["A escada, a parede e o chão formam um triângulo retângulo; a escada é a hipotenusa.", rule], $"√({c}² − {a}²) = {b} m"), 2250);
            case 3:
                return new(Numeric($"Um triângulo retângulo tem hipotenusa de {c} cm e um cateto de {a} cm. Qual é a área dele, em cm²?", a * b / 2m,
                    ["Primeiro descubra o outro cateto com Pitágoras.", "Área do triângulo retângulo = cateto × cateto ÷ 2."], $"Outro cateto = {b}; área = {a} × {b} ÷ 2 = {Num(a * b / 2m)} cm²", decimals: true), 2400);
            default:
            {
                int x1 = Between(rng, -5, 5), y1 = Between(rng, -5, 5);
                return new(Numeric($"Qual é a distância entre os pontos ({MathTools.Minus(x1)}, {MathTools.Minus(y1)}) e ({MathTools.Minus(x1 + a)}, {MathTools.Minus(y1 + b)}) no plano cartesiano?", c,
                    ["Desenhe um triângulo retângulo: a diferença dos x e a diferença dos y são os catetos.", $"Catetos: {a} e {b}."], $"√({a}² + {b}²) = {c}"), 2350);
            }
        }
    }
}

/// <summary>Contagem: princípio multiplicativo, permutações, apertos de mão e combinações.</summary>
public sealed class CombinatoricsGenerator : ProblemGenerator
{
    public override string Key => "mat.combinatoria";
    public override string SkillSlug => "combinatoria";
    public override string Level => "expert";

    private static int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);

    public override GeneratedProblem Generate(Random rng)
    {
        switch (rng.Next(5))
        {
            case 0:
            {
                int shirts = Between(rng, 2, 6), pants = Between(rng, 2, 5), shoes = Between(rng, 1, 3);
                var total = shirts * pants * shoes;
                var shoeText = shoes == 1 ? "" : $" e {shoes} pares de tênis";
                return new(Numeric($"Lia tem {shirts} camisetas, {pants} calças{shoeText}. De quantas maneiras diferentes ela pode se vestir (uma peça de cada)?", total,
                    ["Para cada camiseta, ela pode escolher qualquer calça.", "Multiplique o número de opções de cada escolha."], $"{shirts} × {pants}{(shoes == 1 ? "" : $" × {shoes}")} = {total}"), shoes == 1 ? 2000 : 2050);
            }
            case 1:
            {
                var n = Between(rng, 3, 6);
                return new(Numeric($"De quantas maneiras {n} amigos podem se organizar numa fila?", Factorial(n),
                    [$"O primeiro lugar tem {n} opções; o segundo, {n - 1}…", "Multiplique: n × (n − 1) × … × 1."], $"{string.Join(" × ", Enumerable.Range(1, n).Reverse())} = {Factorial(n)}"), 2050 + n * 30);
            }
            case 2:
            {
                var n = Between(rng, 4, 12);
                return new(Numeric($"Numa reunião com {n} pessoas, cada uma cumprimenta todas as outras uma única vez. Quantos apertos de mão acontecem?", n * (n - 1) / 2,
                    [$"Cada pessoa aperta {n - 1} mãos.", "Mas cada aperto envolve duas pessoas: não conte duas vezes!"], $"{n} × {n - 1} ÷ 2 = {n * (n - 1) / 2}"), 2250);
            }
            case 3:
            {
                var len = Between(rng, 2, 4);
                var total = Enumerable.Range(0, len).Aggregate(1, (acc, i) => acc * (10 - i));
                return new(Numeric($"Quantas senhas de {len} algarismos podem ser formadas sem repetir algarismos (de 0 a 9)?", total,
                    ["O primeiro algarismo tem 10 opções.", "O próximo tem uma opção a menos, porque não pode repetir."], $"{string.Join(" × ", Enumerable.Range(0, len).Select(i => 10 - i))} = {total}"), 2300);
            }
            default:
            {
                int n = Between(rng, 5, 8), k = Between(rng, 2, 3);
                var total = Factorial(n) / (Factorial(k) * Factorial(n - k));
                return new(Numeric($"Uma turma de {n} alunos vai escolher uma comissão de {k} pessoas (sem cargos). Quantas comissões diferentes são possíveis?", total,
                    ["A ordem não importa: {Ana, Bia} é a mesma comissão que {Bia, Ana}.", $"Conte as filas de {k} pessoas e divida pelas {Factorial(k)} ordens possíveis de cada grupo."],
                    $"{string.Join(" × ", Enumerable.Range(n - k + 1, k).Reverse())} ÷ {Factorial(k)} = {total}"), 2450);
            }
        }
    }
}
