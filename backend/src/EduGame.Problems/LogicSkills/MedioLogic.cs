using EduGame.Problems.Tools;

namespace EduGame.Problems.LogicSkills;

/// <summary>Sequências com regras compostas: multiplicação, quadrados, Fibonacci, diferenças crescentes.</summary>
public sealed class CompositeSequenceGenerator : ProblemGenerator
{
    public override string Key => "log.sequencias-compostas";
    public override string SkillSlug => "sequencias-compostas";
    public override string Level => "medio";

    private sealed record Rule(string Description, int Difficulty, Func<Random, int[]> Build);

    private static readonly Rule[] Rules =
    [
        new("multiplicar por 3 a cada passo", 1000, r => Iterate(Between(r, 1, 3), 6, x => x * 3)),
        new("multiplicar por 2 e somar 1", 1250, r => Iterate(Between(r, 1, 4), 6, x => x * 2 + 1)),
        new("os números são quadrados: 1×1, 2×2, 3×3…", 1200, r => { var s = Between(r, 1, 4); return [.. Enumerable.Range(s, 6).Select(i => i * i)]; }),
        new("cada número é a soma dos dois anteriores", 1350, r => Fibonacci(Between(r, 1, 3), Between(r, 1, 4))),
        new("duas sequências intercaladas: uma soma 2, a outra soma 5", 1250, r => Interleave(Between(r, 1, 9), 2, Between(r, 10, 20), 5)),
        new("as diferenças dobram: +1, +2, +4, +8…", 1400, r => Differences(Between(r, 1, 5), i => 1 << i)),
        new("as diferenças crescem: +2, +3, +4, +5…", 1300, r => Differences(Between(r, 1, 10), i => i + 2)),
        new("multiplicar por 2 e depois tirar 1, alternando", 1550, r => Alternate(Between(r, 3, 6), x => x * 2, x => x - 1)),
        new("dividir por 2 a cada passo", 1150, r => { var s = Between(r, 1, 3) << 6; return Iterate(s, 6, x => x / 2); }),
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
            "Qual número completa a sequência?",
            answer.ToString(),
            ["Calcule a diferença entre números vizinhos. Se não for sempre igual, olhe como ela muda.", "Tente também multiplicar ou dividir um número para chegar no próximo.", $"A regra é: {rule.Description}."],
            $"A regra é {rule.Description}, então o número que falta é {answer}.")
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
            "Complete o Sudoku: cada linha, cada coluna e cada quadrado 2×2 deve ter os números 1, 2, 3 e 4.",
            string.Join(',', solution),
            ["Procure uma linha, coluna ou quadrado em que só falta um número.", "Se um número já aparece na linha, ele não pode se repetir nela."],
            "Cada linha, coluna e quadrado tem 1, 2, 3 e 4 sem repetir.")
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
            "Quadrado mágico: use os números de 1 a 9, sem repetir, para que cada linha, coluna e diagonal some 15.",
            string.Join(',', square),
            ["O número do meio de um quadrado mágico 3×3 com 1 a 9 é sempre 5.", "Os números pares ficam nos cantos.", "Se uma linha tem dois números, o terceiro é 15 menos a soma deles."],
            "Todas as linhas, colunas e diagonais somam 15.")
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

    private static readonly (string Statement, bool Value, string Why, int Difficulty)[] Curated =
    [
        ("Todo quadrado é um retângulo.", true, "Um retângulo tem 4 ângulos retos; o quadrado também tem, e ainda os lados iguais.", 1250),
        ("Todo retângulo é um quadrado.", false, "Um retângulo pode ter lados de tamanhos diferentes, como 2 × 5.", 1050),
        ("A soma de dois números ímpares é sempre ímpar.", false, "3 + 5 = 8, que é par. Ímpar + ímpar dá sempre par.", 1250),
        ("A soma de dois números pares é sempre par.", true, "Par + par dá sempre par, como 4 + 6 = 10.", 1050),
        ("Se hoje é segunda-feira, depois de amanhã será quarta-feira.", true, "Segunda → terça (amanhã) → quarta (depois de amanhã).", 950),
        ("Se hoje é sexta-feira, ontem foi sábado.", false, "Ontem foi quinta-feira; sábado é amanhã.", 950),
        ("Todo número terminado em 0 é divisível por 5.", true, "Números terminados em 0 ou 5 são divisíveis por 5.", 1150),
        ("Todo número divisível por 5 termina em 0.", false, "15 é divisível por 5 e termina em 5.", 1300),
        ("Um triângulo pode ter dois ângulos retos.", false, "Os ângulos de um triângulo somam 180°; dois retos já dariam 180°, sem sobrar para o terceiro.", 1550),
        ("Metade de uma metade é um quarto.", true, "1/2 de 1/2 = 1/4.", 1200),
        ("Existe um número que é par e ímpar ao mesmo tempo.", false, "Todo número inteiro é par ou ímpar, nunca os dois.", 1100),
        ("Se todos os gatos têm bigodes e Mimi é um gato, então Mimi tem bigodes.", true, "Se a regra vale para todos os gatos, vale para Mimi.", 1000),
        ("Se todos os pássaros têm penas e Tico tem penas, então Tico é um pássaro.", false, "Ter penas não garante ser pássaro: a regra só diz que pássaros têm penas, não o contrário.", 1550),
        ("Multiplicar um número por 1 não muda o número.", true, "Qualquer número × 1 é ele mesmo.", 950),
        ("Multiplicar dois números sempre dá um resultado maior que os dois.", false, "3 × 1 = 3, que não é maior que 3; e 5 × 0 = 0.", 1400),
    ];

    public override GeneratedProblem Generate(Random rng)
    {
        if (rng.Next(2) == 0)
        {
            var (statement, value, why, difficulty) = Pick(rng, Curated);
            return new(TrueFalse(statement, value, ["Leia com calma: \"todo\" quer dizer que não pode haver nenhuma exceção.", "Tente encontrar um exemplo que mostre que a frase está errada."], why), difficulty);
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
        return new(TrueFalse($"{a} × {b} = {shown}", correct, [$"Calcule {a} × {b}."], $"{a} × {b} = {a * b}"), 950);
    }

    private static GeneratedProblem Multiple(Random rng)
    {
        var k = Between(rng, 3, 9);
        var n = rng.Next(2) == 0 ? k * Between(rng, 3, 12) : k * Between(rng, 3, 12) + Between(rng, 1, k - 1);
        var value = n % k == 0;
        return new(TrueFalse($"{n} é múltiplo de {k}.", value, [$"Um múltiplo de {k} é o resultado de {k} × algum número inteiro.", $"Tente dividir {n} por {k}: sobra resto?"],
            value ? $"{n} = {k} × {n / k}." : $"{n} ÷ {k} = {n / k} e sobra {n % k}."), 1150);
    }

    private static GeneratedProblem Comparison(Random rng)
    {
        int a = Between(rng, 2, 9), b = Between(rng, 2, 9), c = Between(rng, 2, 9), d = Between(rng, 2, 9);
        var left = a * b;
        var right = c + d * 2;
        var value = left > right;
        return new(TrueFalse($"{a} × {b} é maior que {c} + {d} × 2.", value, ["Calcule cada lado separadamente.", "Lembre: a multiplicação vem antes da soma."],
            $"{a} × {b} = {left} e {c} + {d} × 2 = {right}."), 1300);
    }
}

/// <summary>Balanças: pesos equivalentes, cadeias de trocas e equações simples com figuras.</summary>
public sealed class BalanceGenerator : ProblemGenerator
{
    public override string Key => "log.balancas";
    public override string SkillSlug => "balancas";
    public override string Level => "medio";

    // Todas femininas, para o texto concordar ("uma maçã", "duas peras").
    private static readonly (string Emoji, string Name)[] Things = [("🍎", "maçã"), ("🍌", "banana"), ("🍐", "pera"), ("🍊", "laranja"), ("🍉", "melancia")];

    public override GeneratedProblem Generate(Random rng)
    {
        var picked = Things.OrderBy(_ => rng.Next()).Take(3).ToArray();
        var (a, an) = picked[0];
        var (b, bn) = picked[1];
        var (c, cn) = picked[2];

        switch (rng.Next(4))
        {
            case 0:
            {
                int na = Between(rng, 1, 3), k = Between(rng, 2, 4);
                var nb = na * k;
                return new(Numeric($"Na balança, {Repeat(a, na)} equilibra {Repeat(b, nb)}. Uma {an} equilibra quantas {bn}s?", k,
                    [$"Divida os dois lados da balança por {na}.", $"{nb} ÷ {na} = ?"], $"{nb} ÷ {na} = {k}: uma {an} equilibra {k} {bn}s.")
                    with { Visual = $"{Repeat(a, na)} ⚖️ {Repeat(b, nb)}" }, 1000 + na * 30);
            }
            case 1:
            {
                int k1 = Between(rng, 2, 4), k2 = Between(rng, 2, 4);
                return new(Numeric($"Uma {an} pesa o mesmo que {k1} {bn}s. Uma {bn} pesa o mesmo que {k2} {cn}s. Uma {an} pesa o mesmo que quantas {cn}s?", k1 * k2,
                    [$"Troque cada {bn} por {k2} {cn}s.", $"{k1} × {k2} = ?"], $"1 {an} = {k1} {bn}s = {k1} × {k2} {cn}s = {k1 * k2} {cn}s"), 1250);
            }
            case 2:
            {
                int wb = Between(rng, 1, 5), wa = Between(rng, 2, 8);
                var total = 2 * wa + wb;
                return new(Numeric($"{a} + {a} + {b} pesam {total} kg. Se {b} pesa {wb} kg, quanto pesa uma {an}, em kg?", wa,
                    [$"Tire o peso de {b} do total: {total} − {wb}.", $"O que sobra é o peso de duas {an}s."], $"{total} − {wb} = {total - wb}; {total - wb} ÷ 2 = {wa} kg"), 1400);
            }
            default:
            {
                int wb = Between(rng, 1, 6), diff = Between(rng, 1, 6);
                var wa = wb + diff;
                return new(Numeric($"{a} + {b} pesam {wa + wb} kg e {a} pesa {diff} kg a mais que {b}. Quanto pesa {a}, em kg?", wa,
                    [$"Se tirarmos a diferença ({diff} kg), os dois pesariam igual.", $"({wa + wb} − {diff}) ÷ 2 é o peso de {b}."], $"{b} = ({wa + wb} − {diff}) ÷ 2 = {wb} kg; {a} = {wb} + {diff} = {wa} kg"), 1550);
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

    internal static readonly string[] Names = ["Ana", "Bruno", "Carla", "Davi", "Eva", "Felipe", "Gabi", "Hugo"];
    internal static readonly string[] Pets = ["o gato", "o cachorro", "o peixe", "o coelho", "o papagaio"];

    public override GeneratedProblem Generate(Random rng)
    {
        var n = rng.Next(3) == 0 ? 2 : 3;
        var people = Names.OrderBy(_ => rng.Next()).Take(n).ToArray();
        var pets = Pets.OrderBy(_ => rng.Next()).Take(n).ToArray();
        var category = new LogicCategory(pets, i => $"tem {i}", i => $"não tem {i}");
        var asked = rng.Next(n);

        var puzzle = LogicGridTools.Generate(rng, n, 1,
            forbidden: clue => clue is PersonClue { Item: var item, Positive: true } && item == asked,
            questionAnswer: a => people[Array.IndexOf(a[0], asked)]);

        var answer = people[Array.IndexOf(puzzle.Solution[0], asked)];
        var clues = puzzle.Clues.Select(c => c.Text(people, [category])).ToList();
        var prompt = $"{string.Join(", ", people[..^1])} e {people[^1]} têm, cada um, um animal diferente: {string.Join(", ", pets[..^1])} e {pets[^1]}. Pistas: {string.Join(" ", clues)} Quem tem {pets[asked]}?";
        var content = MultipleChoice(rng, prompt, answer, people,
            ["Faça uma tabelinha: pessoas nas linhas, animais nas colunas.", "Marque um X onde a pista diz que não pode. Se só sobrar uma opção numa linha, ela é a resposta."],
            $"Usando as pistas, só sobra uma possibilidade: {answer} tem {pets[asked]}.", count: n);
        return new(content, (n == 2 ? 900 : 1200) + clues.Count * 70);
    }
}
