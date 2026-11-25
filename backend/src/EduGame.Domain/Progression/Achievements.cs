using EduGame.Problems;

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
    int HighestLevelOrder,
    int DailyChallenges = 0,
    int ReviewsCompleted = 0);

public sealed record AchievementDefinition(string Slug, Text NameText, Text DescriptionText, string Icon, Func<PlayerStats, bool> Earned)
{
    public string Name => NameText.Current;
    public string Description => DescriptionText.Current;
}

/// <summary>Medalhas: valorizam esforço e consistência, não só acerto.</summary>
public static class Achievements
{
    public static IReadOnlyList<AchievementDefinition> All { get; } =
    [
        new("primeiros-passos", new("Primeiros passos", "First steps", "Primeros pasos"), new("Resolveu o primeiro problema.", "Solved the first problem.", "Resolvió el primer problema."), "👣", s => s.ProblemsSolved >= 1),
        new("explorador", new("Explorador", "Explorer", "Explorador"), new("Jogou nas trilhas de Matemática e de Lógica.", "Played on both the Math and Logic trails.", "Jugó en los caminos de Matemáticas y de Lógica."), "🧭", s => s.SubjectsPlayed >= 2),
        new("tres-estrelas", new("Três estrelas", "Three stars", "Tres estrellas"), new("Ganhou 3 estrelas numa etapa.", "Earned 3 stars on a stage.", "Ganó 3 estrellas en una etapa."), "⭐", s => s.ThreeStarStages >= 1),
        new("sem-ajuda", new("Sem ajuda", "No help needed", "Sin ayuda"), new("Acertou todos os problemas de uma etapa sem pedir dicas.", "Got every problem in a stage right without asking for hints.", "Acertó todos los problemas de una etapa sin pedir pistas."), "🧠", s => s.PerfectSessionsWithoutHints >= 1),
        new("sequencia-3", new("Três dias de prática", "Three days of practice", "Tres días de práctica"), new("Praticou em 3 dias seguidos (pode faltar 1 dia no meio).", "Practiced 3 days in a row (skipping 1 day in between is OK).", "Practicó 3 días seguidos (puede faltar 1 día en medio)."), "🔥", s => s.Streak >= 3),
        new("certeiro", new("Certeiro", "Sharpshooter", "Certero"), new("Acertou 50 problemas.", "Got 50 problems right.", "Acertó 50 problemas."), "🎯", s => s.Correct >= 50),
        new("persistente", new("Persistente", "Persistent", "Persistente"), new("Completou 10 etapas.", "Completed 10 stages.", "Completó 10 etapas."), "🔁", s => s.SessionsCompleted >= 10),
        new("cem-problemas", new("Cem problemas", "One hundred problems", "Cien problemas"), new("Resolveu 100 problemas.", "Solved 100 problems.", "Resolvió 100 problemas."), "💯", s => s.ProblemsSolved >= 100),
        new("mestre", new("Mestre de uma habilidade", "Skill master", "Maestro de una habilidad"), new("Dominou uma habilidade.", "Mastered a skill.", "Dominó una habilidad."), "🎓", s => s.MasteredSkills >= 1),
        new("sequencia-7", new("Uma semana de prática", "A week of practice", "Una semana de práctica"), new("Praticou em 7 dias seguidos (pode faltar 1 dia no meio).", "Practiced 7 days in a row (skipping 1 day in between is OK).", "Practicó 7 días seguidos (puede faltar 1 día en medio)."), "📅", s => s.Streak >= 7),
        new("colecionador", new("Colecionador de estrelas", "Star collector", "Coleccionista de estrellas"), new("Ganhou 3 estrelas em 10 etapas.", "Earned 3 stars on 10 stages.", "Ganó 3 estrellas en 10 etapas."), "🌟", s => s.ThreeStarStages >= 10),
        new("nivel-medio", new("Chegou ao Médio", "Reached Intermediate", "Llegó a Intermedio"), new("Alcançou o nível Médio em um tópico.", "Reached the Intermediate level in a subject.", "Alcanzó el nivel Intermedio en un tema."), "🌿", s => s.HighestLevelOrder >= 2),
        new("maratonista", new("Maratonista", "Marathoner", "Maratonista"), new("Resolveu 500 problemas.", "Solved 500 problems.", "Resolvió 500 problemas."), "🏃", s => s.ProblemsSolved >= 500),
        new("nivel-avancado", new("Chegou ao Avançado", "Reached Advanced", "Llegó a Avanzado"), new("Alcançou o nível Avançado em um tópico.", "Reached the Advanced level in a subject.", "Alcanzó el nivel Avanzado en un tema."), "🌳", s => s.HighestLevelOrder >= 3),
        new("nivel-expert", new("Chegou ao Expert", "Reached Expert", "Llegó a Experto"), new("Alcançou o nível Expert em um tópico.", "Reached the Expert level in a subject.", "Alcanzó el nivel Experto en un tema."), "🏔️", s => s.HighestLevelOrder >= 4),
        new("desafiante", new("Desafiante", "Challenger", "Desafiante"), new("Completou 5 desafios diários.", "Completed 5 daily challenges.", "Completó 5 desafíos diarios."), "📆", s => s.DailyChallenges >= 5),
        new("revisor", new("Boa memória", "Good memory", "Buena memoria"), new("Completou 3 revisões.", "Completed 3 reviews.", "Completó 3 repasos."), "🧩", s => s.ReviewsCompleted >= 3),
    ];

    public static IEnumerable<AchievementDefinition> EarnedBy(PlayerStats stats) => All.Where(a => a.Earned(stats));
}
