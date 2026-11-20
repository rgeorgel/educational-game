namespace EduGame.Problems.Tools;

/// <summary>Gera Sudokus (4×4, 6×6…) com solução única.</summary>
public static class SudokuTools
{
    /// <summary>Solução válida embaralhada. Blocos de <paramref name="boxRows"/>×<paramref name="boxCols"/>; tamanho = boxRows×boxCols.</summary>
    public static int[] Solution(Random rng, int boxRows, int boxCols)
    {
        var n = boxRows * boxCols;
        // Padrão base válido, depois embaralhado preservando as regras.
        int Pattern(int r, int c) => (boxCols * (r % boxRows) + r / boxRows + c) % n;

        var rows = Shuffle(rng, Enumerable.Range(0, n / boxRows))
            .SelectMany(band => Shuffle(rng, Enumerable.Range(0, boxRows)).Select(r => band * boxRows + r)).ToArray();
        var cols = Shuffle(rng, Enumerable.Range(0, n / boxCols))
            .SelectMany(stack => Shuffle(rng, Enumerable.Range(0, boxCols)).Select(c => stack * boxCols + c)).ToArray();
        var symbols = Shuffle(rng, Enumerable.Range(1, n)).ToArray();

        var grid = new int[n * n];
        for (var r = 0; r < n; r++)
        for (var c = 0; c < n; c++)
            grid[r * n + c] = symbols[Pattern(rows[r], cols[c])];
        return grid;
    }

    /// <summary>Remove células (em ordem aleatória) mantendo solução única, até <paramref name="blanks"/> lacunas.</summary>
    public static int?[] MakePuzzle(Random rng, int[] solution, int boxRows, int boxCols, int blanks)
    {
        var n = boxRows * boxCols;
        var cells = solution.ToArray();
        var removed = 0;
        foreach (var i in Shuffle(rng, Enumerable.Range(0, n * n)))
        {
            if (removed >= blanks) break;
            var keep = cells[i];
            cells[i] = 0;
            if (CountSolutions(cells, boxRows, boxCols, 2) == 1) removed++;
            else cells[i] = keep;
        }
        return [.. cells.Select(v => v == 0 ? (int?)null : v)];
    }

    /// <summary>Conta soluções (até <paramref name="limit"/>) por backtracking. 0 = vazio.</summary>
    public static int CountSolutions(int[] cells, int boxRows, int boxCols, int limit)
    {
        var n = boxRows * boxCols;
        var grid = cells.ToArray();
        var count = 0;
        Solve();
        return count;

        void Solve()
        {
            var i = Array.IndexOf(grid, 0);
            if (i < 0)
            {
                count++;
                return;
            }
            int r = i / n, c = i % n;
            for (var v = 1; v <= n && count < limit; v++)
            {
                if (!Allowed(r, c, v)) continue;
                grid[i] = v;
                Solve();
                grid[i] = 0;
            }
        }

        bool Allowed(int r, int c, int v)
        {
            for (var k = 0; k < n; k++)
                if (grid[r * n + k] == v || grid[k * n + c] == v) return false;
            int br = r / boxRows * boxRows, bc = c / boxCols * boxCols;
            for (var rr = br; rr < br + boxRows; rr++)
            for (var cc = bc; cc < bc + boxCols; cc++)
                if (grid[rr * n + cc] == v) return false;
            return true;
        }
    }

    private static IEnumerable<int> Shuffle(Random rng, IEnumerable<int> items) => items.OrderBy(_ => rng.Next()).ToArray();
}
