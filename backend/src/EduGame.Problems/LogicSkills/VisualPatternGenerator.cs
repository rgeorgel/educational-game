using static EduGame.Problems.Lang;

namespace EduGame.Problems.LogicSkills;

/// <summary>Padrões visuais que se repetem (AB, ABC, AAB, ABB, AABB…).</summary>
public sealed class VisualPatternGenerator : ProblemGenerator
{
    public override string Key => "log.padroes";
    public override string SkillSlug => "padroes-visuais";

    private static readonly string[][] SymbolSets =
    [
        ["🔴", "🔵", "🟢", "🟡", "🟣"],
        ["⭐", "🌙", "☀️", "❤️", "🔷"],
        ["🐶", "🐱", "🐭", "🐰", "🐸"],
        ["🍎", "🍌", "🍇", "🍓", "🍊"],
    ];

    private static readonly (string Unit, int Difficulty)[] Patterns =
    [
        ("AB", 150), ("ABC", 330), ("AAB", 420), ("ABB", 450), ("AABB", 550),
        ("ABCD", 600), ("ABAC", 750), ("AABC", 800), ("ABBCC", 900), ("ABCCBA", 1000),
    ];

    public override GeneratedProblem Generate(Random rng)
    {
        var (unit, difficulty) = Pick(rng, Patterns);
        var symbols = Pick(rng, SymbolSets).OrderBy(_ => rng.Next()).ToArray();

        var letters = unit.Distinct().ToArray();
        string SymbolOf(char letter) => symbols[Array.IndexOf(letters, letter)];

        // Mostra pelo menos duas repetições completas antes da lacuna.
        var length = unit.Length * 2 + Between(rng, 1, unit.Length);
        var items = Enumerable.Range(0, length).Select(i => SymbolOf(unit[i % unit.Length])).ToList();

        var blankInMiddle = rng.Next(3) == 0;
        var blank = blankInMiddle ? Between(rng, unit.Length, length - 2) : length - 1;
        var answer = items[blank];
        var sequence = items.Select((s, i) => i == blank ? null : s).ToList();

        var used = letters.Select(SymbolOf).ToList();
        var extra = symbols.Except(used).FirstOrDefault();
        var unitText = string.Concat(unit.Select(SymbolOf));

        var content = new ProblemContent(
            ProblemTypes.CompletarSequencia,
            Tr("Qual figura completa o padrão?", "Which picture completes the pattern?", "¿Qué figura completa el patrón?"),
            answer,
            [
                Tr("Procure o pedaço que se repete.", "Look for the part that repeats.", "Busca el trozo que se repite."),
                Tr($"O pedaço que se repete é: {unitText}", $"The part that repeats is: {unitText}", $"El trozo que se repite es: {unitText}"),
            ],
            Tr($"O padrão repete {unitText}, então a figura que falta é {answer}.", $"The pattern repeats {unitText}, so the missing picture is {answer}.", $"El patrón repite {unitText}, así que la figura que falta es {answer}."))
        {
            Sequence = sequence,
            Choices = Choices(rng, answer, extra is null ? used : [.. used, extra], count: Math.Min(4, used.Count + 1)),
        };
        return new(content, difficulty + (blankInMiddle ? 100 : 0));
    }
}
