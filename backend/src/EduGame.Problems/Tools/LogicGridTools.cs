namespace EduGame.Problems.Tools;

/// <summary>Uma categoria de um problema "quem é quem" (ex.: animais, cores).</summary>
/// <param name="Has">Frase "tem o item", ex.: item => $"tem {item}" ou $"gosta de {item}".</param>
/// <param name="HasNot">Frase negativa, ex.: item => $"não tem {item}".</param>
public sealed record LogicCategory(string[] Items, Func<string, string> Has, Func<string, string> HasNot);

public abstract record LogicClue
{
    public abstract bool Holds(int[][] assignment);
    public abstract string Text(string[] people, LogicCategory[] categories);
}

/// <summary>Pessoa P tem (ou não tem) o item I da categoria C.</summary>
public sealed record PersonClue(int Person, int Category, int Item, bool Positive) : LogicClue
{
    public override bool Holds(int[][] a) => (a[Category][Person] == Item) == Positive;

    public override string Text(string[] people, LogicCategory[] cats) =>
        $"{people[Person]} {(Positive ? cats[Category].Has(cats[Category].Items[Item]) : cats[Category].HasNot(cats[Category].Items[Item]))}.";
}

/// <summary>Quem tem o item A (categoria CA) também tem (ou não tem) o item B (categoria CB).</summary>
public sealed record LinkClue(int CategoryA, int ItemA, int CategoryB, int ItemB, bool Positive) : LogicClue
{
    public override bool Holds(int[][] a)
    {
        var person = Array.IndexOf(a[CategoryA], ItemA);
        return (a[CategoryB][person] == ItemB) == Positive;
    }

    public override string Text(string[] people, LogicCategory[] cats)
    {
        var who = $"Quem {cats[CategoryA].Has(cats[CategoryA].Items[ItemA])}";
        var what = Positive ? cats[CategoryB].Has(cats[CategoryB].Items[ItemB]) : cats[CategoryB].HasNot(cats[CategoryB].Items[ItemB]);
        return $"{who} {what}.";
    }
}

/// <summary>
/// Gera problemas de lógica "quem é quem": cada pessoa tem exatamente um item de cada categoria.
/// Atribuição: assignment[categoria][pessoa] = índice do item.
/// </summary>
public static class LogicGridTools
{
    public sealed record Puzzle(int[][] Solution, IReadOnlyList<LogicClue> Clues);

    public static IEnumerable<int[]> Permutations(int n)
    {
        if (n == 1)
        {
            yield return [0];
            yield break;
        }
        foreach (var p in Permutations(n - 1))
            for (var i = 0; i <= p.Length; i++)
                yield return [.. p[..i], n - 1, .. p[i..]];
    }

    public static List<int[][]> AllAssignments(int people, int categories)
    {
        var perms = Permutations(people).ToList();
        var result = new List<int[][]> { Array.Empty<int[]>() };
        for (var c = 0; c < categories; c++)
            result = [.. result.SelectMany(partial => perms.Select(p => (int[][])[.. partial, p]))];
        return result;
    }

    /// <summary>
    /// Sorteia uma solução e acrescenta pistas verdadeiras até restar só uma atribuição possível
    /// para a pergunta. Pistas que entregam a resposta diretamente (<paramref name="forbidden"/>) não são usadas.
    /// </summary>
    public static Puzzle Generate(Random rng, int people, int categories, Func<LogicClue, bool> forbidden, Func<int[][], string> questionAnswer)
    {
        var all = AllAssignments(people, categories);
        var solution = all[rng.Next(all.Count)];

        var candidates = new List<LogicClue>();
        for (var c = 0; c < categories; c++)
        for (var p = 0; p < people; p++)
        for (var i = 0; i < people; i++)
            candidates.Add(new PersonClue(p, c, i, solution[c][p] == i));
        for (var ca = 0; ca < categories; ca++)
        for (var cb = ca + 1; cb < categories; cb++)
        for (var ia = 0; ia < people; ia++)
        for (var ib = 0; ib < people; ib++)
        {
            var person = Array.IndexOf(solution[ca], ia);
            candidates.Add(new LinkClue(ca, ia, cb, ib, solution[cb][person] == ib));
        }

        // Pistas negativas deixam o problema mais "de lógica"; positivas aparecem menos.
        var ordered = candidates
            .Where(c => !forbidden(c))
            .OrderBy(c => (c is PersonClue { Positive: true } or LinkClue { Positive: true } ? 1 : 0) + rng.NextDouble() * 1.2)
            .ToList();

        var clues = new List<LogicClue>();
        var remaining = all;
        foreach (var clue in ordered)
        {
            if (remaining.Select(questionAnswer).Distinct().Count() == 1) break;
            var filtered = remaining.Where(clue.Holds).ToList();
            if (filtered.Count < remaining.Count)
            {
                clues.Add(clue);
                remaining = filtered;
            }
        }
        return new Puzzle(solution, clues);
    }
}
