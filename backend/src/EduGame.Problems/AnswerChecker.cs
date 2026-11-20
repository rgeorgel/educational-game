using System.Globalization;

namespace EduGame.Problems;

public static class AnswerChecker
{
    public static bool IsCorrect(ProblemContent problem, string? given)
    {
        if (string.IsNullOrWhiteSpace(given)) return false;
        var answer = given.Trim();

        switch (problem.Type)
        {
            case ProblemTypes.Grade:
                return problem.Grid is { } grid && GridValidator.IsSolved(grid, answer);
            case ProblemTypes.Ordenar:
                return NormalizeList(answer) == NormalizeList(problem.Answer);
            case ProblemTypes.Numero:
            case ProblemTypes.FracaoVisual:
            case ProblemTypes.CompletarSequencia when problem.Choices is null:
                return TryParseNumber(answer, out var a)
                    && TryParseNumber(problem.Answer, out var expected)
                    && a == expected;
            default:
                return string.Equals(answer, problem.Answer, StringComparison.Ordinal);
        }
    }

    private static string NormalizeList(string text) =>
        string.Join('|', text.Split('|').Select(s => s.Trim()));

    /// <summary>Aceita "12", "12,5", "12.5", " 1 000 ", "−3" (sinal de menos tipográfico).</summary>
    public static bool TryParseNumber(string text, out decimal value)
    {
        var normalized = text
            .Replace(" ", "").Replace(" ", "")
            .Replace('−', '-').Replace('–', '-')
            .Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }

    public static string FormatNumber(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',').Replace("-", "−");
}

/// <summary>Valida uma grade preenchida pelas regras do quebra-cabeça (aceita qualquer solução válida).</summary>
public static class GridValidator
{
    public static int[]? Parse(GridPuzzle grid, string answer)
    {
        var parts = answer.Split(',');
        if (parts.Length != grid.Size * grid.Size) return null;
        var values = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i].Trim(), out values[i])) return null;
            if (values[i] < 1 || values[i] > grid.MaxValue) return null;
            if (grid.Cells[i] is { } given && given != values[i]) return null; // não pode mudar as pistas
        }
        return values;
    }

    public static bool IsSolved(GridPuzzle grid, string answer)
    {
        var v = Parse(grid, answer);
        if (v is null) return false;
        var n = grid.Size;
        int At(int r, int c) => v[r * n + c];

        return grid.Kind switch
        {
            GridKinds.Sudoku => SudokuOk(n, grid.BoxRows, grid.BoxCols, At),
            GridKinds.MagicSquare => MagicOk(n, grid.Target ?? 0, v, At),
            _ => false,
        };
    }

    private static bool SudokuOk(int n, int boxRows, int boxCols, Func<int, int, int> at)
    {
        for (var i = 0; i < n; i++)
        {
            if (Enumerable.Range(0, n).Select(c => at(i, c)).Distinct().Count() != n) return false;
            if (Enumerable.Range(0, n).Select(r => at(r, i)).Distinct().Count() != n) return false;
        }
        for (var br = 0; br < n; br += boxRows)
        for (var bc = 0; bc < n; bc += boxCols)
        {
            var box = from r in Enumerable.Range(br, boxRows) from c in Enumerable.Range(bc, boxCols) select at(r, c);
            if (box.Distinct().Count() != n) return false;
        }
        return true;
    }

    private static bool MagicOk(int n, int target, int[] values, Func<int, int, int> at)
    {
        if (values.Distinct().Count() != values.Length) return false;
        for (var i = 0; i < n; i++)
        {
            if (Enumerable.Range(0, n).Sum(c => at(i, c)) != target) return false;
            if (Enumerable.Range(0, n).Sum(r => at(r, i)) != target) return false;
        }
        return Enumerable.Range(0, n).Sum(i => at(i, i)) == target
            && Enumerable.Range(0, n).Sum(i => at(i, n - 1 - i)) == target;
    }
}
