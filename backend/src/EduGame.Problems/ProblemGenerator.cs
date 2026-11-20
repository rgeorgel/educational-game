namespace EduGame.Problems;

public interface IProblemGenerator
{
    /// <summary>Identificador estável (gravado no banco).</summary>
    string Key { get; }

    /// <summary>Habilidade que este gerador exercita.</summary>
    string SkillSlug { get; }

    GeneratedProblem Generate(Random rng);
}

/// <summary>Base com utilitários comuns aos geradores.</summary>
public abstract class ProblemGenerator : IProblemGenerator
{
    public abstract string Key { get; }
    public abstract string SkillSlug { get; }
    public abstract GeneratedProblem Generate(Random rng);

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
