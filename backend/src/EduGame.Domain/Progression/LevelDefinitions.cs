namespace EduGame.Domain.Progression;

public sealed record LevelDefinition(string Slug, string Name, string Icon, int Order, int MinRating);

/// <summary>Os quatro níveis do jogo e suas faixas de rating.</summary>
public static class LevelDefinitions
{
    public static readonly LevelDefinition Iniciante = new("iniciante", "Iniciante", "🌱", 1, 0);
    public static readonly LevelDefinition Medio = new("medio", "Médio", "🌿", 2, 1000);
    public static readonly LevelDefinition Avancado = new("avancado", "Avançado", "🌳", 3, 1500);
    public static readonly LevelDefinition Expert = new("expert", "Expert", "🏔️", 4, 2000);

    public static IReadOnlyList<LevelDefinition> All { get; } = [Iniciante, Medio, Avancado, Expert];
}
