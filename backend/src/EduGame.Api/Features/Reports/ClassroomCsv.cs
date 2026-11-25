using System.Globalization;
using System.Text;
using static EduGame.Problems.Lang;

namespace EduGame.Api.Features.Reports;

/// <summary>
/// Planilhas da turma em CSV para Excel/Google Planilhas, no idioma da requisição:
/// separador ";" (português e espanhol) ou "," (inglês), UTF-8 com BOM (acentos certos no Excel)
/// e proteção contra fórmulas em células.
/// </summary>
public static class ClassroomCsv
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>O Excel em inglês separa colunas por vírgula; em português e espanhol, por ponto e vírgula.</summary>
    private static char Separator => Current == En ? ',' : ';';

    public static byte[] Students(IReadOnlyList<ProfileReport> students, IReadOnlyList<AssignmentDto> assignments)
    {
        var subjects = students.SelectMany(s => s.Subjects).Select(s => s.Name).Distinct().ToList();
        var header = new List<string>
        {
            Tr("Apelido", "Nickname", "Apodo"),
            Tr("Último dia jogado", "Last day played", "Último día jugado"),
            Tr("Dias seguidos", "Day streak", "Días seguidos"),
            Tr("Problemas (7 dias)", "Problems (7 days)", "Problemas (7 días)"),
            Tr("Acertos (7 dias)", "Correct (7 days)", "Aciertos (7 días)"),
            Tr("Problemas (total)", "Problems (total)", "Problemas (total)"),
            Tr("Acertos (total)", "Correct (total)", "Aciertos (total)"),
        };
        foreach (var subject in subjects)
            header.AddRange([Tr($"{subject}: nível", $"{subject}: level", $"{subject}: nivel"), Tr($"{subject}: % para o próximo nível", $"{subject}: % to next level", $"{subject}: % para el siguiente nivel")]);
        header.AddRange([Tr("Tarefas feitas", "Assignments done", "Tareas hechas"), Tr("Pede atenção", "Needs attention", "Necesita atención"), Tr("BNCC (pede atenção)", "BNCC (needs attention)", "BNCC (necesita atención)")]);

        var rows = students.Select(s =>
        {
            var row = new List<string>
            {
                s.Nickname,
                s.LastActive?.ToString(Current == En ? "MM/dd/yyyy" : "dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "",
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
                assignments.Count == 0 ? "" : Tr($"{done} de {assignments.Count}", $"{done} of {assignments.Count}", $"{done} de {assignments.Count}"),
                string.Join(", ", attention.Select(a => a.Name)),
                string.Join(", ", attention.SelectMany(a => a.Bncc).Distinct()),
            ]);
            return row;
        });
        return Build(header, rows);
    }

    public static byte[] Skills(IReadOnlyList<SkillClassDto> skills)
    {
        string[] header =
        [
            Tr("Matéria", "Subject", "Materia"), Tr("Nível", "Level", "Nivel"), Tr("Habilidade", "Skill", "Habilidad"), "BNCC",
            Tr("Alunos que praticaram", "Students who practiced", "Alumnos que practicaron"), Tr("Dominam", "Mastered", "Dominan"),
            Tr("Praticando", "Practicing", "Practicando"), Tr("Pedem revisão", "Need review", "Necesitan repaso"),
            Tr("Respostas", "Answers", "Respuestas"), Tr("Acertos", "Correct", "Aciertos"),
        ];
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
        sb.AppendLine(string.Join(Separator, header.Select(Cell)));
        foreach (var row in rows) sb.AppendLine(string.Join(Separator, row.Select(Cell)));
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(sb.ToString())];
    }

    /// <summary>Escapa aspas/separadores e neutraliza textos que o Excel trataria como fórmula.</summary>
    public static string Cell(string value)
    {
        value ??= "";
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]) && !IsNumberLike(value)) value = "'" + value;
        return value.IndexOfAny([Separator, '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
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
