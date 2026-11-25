using static EduGame.Problems.Lang;

namespace EduGame.Problems.LogicSkills;

/// <summary>Tabelas-verdade: avaliar expressões com ¬, ∧, ∨, → e contar linhas verdadeiras.</summary>
public sealed class TruthTableGenerator : ProblemGenerator
{
    public override string Key => "log.tabelas-verdade";
    public override string SkillSlug => "tabelas-verdade";
    public override string Level => "expert";

    private abstract record Expr
    {
        public abstract bool Eval(IReadOnlyDictionary<char, bool> v);
        public abstract string Text(bool top = true);
        public abstract int Size { get; }
    }

    private sealed record Var(char Name) : Expr
    {
        public override bool Eval(IReadOnlyDictionary<char, bool> v) => v[Name];
        public override string Text(bool top = true) => Name.ToString();
        public override int Size => 1;
    }

    private sealed record Not(Expr Inner) : Expr
    {
        public override bool Eval(IReadOnlyDictionary<char, bool> v) => !Inner.Eval(v);
        public override string Text(bool top = true) => $"¬{Inner.Text(false)}";
        public override int Size => 1 + Inner.Size;
    }

    private sealed record Bin(string Op, Expr L, Expr R) : Expr
    {
        public override bool Eval(IReadOnlyDictionary<char, bool> v) => Op switch
        {
            "∧" => L.Eval(v) && R.Eval(v),
            "∨" => L.Eval(v) || R.Eval(v),
            _ => !L.Eval(v) || R.Eval(v),
        };

        public override string Text(bool top = true) => top ? $"{L.Text(false)} {Op} {R.Text(false)}" : $"({L.Text(false)} {Op} {R.Text(false)})";
        public override int Size => 1 + L.Size + R.Size;
    }

    private static Expr Random(Random rng, char[] vars, int depth)
    {
        if (depth == 0 || rng.Next(4) == 0)
        {
            Expr leaf = new Var(vars[rng.Next(vars.Length)]);
            return rng.Next(3) == 0 ? new Not(leaf) : leaf;
        }
        var op = Pick(rng, ["∧", "∨", "→"]);
        Expr e = new Bin(op, Random(rng, vars, depth - 1), Random(rng, vars, depth - 1));
        return rng.Next(5) == 0 ? new Not(e) : e;
    }

    private static string Legend => Tr(
        "Lembre: ¬ é NÃO, ∧ é E, ∨ é OU e → é SE…ENTÃO (só é falso quando V → F).",
        "Remember: ¬ is NOT, ∧ is AND, ∨ is OR and → is IF…THEN (only false when T → F).",
        "Recuerda: ¬ es NO, ∧ es Y, ∨ es O y → es SI…ENTONCES (solo es falso cuando V → F).");

    /// <summary>Letra de verdadeiro/falso nas tabelas: V/F (português e espanhol) ou T/F (inglês).</summary>
    private static string Tv(bool value) => value ? Tr("V", "T", "V") : "F";

    public override GeneratedProblem Generate(Random rng)
    {
        var three = rng.Next(3) == 0;
        char[] vars = three ? ['P', 'Q', 'R'] : ['P', 'Q'];
        Expr expr;
        do expr = Random(rng, vars, 2); while (expr.Size < 4);

        if (rng.Next(2) == 0)
        {
            var values = vars.ToDictionary(c => c, _ => rng.Next(2) == 0);
            var given = string.Join(", ", vars.Select(c => $"{c} = {Tv(values[c])}"));
            var result = expr.Eval(values);
            return new(TrueFalse(Tr($"Se {given}, então a expressão {expr.Text()} é verdadeira.", $"If {given}, then the expression {expr.Text()} is true.", $"Si {given}, entonces la expresión {expr.Text()} es verdadera."), result,
                [Legend, Tr("Resolva de dentro para fora: primeiro os parênteses e as negações.", "Work from the inside out: parentheses and negations first.", "Resuelve de dentro hacia fuera: primero los paréntesis y las negaciones.")],
                Tr($"Com {given}, {expr.Text()} vale {Tv(result)}.", $"With {given}, {expr.Text()} is {Tv(result)}.", $"Con {given}, {expr.Text()} vale {Tv(result)}.")), 1880 + expr.Size * 40 + (three ? 60 : 0));
        }

        var rows = Enumerable.Range(0, 1 << vars.Length)
            .Select(mask => vars.Select((c, i) => (c, ((mask >> i) & 1) == 1)).ToDictionary(x => x.c, x => x.Item2))
            .ToList();
        var count = rows.Count(r => expr.Eval(r));
        var letters = string.Join(", ", vars);
        return new(Numeric(Tr($"A tabela-verdade de {expr.Text()} tem {rows.Count} linhas. Em quantas delas a expressão é verdadeira?", $"The truth table of {expr.Text()} has {rows.Count} rows. In how many of them is the expression true?", $"La tabla de verdad de {expr.Text()} tiene {rows.Count} filas. ¿En cuántas de ellas la expresión es verdadera?"), count,
            [Legend, Tr($"Monte a tabela com todas as {rows.Count} combinações de V e F para {letters}.", $"Build the table with all {rows.Count} combinations of T and F for {letters}.", $"Arma la tabla con las {rows.Count} combinaciones de V y F para {letters}.")],
            Tr($"A expressão é verdadeira em {count} das {rows.Count} linhas.", $"The expression is true in {count} of the {rows.Count} rows.", $"La expresión es verdadera en {count} de las {rows.Count} filas.")), 2150 + expr.Size * 40 + (three ? 120 : 0));
    }
}

/// <summary>
/// Cavaleiros e mentirosos: cavaleiros sempre dizem a verdade; mentirosos sempre mentem.
/// O gerador sorteia quem é quem e frases coerentes até a solução ser única.
/// </summary>
public sealed class KnightsKnavesGenerator : ProblemGenerator
{
    public override string Key => "log.cavaleiros-mentirosos";
    public override string SkillSlug => "cavaleiros-mentirosos";
    public override string Level => "expert";

    private static readonly string[] Names = ["Ari", "Bela", "Caio", "Duda"];

    private sealed record Statement(string Text, Func<bool[], bool> Holds);

    private static IEnumerable<Statement> Candidates(int speaker, int n)
    {
        for (var j = 0; j < n; j++)
        {
            if (j == speaker) continue;
            var jj = j;
            var name = Names[jj];
            yield return new(Tr($"{name} é mentiroso(a).", $"{name} is a knave.", $"{name} es mentiroso(a)."), a => !a[jj]);
            yield return new(Tr($"{name} é cavaleiro(a).", $"{name} is a knight.", $"{name} es caballero(a)."), a => a[jj]);
            yield return new(Tr($"{name} e eu somos do mesmo tipo.", $"{name} and I are the same type.", $"{name} y yo somos del mismo tipo."), a => a[jj] == a[speaker]);
            yield return new(Tr($"{name} e eu somos de tipos diferentes.", $"{name} and I are different types.", $"{name} y yo somos de tipos distintos."), a => a[jj] != a[speaker]);
        }
        yield return new(Tr("Pelo menos um de nós é mentiroso.", "At least one of us is a knave.", "Al menos uno de nosotros es mentiroso."), a => a.Any(k => !k));
        yield return new(Tr("Todos nós somos cavaleiros.", "We are all knights.", "Todos nosotros somos caballeros."), a => a.All(k => k));
        if (n == 3)
            yield return new(Tr("Exatamente um de nós é cavaleiro.", "Exactly one of us is a knight.", "Exactamente uno de nosotros es caballero."), a => a.Count(k => k) == 1);
    }

    public override GeneratedProblem Generate(Random rng)
    {
        var n = rng.Next(3) == 0 ? 3 : 2;
        var all = Enumerable.Range(0, 1 << n).Select(m => Enumerable.Range(0, n).Select(i => ((m >> i) & 1) == 1).ToArray()).ToList();

        for (var attempt = 0; ; attempt++)
        {
            var truth = all[rng.Next(all.Count)];
            // Cada pessoa diz uma frase coerente com o seu tipo.
            var said = new List<(int Speaker, Statement S)>();
            for (var p = 0; p < n; p++)
            {
                var options = Candidates(p, n).Where(s => s.Holds(truth) == truth[p]).ToList();
                said.Add((p, options[rng.Next(options.Count)]));
            }

            bool Consistent(bool[] a) => said.All(x => x.S.Holds(a) == a[x.Speaker]);
            var solutions = all.Where(Consistent).ToList();
            if (solutions.Count != 1)
            {
                if (attempt > 5000) throw new InvalidOperationException("Não foi possível gerar um problema com solução única.");
                continue;
            }

            var lines = string.Join("\n", said.Select(x => $"{Names[x.Speaker]}: “{x.S.Text}”"));
            var intro = Tr("Numa ilha, os cavaleiros sempre dizem a verdade e os mentirosos sempre mentem.", "On an island, knights always tell the truth and knaves always lie.", "En una isla, los caballeros siempre dicen la verdad y los mentirosos siempre mienten.");
            List<string> hints =
            [
                Tr("Suponha que a primeira pessoa é cavaleiro e veja se tudo fica coerente. Depois teste o contrário.", "Suppose the first person is a knight and see whether everything fits. Then test the opposite.", "Supón que la primera persona es caballero y mira si todo encaja. Después prueba lo contrario."),
                Tr("Uma frase de um cavaleiro é verdadeira; a de um mentiroso é falsa.", "A knight's sentence is true; a knave's is false.", "La frase de un caballero es verdadera; la de un mentiroso es falsa."),
            ];
            var knight = Tr("cavaleiro(a)", "a knight", "caballero(a)");
            var knave = Tr("mentiroso(a)", "a knave", "mentiroso(a)");
            var types = string.Join(", ", Enumerable.Range(0, n).Select(i => Tr($"{Names[i]} é {(truth[i] ? knight : knave)}", $"{Names[i]} is {(truth[i] ? knight : knave)}", $"{Names[i]} es {(truth[i] ? knight : knave)}")));
            var explanation = Tr($"A única combinação coerente é: {types}.", $"The only combination that fits is: {types}.", $"La única combinación coherente es: {types}.");

            if (n == 3 && rng.Next(2) == 0)
            {
                var knaves = truth.Count(k => !k);
                return new(Numeric(Tr($"{intro} Três moradores falaram. Quantos deles são mentirosos?", $"{intro} Three islanders spoke. How many of them are knaves?", $"{intro} Hablaron tres habitantes. ¿Cuántos de ellos son mentirosos?"), knaves, hints, explanation) with { Code = lines }, 2350 + said.Count * 30);
            }

            var asked = rng.Next(n);
            var knightChoice = Tr("Cavaleiro(a)", "Knight", "Caballero(a)");
            var knaveChoice = Tr("Mentiroso(a)", "Knave", "Mentiroso(a)");
            var answer = truth[asked] ? knightChoice : knaveChoice;
            var content = new ProblemContent(ProblemTypes.MultiplaEscolha,
                Tr($"{intro} O que {Names[asked]} é?", $"{intro} What is {Names[asked]}?", $"{intro} ¿Qué es {Names[asked]}?"), answer, hints, explanation)
            { Choices = [knightChoice, knaveChoice], Code = lines };
            return new(content, (n == 2 ? 1980 : 2250) + said.Count * 30);
        }
    }
}

/// <summary>Pensamento computacional: seguir passo a passo pequenos programas em pseudocódigo.</summary>
public sealed class ComputationalThinkingGenerator : ProblemGenerator
{
    public override string Key => "log.pensamento-computacional";
    public override string SkillSlug => "pensamento-computacional";
    public override string Level => "expert";

    private static string Prompt => Tr("O que o programa mostra no final?", "What does the program show at the end?", "¿Qué muestra el programa al final?");

    private static List<string> TraceHints =>
    [
        Tr("Faça uma tabelinha com o valor de cada variável a cada passo.", "Make a little table with the value of each variable at every step.", "Haz una tablita con el valor de cada variable en cada paso."),
        Tr("Execute uma linha de cada vez, sem pular.", "Run one line at a time, without skipping.", "Ejecuta una línea cada vez, sin saltarte ninguna."),
    ];

    // Palavras do pseudocódigo em cada idioma.
    private static string Show => Tr("mostre", "show", "mostrar");
    private static string Repeat(int times) => Tr($"repita {times} vezes:", $"repeat {times} times:", $"repetir {times} veces:");

    public override GeneratedProblem Generate(Random rng)
    {
        switch (rng.Next(5))
        {
            case 0:
            {
                int x = Between(rng, 1, 10), times = Between(rng, 3, 6), add = Between(rng, 2, 5);
                var result = x + times * add;
                return Make($"x ← {x}\n{Repeat(times)}\n    x ← x + {add}\n{Show} x", result,
                    Tr($"x começa em {x} e ganha {add} por {times} vezes: {x} + {times} × {add} = {result}.", $"x starts at {x} and gets {add} more {times} times: {x} + {times} × {add} = {result}.", $"x empieza en {x} y suma {add} {times} veces: {x} + {times} × {add} = {result}."), 2050);
            }
            case 1:
            {
                int x = Between(rng, 1, 12), limit = Between(rng, 4, 9);
                var result = x > limit ? x * 2 : x + 10;
                var code = Tr($"x ← {x}\nse x > {limit} então\n    x ← x × 2\nsenão\n    x ← x + 10\nmostre x",
                              $"x ← {x}\nif x > {limit} then\n    x ← x × 2\nelse\n    x ← x + 10\nshow x",
                              $"x ← {x}\nsi x > {limit} entonces\n    x ← x × 2\nsi no\n    x ← x + 10\nmostrar x");
                var holds = x > limit ? Tr("verdadeiro", "true", "verdadero") : Tr("falso", "false", "falso");
                return Make(code, result, Tr($"{x} > {limit} é {holds}, então x vira {result}.", $"{x} > {limit} is {holds}, so x becomes {result}.", $"{x} > {limit} es {holds}, así que x pasa a ser {result}."), 2100);
            }
            case 2:
            {
                var n = Between(rng, 4, 10);
                var step = Between(rng, 1, 2);
                var loop = step == 1
                    ? Tr($"para i de 1 até {n}:", $"for i from 1 to {n}:", $"para i desde 1 hasta {n}:")
                    : Tr($"para i de 1 até {n}, de 2 em 2:", $"for i from 1 to {n}, in steps of 2:", $"para i desde 1 hasta {n}, de 2 en 2:");
                var values = Enumerable.Range(1, n).Where((_, idx) => idx % step == 0).ToList();
                var sum = Tr("soma", "total", "suma");
                return Make($"{sum} ← 0\n{loop}\n    {sum} ← {sum} + i\n{Show} {sum}", values.Sum(),
                    Tr($"Soma de {string.Join(" + ", values)} = {values.Sum()}.", $"Sum of {string.Join(" + ", values)} = {values.Sum()}.", $"Suma de {string.Join(" + ", values)} = {values.Sum()}."), 2200 + (step == 2 ? 60 : 0));
            }
            case 3:
            {
                int x = Between(rng, 1, 5), limit = Pick(rng, [30, 50, 100, 200]);
                var count = 0;
                var v = x;
                while (v < limit) { v *= 2; count++; }
                var counter = Tr("contador", "counter", "contador");
                var loop = Tr($"enquanto x < {limit}:", $"while x < {limit}:", $"mientras x < {limit}:");
                return Make($"x ← {x}\n{counter} ← 0\n{loop}\n    x ← x × 2\n    {counter} ← {counter} + 1\n{Show} {counter}", count,
                    Tr($"x dobra {count} vezes até passar de {limit} (chega a {v}).", $"x doubles {count} times until it passes {limit} (it reaches {v}).", $"x se duplica {count} veces hasta pasar de {limit} (llega a {v})."), 2300);
            }
            default:
            {
                int a = Between(rng, 2, 15), b = Between(rng, 2, 15);
                return Make($"a ← {a}\nb ← {b}\na ← a + b\nb ← a − b\na ← a − b\n{Show} a", b,
                    Tr($"Depois das três linhas, a e b trocaram de valor: a = {b} e b = {a}.", $"After the three lines, a and b swapped values: a = {b} and b = {a}.", $"Después de las tres líneas, a y b intercambiaron su valor: a = {b} y b = {a}."), 2400);
            }
        }

        static GeneratedProblem Make(string code, int value, string explanation, int difficulty) =>
            new(Numeric(Prompt, value, TraceHints, explanation) with { Code = code }, difficulty);
    }
}
