using System.Reflection;
using System.Text.Json;

namespace EduGame.Problems;

/// <summary>Um problema escrito à mão (desafios abertos, quebra-cabeças clássicos).</summary>
public sealed record CuratedProblemDefinition(string Slug, string Skill, int Difficulty, ProblemContent Content);

/// <summary>
/// Problemas curados, guardados como JSON (recurso embutido) em Curated/*.json.
/// O seeder copia para o banco; os testes validam cada um.
/// </summary>
public static class CuratedCatalog
{
    private static readonly Lazy<IReadOnlyList<CuratedProblemDefinition>> Items = new(Load);

    public static IReadOnlyList<CuratedProblemDefinition> All => Items.Value;

    public static IEnumerable<CuratedProblemDefinition> ForSkill(string skillSlug) => All.Where(p => p.Skill == skillSlug);

    private static IReadOnlyList<CuratedProblemDefinition> Load()
    {
        var assembly = typeof(CuratedCatalog).Assembly;
        var list = new List<CuratedProblemDefinition>();
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".json", StringComparison.Ordinal)).Order())
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            list.AddRange(JsonSerializer.Deserialize<List<CuratedProblemDefinition>>(stream, JsonSerializerOptions.Web)!);
        }
        return list;
    }
}
