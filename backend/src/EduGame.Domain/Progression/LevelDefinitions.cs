using EduGame.Problems;

namespace EduGame.Domain.Progression;

public sealed record LevelDefinition(string Slug, Text Label, string Icon, int Order, int MinRating)
{
    /// <summary>Nome no idioma atual.</summary>
    public string Name => Label.Current;
}

/// <summary>Os quatro níveis do jogo e suas faixas de rating.</summary>
public static class LevelDefinitions
{
    public static readonly LevelDefinition Iniciante = new("iniciante", new("Iniciante", "Beginner", "Principiante"), "🌱", 1, 0);
    public static readonly LevelDefinition Medio = new("medio", new("Médio", "Intermediate", "Intermedio"), "🌿", 2, 1000);
    public static readonly LevelDefinition Avancado = new("avancado", new("Avançado", "Advanced", "Avanzado"), "🌳", 3, 1500);
    public static readonly LevelDefinition Expert = new("expert", new("Expert", "Expert", "Experto"), "🏔️", 4, 2000);

    public static IReadOnlyList<LevelDefinition> All { get; } = [Iniciante, Medio, Avancado, Expert];
}
