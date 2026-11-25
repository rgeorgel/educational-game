using EduGame.Problems.Tools;

using static EduGame.Problems.Lang;

namespace EduGame.Problems.LogicSkills;

/// <summary>Sequências com regras compostas: multiplicação, quadrados, Fibonacci, diferenças crescentes.</summary>
public sealed class CompositeSequenceGenerator : ProblemGenerator
{
    public override string Key => "log.sequencias-compostas";
    public override string SkillSlug => "sequencias-compostas";
    public override string Level => "medio";

    private sealed record Rule(Text Description, int Difficulty, Func<Random, int[]> Build);

    private static readonly Rule[] Rules =
    [
        new(new("multiplicar por 3 a cada passo", "multiply by 3 each step", "multiplicar por 3 en cada paso"), 1000, r => Iterate(Between(r, 1, 3), 6, x => x * 3)),
        new(new("multiplicar por 2 e somar 1", "multiply by 2 and add 1", "multiplicar por 2 y sumar 1"), 1250, r => Iterate(Between(r, 1, 4), 6, x => x * 2 + 1)),
        new(new("os números são quadrados: 1×1, 2×2, 3×3…", "the numbers are squares: 1×1, 2×2, 3×3…", "los números son cuadrados: 1×1, 2×2, 3×3…"), 1200, r => { var s = Between(r, 1, 4); return [.. Enumerable.Range(s, 6).Select(i => i * i)]; }),
        new(new("cada número é a soma dos dois anteriores", "each number is the sum of the two before it", "cada número es la suma de los dos anteriores"), 1350, r => Fibonacci(Between(r, 1, 3), Between(r, 1, 4))),
        new(new("duas sequências intercaladas: uma soma 2, a outra soma 5", "two interleaved sequences: one adds 2, the other adds 5", "dos secuencias intercaladas: una suma 2, la otra suma 5"), 1250, r => Interleave(Between(r, 1, 9), 2, Between(r, 10, 20), 5)),
        new(new("as diferenças dobram: +1, +2, +4, +8…", "the differences double: +1, +2, +4, +8…", "las diferencias se duplican: +1, +2, +4, +8…"), 1400, r => Differences(Between(r, 1, 5), i => 1 << i)),
        new(new("as diferenças crescem: +2, +3, +4, +5…", "the differences grow: +2, +3, +4, +5…", "las diferencias crecen: +2, +3, +4, +5…"), 1300, r => Differences(Between(r, 1, 10), i => i + 2)),
        new(new("multiplicar por 2 e depois tirar 1, alternando", "multiply by 2 and then take away 1, alternating", "multiplicar por 2 y después quitar 1, alternando"), 1550, r => Alternate(Between(r, 3, 6), x => x * 2, x => x - 1)),
        new(new("dividir por 2 a cada passo", "divide by 2 each step", "dividir entre 2 en cada paso"), 1150, r => { var s = Between(r, 1, 3) << 6; return Iterate(s, 6, x => x / 2); }),
    ];

    private static int[] Iterate(int start, int count, Func<int, int> next)
    {
        var list = new List<int> { start };
        while (list.Count < count) list.Add(next(list[^1]));
        return [.. list];
    }

    private static int[] Fibonacci(int a, int b)
    {
        var list = new List<int> { a, b };
        while (list.Count < 7) list.Add(list[^1] + list[^2]);
        return [.. list];
    }

    private static int[] Interleave(int a, int stepA, int b, int stepB) =>
        [.. Enumerable.Range(0, 7).Select(i => i % 2 == 0 ? a + i / 2 * stepA : b + i / 2 * stepB)];

    private static int[] Differences(int start, Func<int, int> step)
    {
        var list = new List<int> { start };
        for (var i = 0; list.Count < 6; i++) list.Add(list[^1] + step(i));
        return [.. list];
    }

    private static int[] Alternate(int start, Func<int, int> f, Func<int, int> g)
    {
        var list = new List<int> { start };
        for (var i = 0; list.Count < 7; i++) list.Add(i % 2 == 0 ? f(list[^1]) : g(list[^1]));
        return [.. list];
    }

    public override GeneratedProblem Generate(Random rng)
    {
        var rule = Pick(rng, Rules);
        var numbers = rule.Build(rng);
        var shown = Math.Min(numbers.Length, 6);
        var blank = shown - 1;
        var answer = numbers[blank];
        var content = new ProblemContent(
            ProblemTypes.CompletarSequencia,
            Tr("Qual número completa a sequência?", "Which number completes the sequence?", "¿Qué número completa la secuencia?"),
            answer.ToString(),
            [
                Tr("Calcule a diferença entre números vizinhos. Se não for sempre igual, olhe como ela muda.", "Work out the difference between neighboring numbers. If it isn't always the same, look at how it changes.", "Calcula la diferencia entre números vecinos. Si no es siempre igual, mira cómo cambia."),
                Tr("Tente também multiplicar ou dividir um número para chegar no próximo.", "Also try multiplying or dividing a number to get to the next one.", "Prueba también a multiplicar o dividir un número para llegar al siguiente."),
                Tr($"A regra é: {rule.Description}.", $"The rule is: {rule.Description}.", $"La regla es: {rule.Description}."),
            ],
            Tr($"A regra é {rule.Description}, então o número que falta é {answer}.", $"The rule is {rule.Description}, so the missing number is {answer}.", $"La regla es {rule.Description}, así que el número que falta es {answer}."))
        { Sequence = [.. numbers.Take(shown).Select((n, i) => i == blank ? null : n.ToString())] };
        return new(content, rule.Difficulty);
    }
}

/// <summary>Grades numéricas: Sudoku 4×4 e quadrado mágico 3×3.</summary>
public sealed class NumberGridGenerator : ProblemGenerator
{
    public override string Key => "log.grades";
    public override string SkillSlug => "grades-numericas";
    public override string Level => "medio";

    // Quadrado mágico de Lo Shu e suas 8 simetrias geram todos os 3×3 com 1..9.
    private static readonly int[] LoShu = [2, 7, 6, 9, 5, 1, 4, 3, 8];

    public override GeneratedProblem Generate(Random rng) => rng.Next(2) == 0 ? Sudoku(rng) : Magic(rng);

    private static GeneratedProblem Sudoku(Random rng)
    {
        var blanks = Between(rng, 4, 11);
        var solution = SudokuTools.Solution(rng, 2, 2);
        var cells = SudokuTools.MakePuzzle(rng, solution, 2, 2, blanks);
        var actualBlanks = cells.Count(c => c is null);
        var content = new ProblemContent(
            ProblemTypes.Grade,
            Tr("Complete o Sudoku: cada linha, cada coluna e cada quadrado 2×2 deve ter os números 1, 2, 3 e 4.", "Complete the Sudoku: every row, every column and every 2×2 box must have the numbers 1, 2, 3 and 4.", "Completa el Sudoku: cada fila, cada columna y cada cuadrado de 2×2 debe tener los números 1, 2, 3 y 4."),
            string.Join(',', solution),
            [
                Tr("Procure uma linha, coluna ou quadrado em que só falta um número.", "Look for a row, column or box where only one number is missing.", "Busca una fila, columna o cuadrado en el que solo falte un número."),
                Tr("Se um número já aparece na linha, ele não pode se repetir nela.", "If a number already appears in a row, it can't appear there again.", "Si un número ya aparece en la fila, no se puede repetir en ella."),
            ],
            Tr("Cada linha, coluna e quadrado tem 1, 2, 3 e 4 sem repetir.", "Every row, column and box has 1, 2, 3 and 4 with no repeats.", "Cada fila, columna y cuadrado tiene 1, 2, 3 y 4 sin repetir."))
        { Grid = new GridPuzzle(GridKinds.Sudoku, 4, 2, 2, 4, null, cells) };
        return new(content, 850 + actualBlanks * 45);
    }

    private static GeneratedProblem Magic(Random rng)
    {
        var square = Symmetry(LoShu, rng.Next(8));
        var givens = Between(rng, 2, 4);
        // Sempre mostra pelo menos uma célula fora do centro, para a solução ser única.
        var shown = Enumerable.Range(0, 9).Where(i => i != 4).OrderBy(_ => rng.Next()).Take(givens).ToHashSet();
        var cells = square.Select((v, i) => shown.Contains(i) ? (int?)v : null).ToList();
        var content = new ProblemContent(
            ProblemTypes.Grade,
            Tr("Quadrado mágico: use os números de 1 a 9, sem repetir, para que cada linha, coluna e diagonal some 15.", "Magic square: use the numbers 1 to 9, without repeating, so that every row, column and diagonal adds up to 15.", "Cuadrado mágico: usa los números del 1 al 9, sin repetir, para que cada fila, columna y diagonal sume 15."),
            string.Join(',', square),
            [
                Tr("O número do meio de um quadrado mágico 3×3 com 1 a 9 é sempre 5.", "The middle number of a 3×3 magic square with 1 to 9 is always 5.", "El número del centro de un cuadrado mágico 3×3 con 1 a 9 siempre es 5."),
                Tr("Os números pares ficam nos cantos.", "The even numbers go in the corners.", "Los números pares van en las esquinas."),
                Tr("Se uma linha tem dois números, o terceiro é 15 menos a soma deles.", "If a row has two numbers, the third is 15 minus their sum.", "Si una fila tiene dos números, el tercero es 15 menos su suma."),
            ],
            Tr("Todas as linhas, colunas e diagonais somam 15.", "All rows, columns and diagonals add up to 15.", "Todas las filas, columnas y diagonales suman 15."))
        { Grid = new GridPuzzle(GridKinds.MagicSquare, 3, 3, 3, 9, 15, cells) };
        return new(content, givens switch { 4 => 1200, 3 => 1350, _ => 1500 });
    }

    private static int[] Symmetry(int[] g, int k)
    {
        int At(int r, int c) => g[r * 3 + c];
        var result = new int[9];
        for (var r = 0; r < 3; r++)
        for (var c = 0; c < 3; c++)
        {
            var (rr, cc) = k switch
            {
                0 => (r, c), 1 => (c, 2 - r), 2 => (2 - r, 2 - c), 3 => (2 - c, r),
                4 => (r, 2 - c), 5 => (2 - r, c), 6 => (c, r), _ => (2 - c, 2 - r),
            };
            result[r * 3 + c] = At(rr, cc);
        }
        return result;
    }
}

/// <summary>Verdadeiro ou falso: afirmações sobre números, formas e raciocínio do dia a dia.</summary>
public sealed class TrueFalseGenerator : ProblemGenerator
{
    public override string Key => "log.verdadeiro-falso";
    public override string SkillSlug => "verdadeiro-ou-falso";
    public override string Level => "medio";

    private static readonly (Text Statement, bool Value, Text Why, int Difficulty)[] Curated =
    [
        (new("Todo quadrado é um retângulo.", "Every square is a rectangle.", "Todo cuadrado es un rectángulo."), true, new("Um retângulo tem 4 ângulos retos; o quadrado também tem, e ainda os lados iguais.", "A rectangle has 4 right angles; a square does too, and its sides are also equal.", "Un rectángulo tiene 4 ángulos rectos; el cuadrado también, y además tiene los lados iguales."), 1250),
        (new("Todo retângulo é um quadrado.", "Every rectangle is a square.", "Todo rectángulo es un cuadrado."), false, new("Um retângulo pode ter lados de tamanhos diferentes, como 2 × 5.", "A rectangle can have sides of different lengths, like 2 × 5.", "Un rectángulo puede tener lados de distinto tamaño, como 2 × 5."), 1050),
        (new("A soma de dois números ímpares é sempre ímpar.", "The sum of two odd numbers is always odd.", "La suma de dos números impares siempre es impar."), false, new("3 + 5 = 8, que é par. Ímpar + ímpar dá sempre par.", "3 + 5 = 8, which is even. Odd + odd always makes even.", "3 + 5 = 8, que es par. Impar + impar siempre da par."), 1250),
        (new("A soma de dois números pares é sempre par.", "The sum of two even numbers is always even.", "La suma de dos números pares siempre es par."), true, new("Par + par dá sempre par, como 4 + 6 = 10.", "Even + even always makes even, like 4 + 6 = 10.", "Par + par siempre da par, como 4 + 6 = 10."), 1050),
        (new("Se hoje é segunda-feira, depois de amanhã será quarta-feira.", "If today is Monday, the day after tomorrow will be Wednesday.", "Si hoy es lunes, pasado mañana será miércoles."), true, new("Segunda → terça (amanhã) → quarta (depois de amanhã).", "Monday → Tuesday (tomorrow) → Wednesday (the day after tomorrow).", "Lunes → martes (mañana) → miércoles (pasado mañana)."), 950),
        (new("Se hoje é sexta-feira, ontem foi sábado.", "If today is Friday, yesterday was Saturday.", "Si hoy es viernes, ayer fue sábado."), false, new("Ontem foi quinta-feira; sábado é amanhã.", "Yesterday was Thursday; Saturday is tomorrow.", "Ayer fue jueves; el sábado es mañana."), 950),
        (new("Todo número terminado em 0 é divisível por 5.", "Every number ending in 0 is divisible by 5.", "Todo número que termina en 0 es divisible por 5."), true, new("Números terminados em 0 ou 5 são divisíveis por 5.", "Numbers ending in 0 or 5 are divisible by 5.", "Los números que terminan en 0 o 5 son divisibles por 5."), 1150),
        (new("Todo número divisível por 5 termina em 0.", "Every number divisible by 5 ends in 0.", "Todo número divisible por 5 termina en 0."), false, new("15 é divisível por 5 e termina em 5.", "15 is divisible by 5 and ends in 5.", "15 es divisible por 5 y termina en 5."), 1300),
        (new("Um triângulo pode ter dois ângulos retos.", "A triangle can have two right angles.", "Un triángulo puede tener dos ángulos rectos."), false, new("Os ângulos de um triângulo somam 180°; dois retos já dariam 180°, sem sobrar para o terceiro.", "The angles of a triangle add up to 180°; two right angles would already make 180°, leaving nothing for the third.", "Los ángulos de un triángulo suman 180°; dos rectos ya darían 180°, sin dejar nada para el tercero."), 1550),
        (new("Metade de uma metade é um quarto.", "Half of a half is a quarter.", "La mitad de una mitad es un cuarto."), true, new("1/2 de 1/2 = 1/4.", "1/2 of 1/2 = 1/4.", "1/2 de 1/2 = 1/4."), 1200),
        (new("Existe um número que é par e ímpar ao mesmo tempo.", "There is a number that is both even and odd.", "Existe un número que es par e impar a la vez."), false, new("Todo número inteiro é par ou ímpar, nunca os dois.", "Every whole number is either even or odd, never both.", "Todo número entero es par o impar, nunca las dos cosas."), 1100),
        (new("Se todos os gatos têm bigodes e Mimi é um gato, então Mimi tem bigodes.", "If all cats have whiskers and Mimi is a cat, then Mimi has whiskers.", "Si todos los gatos tienen bigotes y Mimi es un gato, entonces Mimi tiene bigotes."), true, new("Se a regra vale para todos os gatos, vale para Mimi.", "If the rule holds for all cats, it holds for Mimi.", "Si la regla vale para todos los gatos, vale para Mimi."), 1000),
        (new("Se todos os pássaros têm penas e Tico tem penas, então Tico é um pássaro.", "If all birds have feathers and Tico has feathers, then Tico is a bird.", "Si todos los pájaros tienen plumas y Tico tiene plumas, entonces Tico es un pájaro."), false, new("Ter penas não garante ser pássaro: a regra só diz que pássaros têm penas, não o contrário.", "Having feathers doesn't guarantee being a bird: the rule only says birds have feathers, not the other way around.", "Tener plumas no garantiza ser pájaro: la regla solo dice que los pájaros tienen plumas, no al revés."), 1550),
        (new("Multiplicar um número por 1 não muda o número.", "Multiplying a number by 1 doesn't change it.", "Multiplicar un número por 1 no lo cambia."), true, new("Qualquer número × 1 é ele mesmo.", "Any number × 1 is itself.", "Cualquier número × 1 es él mismo."), 950),
        (new("Multiplicar dois números sempre dá um resultado maior que os dois.", "Multiplying two numbers always gives a result bigger than both.", "Multiplicar dos números siempre da un resultado mayor que los dos."), false, new("3 × 1 = 3, que não é maior que 3; e 5 × 0 = 0.", "3 × 1 = 3, which isn't bigger than 3; and 5 × 0 = 0.", "3 × 1 = 3, que no es mayor que 3; y 5 × 0 = 0."), 1400),
    ];

    public override GeneratedProblem Generate(Random rng)
    {
        if (rng.Next(2) == 0)
        {
            var (statement, value, why, difficulty) = Pick(rng, Curated);
            return new(TrueFalse(statement, value,
                [
                    Tr("Leia com calma: \"todo\" quer dizer que não pode haver nenhuma exceção.", "Read carefully: \"every\" means there can't be a single exception.", "Lee con calma: \"todo\" quiere decir que no puede haber ninguna excepción."),
                    Tr("Tente encontrar um exemplo que mostre que a frase está errada.", "Try to find an example that shows the sentence is wrong.", "Intenta encontrar un ejemplo que muestre que la frase es falsa."),
                ], why), difficulty);
        }

        return rng.Next(3) switch
        {
            0 => Multiplication(rng),
            1 => Multiple(rng),
            _ => Comparison(rng),
        };
    }

    private static GeneratedProblem Multiplication(Random rng)
    {
        int a = Between(rng, 3, 9), b = Between(rng, 3, 9);
        var correct = rng.Next(2) == 0;
        var shown = correct ? a * b : a * b + Pick(rng, [-a, a, -1, 1, b]);
        return new(TrueFalse($"{a} × {b} = {shown}", correct, [Tr($"Calcule {a} × {b}.", $"Work out {a} × {b}.", $"Calcula {a} × {b}.")], $"{a} × {b} = {a * b}"), 950);
    }

    private static GeneratedProblem Multiple(Random rng)
    {
        var k = Between(rng, 3, 9);
        var n = rng.Next(2) == 0 ? k * Between(rng, 3, 12) : k * Between(rng, 3, 12) + Between(rng, 1, k - 1);
        var value = n % k == 0;
        return new(TrueFalse(Tr($"{n} é múltiplo de {k}.", $"{n} is a multiple of {k}.", $"{n} es múltiplo de {k}."), value,
            [
                Tr($"Um múltiplo de {k} é o resultado de {k} × algum número inteiro.", $"A multiple of {k} is the result of {k} × some whole number.", $"Un múltiplo de {k} es el resultado de {k} × algún número entero."),
                Tr($"Tente dividir {n} por {k}: sobra resto?", $"Try dividing {n} by {k}: is there a remainder?", $"Prueba a dividir {n} entre {k}: ¿sobra resto?"),
            ],
            value ? $"{n} = {k} × {n / k}." : Tr($"{n} ÷ {k} = {n / k} e sobra {n % k}.", $"{n} ÷ {k} = {n / k} with {n % k} left over.", $"{n} ÷ {k} = {n / k} y sobra {n % k}.")), 1150);
    }

    private static GeneratedProblem Comparison(Random rng)
    {
        int a = Between(rng, 2, 9), b = Between(rng, 2, 9), c = Between(rng, 2, 9), d = Between(rng, 2, 9);
        var left = a * b;
        var right = c + d * 2;
        var value = left > right;
        return new(TrueFalse(Tr($"{a} × {b} é maior que {c} + {d} × 2.", $"{a} × {b} is bigger than {c} + {d} × 2.", $"{a} × {b} es mayor que {c} + {d} × 2."), value,
            [
                Tr("Calcule cada lado separadamente.", "Work out each side separately.", "Calcula cada lado por separado."),
                Tr("Lembre: a multiplicação vem antes da soma.", "Remember: multiplication comes before addition.", "Recuerda: la multiplicación va antes que la suma."),
            ],
            Tr($"{a} × {b} = {left} e {c} + {d} × 2 = {right}.", $"{a} × {b} = {left} and {c} + {d} × 2 = {right}.", $"{a} × {b} = {left} y {c} + {d} × 2 = {right}.")), 1300);
    }
}

/// <summary>Balanças: pesos equivalentes, cadeias de trocas e equações simples com figuras.</summary>
public sealed class BalanceGenerator : ProblemGenerator
{
    public override string Key => "log.balancas";
    public override string SkillSlug => "balancas";
    public override string Level => "medio";

    // Todas femininas em português e espanhol, para o texto concordar ("uma maçã", "una pera").
    // O plural é sempre o nome + "s" nos três idiomas.
    private static readonly (string Emoji, Text Name)[] Things =
    [
        ("🍎", new("maçã", "apple", "manzana")), ("🍌", new("banana", "banana", "banana")), ("🍐", new("pera", "pear", "pera")),
        ("🍊", new("laranja", "orange", "naranja")), ("🍉", new("melancia", "watermelon", "sandía")),
    ];

    /// <summary>"Uma maçã" / "An apple" / "Una manzana".</summary>
    private static string One(string name, bool capital = false) => Current switch
    {
        En => $"{(capital ? "A" : "a")}{("aeiou".Contains(name[0]) ? "n" : "")} {name}",
        Es => $"{(capital ? "Una" : "una")} {name}",
        _ => $"{(capital ? "Uma" : "uma")} {name}",
    };

    public override GeneratedProblem Generate(Random rng)
    {
        var picked = Things.OrderBy(_ => rng.Next()).Take(3).ToArray();
        var (a, anText) = picked[0];
        var (b, bnText) = picked[1];
        var (c, cnText) = picked[2];
        string an = anText, bn = bnText, cn = cnText;

        switch (rng.Next(4))
        {
            case 0:
            {
                int na = Between(rng, 1, 3), k = Between(rng, 2, 4);
                var nb = na * k;
                return new(Numeric(Tr($"Na balança, {Repeat(a, na)} equilibra {Repeat(b, nb)}. {One(an, true)} equilibra quantas {bn}s?",
                                      $"On the scale, {Repeat(a, na)} balances {Repeat(b, nb)}. {One(an, true)} balances how many {bn}s?",
                                      $"En la balanza, {Repeat(a, na)} equilibra {Repeat(b, nb)}. ¿Cuántas {bn}s equilibra {One(an)}?"), k,
                    [
                        Tr($"Divida os dois lados da balança por {na}.", $"Divide both sides of the scale by {na}.", $"Divide los dos lados de la balanza entre {na}."),
                        $"{nb} ÷ {na} = ?",
                    ],
                    Tr($"{nb} ÷ {na} = {k}: {One(an)} equilibra {k} {bn}s.", $"{nb} ÷ {na} = {k}: {One(an)} balances {k} {bn}s.", $"{nb} ÷ {na} = {k}: {One(an)} equilibra {k} {bn}s."))
                    with { Visual = $"{Repeat(a, na)} ⚖️ {Repeat(b, nb)}" }, 1000 + na * 30);
            }
            case 1:
            {
                int k1 = Between(rng, 2, 4), k2 = Between(rng, 2, 4);
                return new(Numeric(Tr($"{One(an, true)} pesa o mesmo que {k1} {bn}s. {One(bn, true)} pesa o mesmo que {k2} {cn}s. {One(an, true)} pesa o mesmo que quantas {cn}s?",
                                      $"{One(an, true)} weighs the same as {k1} {bn}s. {One(bn, true)} weighs the same as {k2} {cn}s. {One(an, true)} weighs the same as how many {cn}s?",
                                      $"{One(an, true)} pesa lo mismo que {k1} {bn}s. {One(bn, true)} pesa lo mismo que {k2} {cn}s. ¿Cuántas {cn}s pesa {One(an)}?"), k1 * k2,
                    [Tr($"Troque cada {bn} por {k2} {cn}s.", $"Swap each {bn} for {k2} {cn}s.", $"Cambia cada {bn} por {k2} {cn}s."), $"{k1} × {k2} = ?"],
                    $"1 {an} = {k1} {bn}s = {k1} × {k2} {cn}s = {k1 * k2} {cn}s"), 1250);
            }
            case 2:
            {
                int wb = Between(rng, 1, 5), wa = Between(rng, 2, 8);
                var total = 2 * wa + wb;
                return new(Numeric(Tr($"{a} + {a} + {b} pesam {total} kg. Se {b} pesa {wb} kg, quanto pesa {One(an)}, em kg?",
                                      $"{a} + {a} + {b} weigh {total} kg. If {b} weighs {wb} kg, how much does {One(an)} weigh, in kg?",
                                      $"{a} + {a} + {b} pesan {total} kg. Si {b} pesa {wb} kg, ¿cuánto pesa {One(an)}, en kg?"), wa,
                    [
                        Tr($"Tire o peso de {b} do total: {total} − {wb}.", $"Take the weight of {b} away from the total: {total} − {wb}.", $"Quita el peso de {b} del total: {total} − {wb}."),
                        Tr($"O que sobra é o peso de duas {an}s.", $"What's left is the weight of two {an}s.", $"Lo que queda es el peso de dos {an}s."),
                    ], $"{total} − {wb} = {total - wb}; {total - wb} ÷ 2 = {wa} kg"), 1400);
            }
            default:
            {
                int wb = Between(rng, 1, 6), diff = Between(rng, 1, 6);
                var wa = wb + diff;
                return new(Numeric(Tr($"{a} + {b} pesam {wa + wb} kg e {a} pesa {diff} kg a mais que {b}. Quanto pesa {a}, em kg?",
                                      $"{a} + {b} weigh {wa + wb} kg and {a} weighs {diff} kg more than {b}. How much does {a} weigh, in kg?",
                                      $"{a} + {b} pesan {wa + wb} kg y {a} pesa {diff} kg más que {b}. ¿Cuánto pesa {a}, en kg?"), wa,
                    [
                        Tr($"Se tirarmos a diferença ({diff} kg), os dois pesariam igual.", $"If we take away the difference ({diff} kg), both would weigh the same.", $"Si quitamos la diferencia ({diff} kg), los dos pesarían lo mismo."),
                        Tr($"({wa + wb} − {diff}) ÷ 2 é o peso de {b}.", $"({wa + wb} − {diff}) ÷ 2 is the weight of {b}.", $"({wa + wb} − {diff}) ÷ 2 es el peso de {b}."),
                    ], $"{b} = ({wa + wb} − {diff}) ÷ 2 = {wb} kg; {a} = {wb} + {diff} = {wa} kg"), 1550);
            }
        }
    }
}

/// <summary>Tabelas lógicas simples: quem tem o quê, a partir de pistas.</summary>
public sealed class LogicTableGenerator : ProblemGenerator
{
    public override string Key => "log.tabelas";
    public override string SkillSlug => "tabelas-logicas";
    public override string Level => "medio";

    private static readonly string[] NamesPt = ["Ana", "Bruno", "Carla", "Davi", "Eva", "Felipe", "Gabi", "Hugo"];
    private static readonly string[] NamesEn = ["Amy", "Ben", "Carla", "David", "Eva", "Felix", "Grace", "Hugo"];
    private static readonly string[] NamesEs = ["Ana", "Bruno", "Carla", "Diego", "Eva", "Felipe", "Gabi", "Hugo"];
    internal static string[] Names => Current switch { En => NamesEn, Es => NamesEs, _ => NamesPt };

    private static readonly string[] PetsPt = ["o gato", "o cachorro", "o peixe", "o coelho", "o papagaio"];
    private static readonly string[] PetsEn = ["the cat", "the dog", "the fish", "the rabbit", "the parrot"];
    private static readonly string[] PetsEs = ["el gato", "el perro", "el pez", "el conejo", "el loro"];
    internal static string[] Pets => Current switch { En => PetsEn, Es => PetsEs, _ => PetsPt };

    /// <summary>"Ana, Bruno e Carla" / "Ana, Bruno and Carla" / "Ana, Bruno y Carla".</summary>
    internal static string JoinAnd(IReadOnlyList<string> items) =>
        $"{string.Join(", ", items.Take(items.Count - 1))} {Tr("e", "and", "y")} {items[^1]}";

    public override GeneratedProblem Generate(Random rng)
    {
        var n = rng.Next(3) == 0 ? 2 : 3;
        var people = Names.OrderBy(_ => rng.Next()).Take(n).ToArray();
        var pets = Pets.OrderBy(_ => rng.Next()).Take(n).ToArray();
        var category = new LogicCategory(pets, i => Tr($"tem {i}", $"has {i}", $"tiene {i}"), i => Tr($"não tem {i}", $"doesn't have {i}", $"no tiene {i}"));
        var asked = rng.Next(n);

        var puzzle = LogicGridTools.Generate(rng, n, 1,
            forbidden: clue => clue is PersonClue { Item: var item, Positive: true } && item == asked,
            questionAnswer: a => people[Array.IndexOf(a[0], asked)]);

        var answer = people[Array.IndexOf(puzzle.Solution[0], asked)];
        var clues = puzzle.Clues.Select(c => c.Text(people, [category])).ToList();
        var prompt = Tr(
            $"{JoinAnd(people)} têm, cada um, um animal diferente: {JoinAnd(pets)}. Pistas: {string.Join(" ", clues)} Quem tem {pets[asked]}?",
            $"{JoinAnd(people)} each have a different pet: {JoinAnd(pets)}. Clues: {string.Join(" ", clues)} Who has {pets[asked]}?",
            $"{JoinAnd(people)} tienen, cada uno, un animal distinto: {JoinAnd(pets)}. Pistas: {string.Join(" ", clues)} ¿Quién tiene {pets[asked]}?");
        var content = MultipleChoice(rng, prompt, answer, people,
            [
                Tr("Faça uma tabelinha: pessoas nas linhas, animais nas colunas.", "Make a little table: people in the rows, pets in the columns.", "Haz una tablita: personas en las filas, animales en las columnas."),
                Tr("Marque um X onde a pista diz que não pode. Se só sobrar uma opção numa linha, ela é a resposta.", "Mark an X where a clue says it can't be. If only one option is left in a row, that's the answer.", "Marca una X donde la pista dice que no puede ser. Si solo queda una opción en una fila, esa es la respuesta."),
            ],
            Tr($"Usando as pistas, só sobra uma possibilidade: {answer} tem {pets[asked]}.", $"Using the clues, only one possibility is left: {answer} has {pets[asked]}.", $"Con las pistas, solo queda una posibilidad: {answer} tiene {pets[asked]}."), count: n);
        return new(content, (n == 2 ? 900 : 1200) + clues.Count * 70);
    }
}
