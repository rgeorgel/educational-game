using System.Globalization;
using System.Text;

namespace EduGame.Api.Features.Reports;

/// <summary>
/// Planilhas da turma em CSV para Excel/Google Planilhas em português:
/// separador ";", UTF-8 com BOM (acentos certos no Excel) e proteção contra fórmulas em células.
/// </summary>
public static class ClassroomCsv
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static byte[] Students(IReadOnlyList<ProfileReport> students, IReadOnlyList<AssignmentDto> assignments)
    {
        var subjects = students.SelectMany(s => s.Subjects).Select(s => s.Name).Distinct().ToList();
        var header = new List<string> { "Apelido", "Último dia jogado", "Dias seguidos", "Problemas (7 dias)", "Acertos (7 dias)", "Problemas (total)", "Acertos (total)" };
        foreach (var subject in subjects) header.AddRange([$"{subject}: nível", $"{subject}: % para o próximo nível"]);
        header.AddRange(["Tarefas feitas", "Pede atenção", "BNCC (pede atenção)"]);

        var rows = students.Select(s =>
        {
            var row = new List<string>
            {
                s.Nickname,
                s.LastActive?.ToString("dd/MM/yyyy") ?? "",
                s.Streak.ToString(PtBr),
                s.LastSevenDays.Problems.ToString(PtBr),
                Percent(s.LastSevenDays.Correct, s.LastSevenDays.Problems),
                s.TotalProblems.ToString(PtBr),
                s.TotalProblems == 0 ? "" : $"{s.Accuracy}%",
            };
            foreach (var subject in subjects)
            {
                var sub = s.Subjects.FirstOrDefault(x => x.Name == subject);
                row.AddRange([sub?.Level.Name ?? "", sub is null ? "" : $"{sub.Percent}%"]);
            }
            var done = assignments.Count(a => a.Students.Any(x => x.ProfileId == s.Id && x.Status == "feita"));
            var attention = s.Subjects.SelectMany(x => x.AttentionSkills).ToList();
            row.AddRange([
                assignments.Count == 0 ? "" : $"{done} de {assignments.Count}",
                string.Join(", ", attention.Select(a => a.Name)),
                string.Join(", ", attention.SelectMany(a => a.Bncc).Distinct()),
            ]);
            return row;
        });
        return Build(header, rows);
    }

    public static byte[] Skills(IReadOnlyList<SkillClassDto> skills)
    {
        string[] header = ["Matéria", "Nível", "Habilidade", "BNCC", "Alunos que praticaram", "Dominam", "Praticando", "Pedem revisão", "Respostas", "Acertos"];
        var rows = skills.Select(s => (IReadOnlyList<string>)
        [
            s.SubjectName, s.LevelName, s.Name, string.Join(", ", s.Bncc),
            s.Practiced.ToString(PtBr), s.Mastered.ToString(PtBr), s.Practicing.ToString(PtBr), s.NeedsReview.ToString(PtBr),
            s.Attempts.ToString(PtBr), Percent(s.Correct, s.Attempts),
        ]);
        return Build(header, rows);
    }

    private static string Percent(int part, int total) => total == 0 ? "" : $"{Math.Round(100.0 * part / total)}%";

    private static byte[] Build(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', header.Select(Cell)));
        foreach (var row in rows) sb.AppendLine(string.Join(';', row.Select(Cell)));
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(sb.ToString())];
    }

    /// <summary>Escapa aspas/separadores e neutraliza textos que o Excel trataria como fórmula.</summary>
    public static string Cell(string value)
    {
        value ??= "";
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]) && !IsNumberLike(value)) value = "'" + value;
        return value.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    private static bool IsNumberLike(string value) => decimal.TryParse(value.TrimEnd('%'), NumberStyles.Number, PtBr, out _);

    /// <summary>Nome de arquivo seguro a partir do nome da turma.</summary>
    public static string Slug(string name)
    {
        var plain = new string(name.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
        var slug = new string(plain.Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Length == 0 ? "turma" : slug[..Math.Min(40, slug.Length)];
    }
}
