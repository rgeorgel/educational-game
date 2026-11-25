using EduGame.Problems.Tools;

using static EduGame.Problems.Lang;

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
        var addHint = Tr("Some ou subtraia as duas equações para eliminar uma das letras.", "Add or subtract the two equations to get rid of one of the letters.", "Suma o resta las dos ecuaciones para eliminar una de las letras.");

        switch (rng.Next(5))
        {
            case 0:
                return new(Numeric(Tr($"Resolva o sistema: x + y = {x + y} e x − y = {x - y}. Quanto vale x?", $"Solve the system: x + y = {x + y} and x − y = {x - y}. What is x?", $"Resuelve el sistema: x + y = {x + y} y x − y = {x - y}. ¿Cuánto vale x?"), x,
                    [addHint, Tr($"Somando: 2x = {2 * x}.", $"Adding: 2x = {2 * x}.", $"Sumando: 2x = {2 * x}.")],
                    Tr($"2x = {x + y} + {x - y} = {2 * x}, então x = {x} (e y = {y}).", $"2x = {x + y} + {x - y} = {2 * x}, so x = {x} (and y = {y}).", $"2x = {x + y} + {x - y} = {2 * x}, así que x = {x} (e y = {y})."), negatives: true), 2050);
            case 1:
            {
                var a = Between(rng, 2, 4);
                return new(Numeric(Tr($"Resolva o sistema: {a}x + y = {a * x + y} e x + y = {x + y}. Quanto vale y?", $"Solve the system: {a}x + y = {a * x + y} and x + y = {x + y}. What is y?", $"Resuelve el sistema: {a}x + y = {a * x + y} y x + y = {x + y}. ¿Cuánto vale y?"), y,
                    [addHint, Tr($"Subtraindo a segunda da primeira: {a - 1}x = {(a - 1) * x}.", $"Subtracting the second from the first: {a - 1}x = {(a - 1) * x}.", $"Restando la segunda de la primera: {a - 1}x = {(a - 1) * x}.")], $"x = {x}; y = {x + y} − {x} = {y}", negatives: true), 2150);
            }
            case 2:
            {
                int cars = Between(rng, 3, 20), bikes = Between(rng, 3, 20);
                int vehicles = cars + bikes, wheels = 4 * cars + 2 * bikes, allBikes = 2 * (cars + bikes);
                return new(Numeric(Tr($"Num estacionamento há carros e motos: {vehicles} veículos e {wheels} rodas ao todo. Quantas motos há?",
                                      $"A parking lot has cars and motorcycles: {vehicles} vehicles and {wheels} wheels in all. How many motorcycles are there?",
                                      $"En un estacionamiento hay coches y motos: {vehicles} vehículos y {wheels} ruedas en total. ¿Cuántas motos hay?"), bikes,
                    [
                        Tr($"Chame de c os carros e de m as motos: c + m = {vehicles} e 4c + 2m = {wheels}.", $"Call the cars c and the motorcycles m: c + m = {vehicles} and 4c + 2m = {wheels}.", $"Llama c a los coches y m a las motos: c + m = {vehicles} y 4c + 2m = {wheels}."),
                        Tr($"Se todos fossem motos, haveria {allBikes} rodas. Cada carro acrescenta 2 rodas.", $"If they were all motorcycles, there would be {allBikes} wheels. Each car adds 2 wheels.", $"Si todos fueran motos, habría {allBikes} ruedas. Cada coche añade 2 ruedas."),
                    ],
                    Tr($"{cars} carros e {bikes} motos: {cars} × 4 + {bikes} × 2 = {wheels} rodas.", $"{cars} cars and {bikes} motorcycles: {cars} × 4 + {bikes} × 2 = {wheels} wheels.", $"{cars} coches y {bikes} motos: {cars} × 4 + {bikes} × 2 = {wheels} ruedas.")), 2300);
            }
            case 3:
            {
                int kid = Between(rng, 5, 15), diff = Between(rng, 20, 35);
                var parent = kid + diff;
                int sum = kid + parent, twice = kid + parent - diff;
                return new(Numeric(Tr($"A soma das idades de uma mãe e de sua filha é {sum} anos. A mãe tem {diff} anos a mais que a filha. Quantos anos tem a filha?",
                                      $"A mother's and her daughter's ages add up to {sum} years. The mother is {diff} years older than the daughter. How old is the daughter?",
                                      $"La suma de las edades de una madre y su hija es {sum} años. La madre tiene {diff} años más que la hija. ¿Cuántos años tiene la hija?"), kid,
                    [
                        Tr($"Monte duas equações: m + f = {sum} e m − f = {diff}.", $"Write two equations: m + d = {sum} and m − d = {diff}.", $"Plantea dos ecuaciones: m + h = {sum} y m − h = {diff}."),
                        Tr($"Subtraia: 2f = {twice}.", $"Subtract: 2d = {twice}.", $"Resta: 2h = {twice}."),
                    ], Tr($"f = ({sum} − {diff}) ÷ 2 = {kid}", $"d = ({sum} − {diff}) ÷ 2 = {kid}", $"h = ({sum} − {diff}) ÷ 2 = {kid}")), 2350);
            }
            default:
            {
                int p1 = Between(rng, 2, 9), p2 = Between(rng, 10, 20), n1 = Between(rng, 2, 6), n2 = Between(rng, 2, 6);
                int cost = n1 * p1 + n2 * p2, more = p2 - p1;
                return new(Numeric(Tr($"Na cantina, {n1} sucos e {n2} sanduíches custam R$ {cost}. Um sanduíche custa R$ {more} a mais que um suco. Quanto custa um suco, em reais?",
                                      $"At the cafeteria, {n1} juices and {n2} sandwiches cost ${cost}. A sandwich costs ${more} more than a juice. How much does a juice cost, in dollars?",
                                      $"En la cafetería, {n1} jugos y {n2} sándwiches cuestan ${cost}. Un sándwich cuesta ${more} más que un jugo. ¿Cuánto cuesta un jugo?"), p1,
                    [
                        Tr($"Escreva o sanduíche como (suco + {more}).", $"Write the sandwich as (juice + {more}).", $"Escribe el sándwich como (jugo + {more})."),
                        Tr($"{n1}s + {n2}(s + {more}) = {cost}.", $"{n1}j + {n2}(j + {more}) = {cost}.", $"{n1}j + {n2}(j + {more}) = {cost}."),
                    ],
                    Tr($"{n1 + n2}s = {cost} − {n2 * more} = {(n1 + n2) * p1}, então s = R$ {p1}", $"{n1 + n2}j = {cost} − {n2 * more} = {(n1 + n2) * p1}, so j = ${p1}", $"{n1 + n2}j = {cost} − {n2 * more} = {(n1 + n2) * p1}, así que j = ${p1}"), decimals: true), 2450);
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
        var factor = Tr("Procure dois números com soma igual a −b e produto igual a c.", "Look for two numbers whose sum is −b and whose product is c.", "Busca dos números cuya suma sea −b y cuyo producto sea c.");
        var numbers = Tr($"Os números são {MathTools.Minus(r1)} e {MathTools.Minus(r2)}.", $"The numbers are {MathTools.Minus(r1)} and {MathTools.Minus(r2)}.", $"Los números son {MathTools.Minus(r1)} y {MathTools.Minus(r2)}.");
        var noNeed = Tr("Não precisa achar as raízes!", "You don't need to find the roots!", "¡No hace falta encontrar las raíces!");

        switch (rng.Next(5))
        {
            case 0:
            {
                var r = Between(rng, 3, 15);
                return new(Numeric(Tr($"Qual é a solução positiva de x² = {r * r}?", $"What is the positive solution of x² = {r * r}?", $"¿Cuál es la solución positiva de x² = {r * r}?"), r,
                    [Tr("Que número ao quadrado dá esse valor?", "Which number squared gives this value?", "¿Qué número al cuadrado da ese valor?")],
                    Tr($"{r}² = {r * r} (a outra solução é −{r}).", $"{r}² = {r * r} (the other solution is −{r}).", $"{r}² = {r * r} (la otra solución es −{r}).")), 2050);
            }
            case 1:
                return new(Numeric(Tr($"Qual é a maior raiz de {eq}?", $"What is the larger root of {eq}?", $"¿Cuál es la mayor raíz de {eq}?"), Math.Max(r1, r2), [factor, numbers], $"{eq} → (x {Term(-r1)})(x {Term(-r2)}) = 0", negatives: true), 2250);
            case 2:
                return new(Numeric(Tr($"Qual é a menor raiz de {eq}?", $"What is the smaller root of {eq}?", $"¿Cuál es la menor raíz de {eq}?"), Math.Min(r1, r2), [factor, numbers],
                    Tr($"As raízes são {MathTools.Minus(r1)} e {MathTools.Minus(r2)}.", $"The roots are {MathTools.Minus(r1)} and {MathTools.Minus(r2)}.", $"Las raíces son {MathTools.Minus(r1)} y {MathTools.Minus(r2)}."), negatives: true), 2300);
            case 3:
                return new(Numeric(Tr($"Qual é a soma das raízes de {eq}?", $"What is the sum of the roots of {eq}?", $"¿Cuál es la suma de las raíces de {eq}?"), r1 + r2,
                    [Tr("Numa equação x² + bx + c = 0, a soma das raízes é −b.", "In an equation x² + bx + c = 0, the sum of the roots is −b.", "En una ecuación x² + bx + c = 0, la suma de las raíces es −b."), noNeed],
                    Tr($"Soma = −({MathTools.Minus(b)}) = {MathTools.Minus(r1 + r2)}", $"Sum = −({MathTools.Minus(b)}) = {MathTools.Minus(r1 + r2)}", $"Suma = −({MathTools.Minus(b)}) = {MathTools.Minus(r1 + r2)}"), negatives: true), 2350);
            default:
                return new(Numeric(Tr($"Qual é o produto das raízes de {eq}?", $"What is the product of the roots of {eq}?", $"¿Cuál es el producto de las raíces de {eq}?"), c,
                    [Tr("Numa equação x² + bx + c = 0, o produto das raízes é c.", "In an equation x² + bx + c = 0, the product of the roots is c.", "En una ecuación x² + bx + c = 0, el producto de las raíces es c."), noNeed],
                    Tr($"Produto = {MathTools.Minus(c)}", $"Product = {MathTools.Minus(c)}", $"Producto = {MathTools.Minus(c)}"), negatives: true), 2400);
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

    private static string Money(decimal value) => Tr($"R$ {Num(value)}", $"${Num(value)}", $"${Num(value)}");

    public override GeneratedProblem Generate(Random rng)
    {
        switch (rng.Next(4))
        {
            case 0:
            {
                int c = 100 * Between(rng, 2, 20), i = Pick(rng, [2, 3, 5, 10]), t = Between(rng, 2, 6);
                var j = c * i * t / 100m;
                return new(Numeric(Tr($"Um capital de R$ {c} é aplicado a juros simples de {i}% ao mês durante {t} meses. Quanto rende de juros, em reais?",
                                      $"${c} is invested at {i}% simple interest per month for {t} months. How much interest does it earn, in dollars?",
                                      $"Un capital de ${c} se invierte a un interés simple del {i} % mensual durante {t} meses. ¿Cuánto genera de intereses?"), j,
                    [
                        Tr("Juros simples: o mesmo valor a cada mês.", "Simple interest: the same amount every month.", "Interés simple: el mismo valor cada mes."),
                        Tr($"Por mês: {i}% de {c} = {Num(c * i / 100m)}.", $"Per month: {i}% of {c} = {Num(c * i / 100m)}.", $"Por mes: {i} % de {c} = {Num(c * i / 100m)}."),
                    ], $"{Num(c * i / 100m)} × {t} = {Money(j)}", decimals: true), 2050);
            }
            case 1:
            {
                int c = 1000 * Between(rng, 1, 5), i = Pick(rng, [10, 20]);
                var m = c * (1 + i / 100m) * (1 + i / 100m);
                return new(Numeric(Tr($"R$ {c} são aplicados a juros compostos de {i}% ao ano. Qual é o valor depois de 2 anos, em reais?",
                                      $"${c} is invested at {i}% compound interest per year. What is it worth after 2 years, in dollars?",
                                      $"Se invierten ${c} a un interés compuesto del {i} % anual. ¿Cuál es el valor después de 2 años?"), m,
                    [
                        Tr("Juros compostos: os juros do 2º ano incidem sobre o valor já aumentado.", "Compound interest: the 2nd year's interest is calculated on the already increased amount.", "Interés compuesto: los intereses del 2.º año se calculan sobre el valor ya aumentado."),
                        Tr($"Depois de 1 ano: {Num(c * (1 + i / 100m))}.", $"After 1 year: {Num(c * (1 + i / 100m))}.", $"Después de 1 año: {Num(c * (1 + i / 100m))}."),
                    ], $"{c} × {Num(1 + i / 100m)} × {Num(1 + i / 100m)} = {Money(m)}", decimals: true), 2250);
            }
            case 2:
            {
                var p = Pick(rng, [10, 20, 25, 50]);
                var end = 100m * (1 + p / 100m) * (1 - p / 100m);
                return new(Numeric(Tr($"Um produto de R$ 100 teve aumento de {p}% e depois desconto de {p}%. Qual é o preço final, em reais?",
                                      $"A $100 product went up {p}% and then got a {p}% discount. What is the final price, in dollars?",
                                      $"Un producto de $100 subió un {p} % y después tuvo un descuento del {p} %. ¿Cuál es el precio final?"), end,
                    [
                        Tr("O desconto é calculado sobre o preço já aumentado, não sobre os R$ 100.", "The discount is calculated on the already increased price, not on the $100.", "El descuento se calcula sobre el precio ya aumentado, no sobre los $100."),
                        Tr($"Depois do aumento: R$ {Num(100 + p)}.", $"After the increase: ${Num(100 + p)}.", $"Después del aumento: ${Num(100 + p)}."),
                    ],
                    Tr($"100 × {Num(1 + p / 100m)} × {Num(1 - p / 100m)} = R$ {Num(end)} (menos que 100!)", $"100 × {Num(1 + p / 100m)} × {Num(1 - p / 100m)} = ${Num(end)} (less than 100!)", $"100 × {Num(1 + p / 100m)} × {Num(1 - p / 100m)} = ${Num(end)} (¡menos que 100!)"), decimals: true), 2350);
            }
            default:
            {
                var p = Pick(rng, [10, 20, 30]);
                var total = ((1 + p / 100m) * (1 + p / 100m) - 1) * 100;
                return new(Numeric(Tr($"Um preço sofreu dois aumentos seguidos de {p}%. Qual foi o aumento total, em porcentagem?",
                                      $"A price went up {p}% twice in a row. What was the total increase, as a percentage?",
                                      $"Un precio subió dos veces seguidas un {p} %. ¿Cuál fue el aumento total, en porcentaje?"), total,
                    [
                        Tr("Não é só somar as porcentagens.", "It's not just adding the percentages.", "No basta con sumar los porcentajes."),
                        Tr($"Comece com 100: depois do 1º aumento fica {100 + p}.", $"Start with 100: after the 1st increase it's {100 + p}.", $"Empieza con 100: después del 1.er aumento queda en {100 + p}."),
                    ],
                    Tr($"100 → {100 + p} → {Num(100 * (1 + p / 100m) * (1 + p / 100m))}: aumento de {Num(total)}%", $"100 → {100 + p} → {Num(100 * (1 + p / 100m) * (1 + p / 100m))}: an increase of {Num(total)}%", $"100 → {100 + p} → {Num(100 * (1 + p / 100m) * (1 + p / 100m))}: aumento del {Num(total)} %"), decimals: true), 2450);
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
        var rule = Tr("Num triângulo retângulo: (cateto)² + (cateto)² = (hipotenusa)².", "In a right triangle: (leg)² + (leg)² = (hypotenuse)².", "En un triángulo rectángulo: (cateto)² + (cateto)² = (hipotenusa)².");

        switch (rng.Next(5))
        {
            case 0:
                return new(Numeric(Tr($"Os catetos de um triângulo retângulo medem {a} cm e {b} cm. Quanto mede a hipotenusa, em cm?", $"The legs of a right triangle are {a} cm and {b} cm. How long is the hypotenuse, in cm?", $"Los catetos de un triángulo rectángulo miden {a} cm y {b} cm. ¿Cuánto mide la hipotenusa, en cm?"), c,
                    [rule, $"{a}² + {b}² = {a * a + b * b}."], $"√{a * a + b * b} = {c} cm"), 2050);
            case 1:
                return new(Numeric(Tr($"A hipotenusa de um triângulo retângulo mede {c} cm e um cateto mede {a} cm. Quanto mede o outro cateto, em cm?", $"The hypotenuse of a right triangle is {c} cm and one leg is {a} cm. How long is the other leg, in cm?", $"La hipotenusa de un triángulo rectángulo mide {c} cm y un cateto mide {a} cm. ¿Cuánto mide el otro cateto, en cm?"), b,
                    [rule, $"{c}² − {a}² = {c * c - a * a}."], $"√{c * c - a * a} = {b} cm"), 2150);
            case 2:
                return new(Numeric(Tr($"Uma escada de {c} m está apoiada numa parede, com o pé a {a} m da parede. A que altura ela toca a parede, em metros?", $"A {c} m ladder leans against a wall, with its foot {a} m from the wall. How high up does it touch the wall, in meters?", $"Una escalera de {c} m está apoyada en una pared, con el pie a {a} m de la pared. ¿A qué altura toca la pared, en metros?"), b,
                    [Tr("A escada, a parede e o chão formam um triângulo retângulo; a escada é a hipotenusa.", "The ladder, the wall and the ground form a right triangle; the ladder is the hypotenuse.", "La escalera, la pared y el suelo forman un triángulo rectángulo; la escalera es la hipotenusa."), rule], $"√({c}² − {a}²) = {b} m"), 2250);
            case 3:
                return new(Numeric(Tr($"Um triângulo retângulo tem hipotenusa de {c} cm e um cateto de {a} cm. Qual é a área dele, em cm²?", $"A right triangle has a {c} cm hypotenuse and a {a} cm leg. What is its area, in cm²?", $"Un triángulo rectángulo tiene una hipotenusa de {c} cm y un cateto de {a} cm. ¿Cuál es su área, en cm²?"), a * b / 2m,
                    [
                        Tr("Primeiro descubra o outro cateto com Pitágoras.", "First find the other leg with Pythagoras.", "Primero averigua el otro cateto con Pitágoras."),
                        Tr("Área do triângulo retângulo = cateto × cateto ÷ 2.", "Area of a right triangle = leg × leg ÷ 2.", "Área del triángulo rectángulo = cateto × cateto ÷ 2."),
                    ], Tr($"Outro cateto = {b}; área = {a} × {b} ÷ 2 = {Num(a * b / 2m)} cm²", $"Other leg = {b}; area = {a} × {b} ÷ 2 = {Num(a * b / 2m)} cm²", $"Otro cateto = {b}; área = {a} × {b} ÷ 2 = {Num(a * b / 2m)} cm²"), decimals: true), 2400);
            default:
            {
                int x1 = Between(rng, -5, 5), y1 = Between(rng, -5, 5);
                string p1 = $"({MathTools.Minus(x1)}, {MathTools.Minus(y1)})", p2 = $"({MathTools.Minus(x1 + a)}, {MathTools.Minus(y1 + b)})";
                return new(Numeric(Tr($"Qual é a distância entre os pontos {p1} e {p2} no plano cartesiano?", $"What is the distance between the points {p1} and {p2} on the coordinate plane?", $"¿Cuál es la distancia entre los puntos {p1} y {p2} en el plano cartesiano?"), c,
                    [
                        Tr("Desenhe um triângulo retângulo: a diferença dos x e a diferença dos y são os catetos.", "Draw a right triangle: the difference in x and the difference in y are the legs.", "Dibuja un triángulo rectángulo: la diferencia de las x y la diferencia de las y son los catetos."),
                        Tr($"Catetos: {a} e {b}.", $"Legs: {a} and {b}.", $"Catetos: {a} y {b}."),
                    ], $"√({a}² + {b}²) = {c}"), 2350);
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
                var clothes = shoes == 1
                    ? Tr($"{shirts} camisetas e {pants} calças", $"{shirts} T-shirts and {pants} pairs of pants", $"{shirts} camisetas y {pants} pantalones")
                    : Tr($"{shirts} camisetas, {pants} calças e {shoes} pares de tênis", $"{shirts} T-shirts, {pants} pairs of pants and {shoes} pairs of sneakers", $"{shirts} camisetas, {pants} pantalones y {shoes} pares de zapatillas");
                return new(Numeric(Tr($"Lia tem {clothes}. De quantas maneiras diferentes ela pode se vestir (uma peça de cada)?", $"Lia has {clothes}. In how many different ways can she get dressed (one of each)?", $"Lía tiene {clothes}. ¿De cuántas maneras distintas puede vestirse (una prenda de cada)?"), total,
                    [
                        Tr("Para cada camiseta, ela pode escolher qualquer calça.", "For each T-shirt, she can choose any pair of pants.", "Para cada camiseta, puede elegir cualquier pantalón."),
                        Tr("Multiplique o número de opções de cada escolha.", "Multiply the number of options for each choice.", "Multiplica el número de opciones de cada elección."),
                    ], $"{shirts} × {pants}{(shoes == 1 ? "" : $" × {shoes}")} = {total}"), shoes == 1 ? 2000 : 2050);
            }
            case 1:
            {
                var n = Between(rng, 3, 6);
                return new(Numeric(Tr($"De quantas maneiras {n} amigos podem se organizar numa fila?", $"In how many ways can {n} friends line up?", $"¿De cuántas maneras pueden ponerse en fila {n} amigos?"), Factorial(n),
                    [
                        Tr($"O primeiro lugar tem {n} opções; o segundo, {n - 1}…", $"The first spot has {n} options; the second, {n - 1}…", $"El primer lugar tiene {n} opciones; el segundo, {n - 1}…"),
                        Tr("Multiplique: n × (n − 1) × … × 1.", "Multiply: n × (n − 1) × … × 1.", "Multiplica: n × (n − 1) × … × 1."),
                    ], $"{string.Join(" × ", Enumerable.Range(1, n).Reverse())} = {Factorial(n)}"), 2050 + n * 30);
            }
            case 2:
            {
                var n = Between(rng, 4, 12);
                return new(Numeric(Tr($"Numa reunião com {n} pessoas, cada uma cumprimenta todas as outras uma única vez. Quantos apertos de mão acontecem?", $"At a meeting with {n} people, each person shakes hands with everyone else exactly once. How many handshakes are there?", $"En una reunión con {n} personas, cada una saluda a todas las demás una sola vez. ¿Cuántos apretones de manos hay?"), n * (n - 1) / 2,
                    [
                        Tr($"Cada pessoa aperta {n - 1} mãos.", $"Each person shakes {n - 1} hands.", $"Cada persona estrecha {n - 1} manos."),
                        Tr("Mas cada aperto envolve duas pessoas: não conte duas vezes!", "But each handshake involves two people: don't count it twice!", "Pero cada apretón es entre dos personas: ¡no lo cuentes dos veces!"),
                    ], $"{n} × {n - 1} ÷ 2 = {n * (n - 1) / 2}"), 2250);
            }
            case 3:
            {
                var len = Between(rng, 2, 4);
                var total = Enumerable.Range(0, len).Aggregate(1, (acc, i) => acc * (10 - i));
                return new(Numeric(Tr($"Quantas senhas de {len} algarismos podem ser formadas sem repetir algarismos (de 0 a 9)?", $"How many {len}-digit codes can be made without repeating digits (0 to 9)?", $"¿Cuántas contraseñas de {len} cifras se pueden formar sin repetir cifras (del 0 al 9)?"), total,
                    [
                        Tr("O primeiro algarismo tem 10 opções.", "The first digit has 10 options.", "La primera cifra tiene 10 opciones."),
                        Tr("O próximo tem uma opção a menos, porque não pode repetir.", "The next one has one option fewer, because it can't repeat.", "La siguiente tiene una opción menos, porque no se puede repetir."),
                    ], $"{string.Join(" × ", Enumerable.Range(0, len).Select(i => 10 - i))} = {total}"), 2300);
            }
            default:
            {
                int n = Between(rng, 5, 8), k = Between(rng, 2, 3);
                var total = Factorial(n) / (Factorial(k) * Factorial(n - k));
                return new(Numeric(Tr($"Uma turma de {n} alunos vai escolher uma comissão de {k} pessoas (sem cargos). Quantas comissões diferentes são possíveis?", $"A class of {n} students will choose a committee of {k} people (no roles). How many different committees are possible?", $"Una clase de {n} alumnos va a elegir una comisión de {k} personas (sin cargos). ¿Cuántas comisiones distintas son posibles?"), total,
                    [
                        Tr("A ordem não importa: {Ana, Bia} é a mesma comissão que {Bia, Ana}.", "Order doesn't matter: {Ana, Bea} is the same committee as {Bea, Ana}.", "El orden no importa: {Ana, Bea} es la misma comisión que {Bea, Ana}."),
                        Tr($"Conte as filas de {k} pessoas e divida pelas {Factorial(k)} ordens possíveis de cada grupo.", $"Count the lines of {k} people and divide by the {Factorial(k)} possible orders of each group.", $"Cuenta las filas de {k} personas y divide entre los {Factorial(k)} órdenes posibles de cada grupo."),
                    ],
                    $"{string.Join(" × ", Enumerable.Range(n - k + 1, k).Reverse())} ÷ {Factorial(k)} = {total}"), 2450);
            }
        }
    }
}
