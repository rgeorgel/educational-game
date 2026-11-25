namespace EduGame.Problems;

public interface IProblemGenerator
{
    /// <summary>Identificador estável (gravado no banco).</summary>
    string Key { get; }

    /// <summary>Habilidade que este gerador exercita.</summary>
    string SkillSlug { get; }

    /// <summary>Nível do conteúdo ("iniciante", "medio", "avancado"), usado em testes e no catálogo.</summary>
    string Level { get; }

    GeneratedProblem Generate(Random rng);
}

/// <summary>Base com utilitários comuns aos geradores.</summary>
public abstract class ProblemGenerator : IProblemGenerator
{
    public abstract string Key { get; }
    public abstract string SkillSlug { get; }
    public virtual string Level => "iniciante";
    public abstract GeneratedProblem Generate(Random rng);

    protected static string Num(decimal value) => AnswerChecker.FormatNumber(value);

    /// <summary>"Quanto é 3 + 4?" no idioma atual.</summary>
    protected static string HowMuch(string expression) =>
        Lang.Tr($"Quanto é {expression}?", $"What is {expression}?", $"¿Cuánto es {expression}?");

    /// <summary>
    /// Problema de resposta numérica. Use <paramref name="negatives"/>/<paramref name="decimals"/> para a habilidade inteira
    /// (e não só quando a resposta é negativa/decimal), senão o teclado entregaria a dica.
    /// </summary>
    protected static ProblemContent Numeric(string prompt, decimal answer, IReadOnlyList<string> hints, string explanation, bool negatives = false, bool decimals = false) =>
        new(ProblemTypes.Numero, prompt, Num(answer), hints, explanation)
        {
            AcceptsNegative = negatives || answer < 0,
            AcceptsDecimal = decimals || answer != decimal.Truncate(answer),
        };

    protected static ProblemContent MultipleChoice(Random rng, string prompt, string answer, IEnumerable<string> distractors, IReadOnlyList<string> hints, string explanation, int count = 4) =>
        new(ProblemTypes.MultiplaEscolha, prompt, answer, hints, explanation) { Choices = Choices(rng, answer, distractors, count) };

    protected static ProblemContent TrueFalse(string statement, bool value, IReadOnlyList<string> hints, string explanation) =>
        new(ProblemTypes.VerdadeiroFalso, statement, value ? ProblemTypes.True : ProblemTypes.False, hints, explanation)
        {
            Choices = [ProblemTypes.True, ProblemTypes.False],
        };

    protected static T Pick<T>(Random rng, IReadOnlyList<T> items) => items[rng.Next(items.Count)];

    /// <summary>Inteiro em [min, max] (inclusivo).</summary>
    protected static int Between(Random rng, int min, int max) => rng.Next(min, max + 1);

    /// <summary>Embaralha as opções garantindo que não haja repetidas.</summary>
    protected static IReadOnlyList<string> Choices(Random rng, string answer, IEnumerable<string> distractors, int count = 4)
    {
        var options = new List<string> { answer };
        foreach (var d in distractors)
        {
            if (options.Count >= count) break;
            if (!options.Contains(d)) options.Add(d);
        }
        return [.. options.OrderBy(_ => rng.Next())];
    }

    /// <summary>Distratores numéricos plausíveis (erros comuns: ±1, ±10).</summary>
    protected static IEnumerable<string> NumericDistractors(Random rng, int answer, params int[] extra)
    {
        var candidates = extra.Concat([answer + 1, answer - 1, answer + 10, answer - 10, answer + 2])
            .Where(n => n >= 0 && n != answer)
            .Distinct()
            .OrderBy(_ => rng.Next());
        return candidates.Select(n => n.ToString());
    }

    protected static string Repeat(string emoji, int count) => string.Concat(Enumerable.Repeat(emoji, count));
}
