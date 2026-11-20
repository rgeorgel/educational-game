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

    private static readonly string Legend = "Lembre: ¬ é NÃO, ∧ é E, ∨ é OU e → é SE…ENTÃO (só é falso quando V → F).";

    public override GeneratedProblem Generate(Random rng)
    {
        var three = rng.Next(3) == 0;
        char[] vars = three ? ['P', 'Q', 'R'] : ['P', 'Q'];
        Expr expr;
        do expr = Random(rng, vars, 2); while (expr.Size < 4);

        if (rng.Next(2) == 0)
        {
            var values = vars.ToDictionary(c => c, _ => rng.Next(2) == 0);
            var given = string.Join(", ", vars.Select(c => $"{c} = {(values[c] ? "V" : "F")}"));
            var result = expr.Eval(values);
            return new(TrueFalse($"Se {given}, então a expressão {expr.Text()} é verdadeira.", result,
                [Legend, "Resolva de dentro para fora: primeiro os parênteses e as negações."],
                $"Com {given}, {expr.Text()} vale {(result ? "V" : "F")}."), 1880 + expr.Size * 40 + (three ? 60 : 0));
        }

        var rows = Enumerable.Range(0, 1 << vars.Length)
            .Select(mask => vars.Select((c, i) => (c, ((mask >> i) & 1) == 1)).ToDictionary(x => x.c, x => x.Item2))
            .ToList();
        var count = rows.Count(r => expr.Eval(r));
        return new(Numeric($"A tabela-verdade de {expr.Text()} tem {rows.Count} linhas. Em quantas delas a expressão é verdadeira?", count,
            [Legend, $"Monte a tabela com todas as {rows.Count} combinações de V e F para {string.Join(", ", vars)}."],
            $"A expressão é verdadeira em {count} das {rows.Count} linhas."), 2150 + expr.Size * 40 + (three ? 120 : 0));
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
            yield return new($"{Names[jj]} é mentiroso(a).", a => !a[jj]);
            yield return new($"{Names[jj]} é cavaleiro(a).", a => a[jj]);
            yield return new($"{Names[jj]} e eu somos do mesmo tipo.", a => a[jj] == a[speaker]);
            yield return new($"{Names[jj]} e eu somos de tipos diferentes.", a => a[jj] != a[speaker]);
        }
        yield return new("Pelo menos um de nós é mentiroso.", a => a.Any(k => !k));
        yield return new("Todos nós somos cavaleiros.", a => a.All(k => k));
        if (n == 3)
            yield return new("Exatamente um de nós é cavaleiro.", a => a.Count(k => k) == 1);
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
            var intro = "Numa ilha, os cavaleiros sempre dizem a verdade e os mentirosos sempre mentem.";
            List<string> hints = ["Suponha que a primeira pessoa é cavaleiro e veja se tudo fica coerente. Depois teste o contrário.", "Uma frase de um cavaleiro é verdadeira; a de um mentiroso é falsa."];
            var explanation = "A única combinação coerente é: " + string.Join(", ", Enumerable.Range(0, n).Select(i => $"{Names[i]} é {(truth[i] ? "cavaleiro(a)" : "mentiroso(a)")}")) + ".";

            if (n == 3 && rng.Next(2) == 0)
            {
                var knaves = truth.Count(k => !k);
                return new(Numeric($"{intro} Três moradores falaram. Quantos deles são mentirosos?", knaves, hints, explanation) with { Code = lines }, 2350 + said.Count * 30);
            }

            var asked = rng.Next(n);
            var answer = truth[asked] ? "Cavaleiro(a)" : "Mentiroso(a)";
            var content = new ProblemContent(ProblemTypes.MultiplaEscolha,
                $"{intro} O que {Names[asked]} é?", answer, hints, explanation)
            { Choices = ["Cavaleiro(a)", "Mentiroso(a)"], Code = lines };
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

    private const string Prompt = "O que o programa mostra no final?";
    private static readonly List<string> TraceHints = ["Faça uma tabelinha com o valor de cada variável a cada passo.", "Execute uma linha de cada vez, sem pular."];

    public override GeneratedProblem Generate(Random rng)
    {
        switch (rng.Next(5))
        {
            case 0:
            {
                int x = Between(rng, 1, 10), times = Between(rng, 3, 6), add = Between(rng, 2, 5);
                var result = x + times * add;
                return Make($"x ← {x}\nrepita {times} vezes:\n    x ← x + {add}\nmostre x", result, $"x começa em {x} e ganha {add} por {times} vezes: {x} + {times} × {add} = {result}.", 2050);
            }
            case 1:
            {
                int x = Between(rng, 1, 12), limit = Between(rng, 4, 9);
                var result = x > limit ? x * 2 : x + 10;
                return Make($"x ← {x}\nse x > {limit} então\n    x ← x × 2\nsenão\n    x ← x + 10\nmostre x", result, $"{x} > {limit} é {(x > limit ? "verdadeiro" : "falso")}, então x vira {result}.", 2100);
            }
            case 2:
            {
                var n = Between(rng, 4, 10);
                var step = Between(rng, 1, 2);
                var loop = step == 1 ? $"para i de 1 até {n}:" : $"para i de 1 até {n}, de 2 em 2:";
                var values = Enumerable.Range(1, n).Where((_, idx) => idx % step == 0).ToList();
                return Make($"soma ← 0\n{loop}\n    soma ← soma + i\nmostre soma", values.Sum(), $"Soma de {string.Join(" + ", values)} = {values.Sum()}.", 2200 + (step == 2 ? 60 : 0));
            }
            case 3:
            {
                int x = Between(rng, 1, 5), limit = Pick(rng, [30, 50, 100, 200]);
                var count = 0;
                var v = x;
                while (v < limit) { v *= 2; count++; }
                return Make($"x ← {x}\ncontador ← 0\nenquanto x < {limit}:\n    x ← x × 2\n    contador ← contador + 1\nmostre contador", count, $"x dobra {count} vezes até passar de {limit} (chega a {v}).", 2300);
            }
            default:
            {
                int a = Between(rng, 2, 15), b = Between(rng, 2, 15);
                return Make($"a ← {a}\nb ← {b}\na ← a + b\nb ← a − b\na ← a − b\nmostre a", b, $"Depois das três linhas, a e b trocaram de valor: a = {b} e b = {a}.", 2400);
            }
        }

        static GeneratedProblem Make(string code, int value, string explanation, int difficulty) =>
            new(Numeric(Prompt, value, TraceHints, explanation) with { Code = code }, difficulty);
    }
}
