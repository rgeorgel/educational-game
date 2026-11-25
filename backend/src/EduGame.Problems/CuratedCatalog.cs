using System.Reflection;
using System.Text.Json;

namespace EduGame.Problems;

/// <summary>Um problema escrito à mão (desafios abertos, quebra-cabeças clássicos).</summary>
public sealed record CuratedProblemDefinition(string Slug, string Skill, int Difficulty, ProblemContent Content);

/// <summary>Tradução de um problema curado (mesmo slug, conteúdo em outro idioma).</summary>
public sealed record CuratedTranslation(string Slug, ProblemContent Content);

/// <summary>
/// Problemas curados, guardados como JSON (recurso embutido) em Curated/*.json, em português.
/// As traduções ficam em Curated/i18n/*.{en,es}.json, pelo slug.
/// O seeder copia o português para o banco; a sessão troca pela tradução no idioma da requisição.
/// Os testes validam cada um, nos três idiomas.
/// </summary>
public static class CuratedCatalog
{
    private const string TranslationsFolder = ".Curated.i18n.";

    private static readonly Lazy<IReadOnlyList<CuratedProblemDefinition>> Items = new(Load);
    private static readonly Lazy<Dictionary<string, Dictionary<string, ProblemContent>>> Translations = new(LoadTranslations);

    public static IReadOnlyList<CuratedProblemDefinition> All => Items.Value;

    public static IEnumerable<CuratedProblemDefinition> ForSkill(string skillSlug) => All.Where(p => p.Skill == skillSlug);

    /// <summary>Conteúdo do problema no idioma pedido (ou no atual); <c>null</c> = sem tradução, use o português.</summary>
    public static ProblemContent? Translated(string slug, string? lang = null)
    {
        var l = Lang.Normalize(lang ?? Lang.Current);
        if (l == Lang.Pt) return null;
        return Translations.Value.TryGetValue(l, out var bySlug) && bySlug.TryGetValue(slug, out var content) ? content : null;
    }

    private static Dictionary<string, Dictionary<string, ProblemContent>> LoadTranslations()
    {
        var assembly = typeof(CuratedCatalog).Assembly;
        var result = new Dictionary<string, Dictionary<string, ProblemContent>>();
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.Contains(TranslationsFolder, StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal)))
        {
            // "...desafios-olimpicos.en.json" → "en"
            var lang = name[..^".json".Length].Split('.')[^1];
            using var stream = assembly.GetManifestResourceStream(name)!;
            var items = JsonSerializer.Deserialize<List<CuratedTranslation>>(stream, JsonSerializerOptions.Web)!;
            if (!result.TryGetValue(lang, out var bySlug)) result[lang] = bySlug = [];
            foreach (var item in items) bySlug[item.Slug] = item.Content;
        }
        return result;
    }

    private static IReadOnlyList<CuratedProblemDefinition> Load()
    {
        var assembly = typeof(CuratedCatalog).Assembly;
        var list = new List<CuratedProblemDefinition>();
        foreach (var name in assembly.GetManifestResourceNames()
                     .Where(n => n.EndsWith(".json", StringComparison.Ordinal) && !n.Contains(TranslationsFolder, StringComparison.Ordinal)).Order())
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            list.AddRange(JsonSerializer.Deserialize<List<CuratedProblemDefinition>>(stream, JsonSerializerOptions.Web)!);
        }
        return list;
    }
}
