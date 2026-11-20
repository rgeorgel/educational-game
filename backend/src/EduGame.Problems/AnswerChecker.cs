using System.Globalization;

namespace EduGame.Problems;

public static class AnswerChecker
{
    public static bool IsCorrect(ProblemContent problem, string? given)
    {
        if (string.IsNullOrWhiteSpace(given)) return false;
        var answer = given.Trim();

        var numeric = problem.Type == ProblemTypes.Numero
            || (problem.Type == ProblemTypes.CompletarSequencia && problem.Choices is null);

        if (numeric)
        {
            return TryParseNumber(answer, out var a)
                && TryParseNumber(problem.Answer, out var expected)
                && a == expected;
        }

        return string.Equals(answer, problem.Answer, StringComparison.Ordinal);
    }

    /// <summary>Aceita "12", "12,5", "12.5", " 1 000 " (espaços como separador de milhar).</summary>
    public static bool TryParseNumber(string text, out decimal value)
    {
        var normalized = text.Replace(" ", "").Replace(" ", "").Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }
}
