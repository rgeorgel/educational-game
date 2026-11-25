using System.Globalization;

namespace EduGame.Problems;

/// <summary>
/// Idioma da requisição atual: português do Brasil ("pt", o padrão), inglês ("en") ou espanhol ("es").
/// A API define o idioma no começo de cada requisição (cabeçalho X-Lang, ver Program.cs) e o valor
/// acompanha o código assíncrono até o fim. Sem definição (testes, tarefas em segundo plano), vale o português.
/// </summary>
public static class Lang
{
    public const string Pt = "pt";
    public const string En = "en";
    public const string Es = "es";

    public static IReadOnlyList<string> All { get; } = [Pt, En, Es];

    private static readonly AsyncLocal<string?> CurrentValue = new();

    public static string Current => CurrentValue.Value ?? Pt;

    /// <summary>Define o idioma do fluxo atual (vale para o código chamado a partir daqui).</summary>
    public static void Set(string? lang) => CurrentValue.Value = Normalize(lang);

    /// <summary>Usa outro idioma até o <c>Dispose</c> (ex.: <c>using var _ = Lang.Use("en");</c>).</summary>
    public static IDisposable Use(string? lang)
    {
        var previous = CurrentValue.Value;
        CurrentValue.Value = Normalize(lang);
        return new Restore(previous);
    }

    /// <summary>"en-US" → "en", "es-419" → "es"; qualquer outro valor → "pt".</summary>
    public static string Normalize(string? tag)
    {
        var t = tag?.Trim().ToLowerInvariant() ?? "";
        if (t.StartsWith(En, StringComparison.Ordinal)) return En;
        if (t.StartsWith(Es, StringComparison.Ordinal)) return Es;
        return Pt;
    }

    /// <summary>Escolhe o texto do idioma atual.</summary>
    public static string Tr(string pt, string en, string es) => Current switch
    {
        En => en,
        Es => es,
        _ => pt,
    };

    /// <summary>Código para o atributo lang do HTML (e-mails).</summary>
    public static string HtmlTag => Current switch { En => "en", Es => "es", _ => "pt-BR" };

    /// <summary>Cultura para formatar datas e números no idioma atual.</summary>
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(Current switch { En => "en-US", Es => "es-ES", _ => "pt-BR" });

    /// <summary>Separador decimal do idioma atual (inglês usa ponto; português e espanhol, vírgula).</summary>
    public static char DecimalSeparator => Current == En ? '.' : ',';

    private sealed class Restore(string? previous) : IDisposable
    {
        public void Dispose() => CurrentValue.Value = previous;
    }
}

/// <summary>Um texto nos três idiomas. Vira <c>string</c> no idioma atual.</summary>
public sealed record Text(string Pt, string En, string Es)
{
    public string Current => Lang.Tr(Pt, En, Es);

    public string In(string lang) => Lang.Normalize(lang) switch
    {
        Lang.En => En,
        Lang.Es => Es,
        _ => Pt,
    };

    public override string ToString() => Current;

    public static implicit operator string(Text text) => text.Current;
}
