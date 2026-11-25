using EduGame.Domain.Progression;
using EduGame.Problems;

namespace EduGame.Infrastructure.Seeding;

/// <summary>
/// Traduz os textos do catálogo guardados no banco (sempre em português) para o idioma da requisição.
/// O <see cref="AppDbContext"/> aplica isto na leitura das colunas de nome e descrição, então toda consulta
/// já devolve o texto traduzido sem mudar o código que a usa. Texto desconhecido volta como está.
/// </summary>
public static class CatalogText
{
    private static readonly Lazy<Dictionary<string, Text>> ByPortuguese = new(Build);

    public static string Translate(string portuguese)
    {
        if (Lang.Current == Lang.Pt || string.IsNullOrEmpty(portuguese)) return portuguese;
        return ByPortuguese.Value.TryGetValue(portuguese, out var text) ? text.Current : portuguese;
    }

    private static Dictionary<string, Text> Build()
    {
        var all = new Dictionary<string, Text>(StringComparer.Ordinal);
        void Add(Text t) => all.TryAdd(t.Pt, t);

        foreach (var level in LevelDefinitions.All) Add(level.Label);
        foreach (var subject in ContentCatalog.Subjects)
        {
            Add(subject.Name);
            foreach (var skill in subject.Skills) { Add(skill.Name); Add(skill.Description); }
            foreach (var region in subject.Regions)
            {
                Add(region.Name);
                Add(region.Description);
                foreach (var stage in region.Stages) Add(stage.Name);
            }
        }
        foreach (var bncc in BnccCatalog.Habilidades) { Add(bncc.Summary); Add(bncc.Component); Add(bncc.Grade); }
        return all;
    }
}
