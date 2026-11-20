namespace EduGame.Domain.Progression;

/// <summary>Números do jogador usados para conceder medalhas.</summary>
public sealed record PlayerStats(
    int ProblemsSolved,
    int Correct,
    int SessionsCompleted,
    int ThreeStarStages,
    int PerfectSessionsWithoutHints,
    int Streak,
    int MasteredSkills,
    int SubjectsPlayed,
    int HighestLevelOrder);

public sealed record AchievementDefinition(string Slug, string Name, string Description, string Icon, Func<PlayerStats, bool> Earned);

/// <summary>Medalhas: valorizam esforço e consistência, não só acerto.</summary>
public static class Achievements
{
    public static IReadOnlyList<AchievementDefinition> All { get; } =
    [
        new("primeiros-passos", "Primeiros passos", "Resolveu o primeiro problema.", "👣", s => s.ProblemsSolved >= 1),
        new("explorador", "Explorador", "Jogou nas trilhas de Matemática e de Lógica.", "🧭", s => s.SubjectsPlayed >= 2),
        new("tres-estrelas", "Três estrelas", "Ganhou 3 estrelas numa etapa.", "⭐", s => s.ThreeStarStages >= 1),
        new("sem-ajuda", "Sem ajuda", "Acertou todos os problemas de uma etapa sem pedir dicas.", "🧠", s => s.PerfectSessionsWithoutHints >= 1),
        new("sequencia-3", "Três dias de prática", "Praticou em 3 dias seguidos (pode faltar 1 dia no meio).", "🔥", s => s.Streak >= 3),
        new("certeiro", "Certeiro", "Acertou 50 problemas.", "🎯", s => s.Correct >= 50),
        new("persistente", "Persistente", "Completou 10 etapas.", "🔁", s => s.SessionsCompleted >= 10),
        new("cem-problemas", "Cem problemas", "Resolveu 100 problemas.", "💯", s => s.ProblemsSolved >= 100),
        new("mestre", "Mestre de uma habilidade", "Dominou uma habilidade.", "🎓", s => s.MasteredSkills >= 1),
        new("sequencia-7", "Uma semana de prática", "Praticou em 7 dias seguidos (pode faltar 1 dia no meio).", "📅", s => s.Streak >= 7),
        new("colecionador", "Colecionador de estrelas", "Ganhou 3 estrelas em 10 etapas.", "🌟", s => s.ThreeStarStages >= 10),
        new("nivel-medio", "Chegou ao Médio", "Alcançou o nível Médio em um tópico.", "🌿", s => s.HighestLevelOrder >= 2),
        new("maratonista", "Maratonista", "Resolveu 500 problemas.", "🏃", s => s.ProblemsSolved >= 500),
        new("nivel-avancado", "Chegou ao Avançado", "Alcançou o nível Avançado em um tópico.", "🌳", s => s.HighestLevelOrder >= 3),
        new("nivel-expert", "Chegou ao Expert", "Alcançou o nível Expert em um tópico.", "🏔️", s => s.HighestLevelOrder >= 4),
    ];

    public static IEnumerable<AchievementDefinition> EarnedBy(PlayerStats stats) => All.Where(a => a.Earned(stats));
}
