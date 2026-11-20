namespace EduGame.Domain.Progression;

/// <summary>
/// Desafio diário: um problema especial por dia, por tópico e nível — o mesmo para todos os jogadores
/// daquele nível (a semente vem da data). Vale XP em dobro e pode ser feito uma vez por dia.
/// </summary>
public static class DailyChallenge
{
    public const int XpMultiplier = 2;

    /// <summary>Semente estável (FNV-1a): não usar string.GetHashCode, que muda a cada execução.</summary>
    public static int Seed(DateOnly date, string subjectSlug, string levelSlug)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in $"{date:yyyy-MM-dd}|{subjectSlug}|{levelSlug}")
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    /// <summary>Índice determinístico (0..count-1) para escolher a habilidade do dia.</summary>
    public static int Pick(int seed, int count) => count <= 0 ? 0 : seed % count;

    /// <summary>Dificuldade-alvo: um pouco acima do meio da faixa do nível — é um desafio!</summary>
    public static int TargetDifficulty(LevelDefinition level) =>
        (Rating.InitialFor(level) + LevelProgress.TargetRating(level)) / 2 + 100;
}

/// <summary>Revisão inteligente: escolhe as habilidades que mais precisam de prática.</summary>
public static class Review
{
    public const int ProblemCount = 8;
    public const int MaxSkills = 4;

    public sealed record Candidate(int SkillId, LevelDefinition Level, int Rating, int Attempts, DateTimeOffset? LastPracticed);

    /// <summary>
    /// Prioridade: habilidades que pedem revisão (dominadas há muito tempo sem prática),
    /// depois as com menor avanço dentro do próprio nível. Só entram habilidades já praticadas.
    /// </summary>
    public static IReadOnlyList<int> PickSkills(IEnumerable<Candidate> candidates, DateTimeOffset now) =>
        [.. candidates
            .Where(c => c.Attempts > 0)
            .Select(c => new
            {
                c.SkillId,
                NeedsReview = LevelProgress.StatusOf(c.Rating, c.Attempts, c.LastPracticed, c.Level, now) == SkillStatus.NeedsReview,
                Progress = (double)(c.Rating - Rating.InitialFor(c.Level)) / (LevelProgress.TargetRating(c.Level) - Rating.InitialFor(c.Level)),
                c.LastPracticed,
            })
            .OrderByDescending(x => x.NeedsReview)
            .ThenBy(x => Math.Min(1, x.Progress))
            .ThenBy(x => x.LastPracticed)
            .Take(MaxSkills)
            .Select(x => x.SkillId)];
}

public sealed record AvatarItem(string Slug, string Name, string Value, int XpRequired);

/// <summary>Itens de avatar desbloqueados com XP (sem compras).</summary>
public static class AvatarItems
{
    public static IReadOnlyList<AvatarItem> Accessories { get; } =
    [
        new("nenhum", "Nenhum", "", 0),
        new("laco", "Laço", "🎀", 50),
        new("bone", "Boné", "🧢", 150),
        new("oculos", "Óculos escuros", "🕶️", 300),
        new("cartola", "Cartola", "🎩", 600),
        new("fones", "Fones de ouvido", "🎧", 1000),
        new("medalha", "Medalha", "🏅", 1500),
        new("coroa", "Coroa", "👑", 2500),
    ];

    /// <summary>Cores de fundo do avatar. O valor é um nome de token de cor do front.</summary>
    public static IReadOnlyList<AvatarItem> Colors { get; } =
    [
        new("padrao", "Padrão", "padrao", 0),
        new("menta", "Menta", "menta", 100),
        new("lavanda", "Lavanda", "lavanda", 250),
        new("pessego", "Pêssego", "pessego", 500),
        new("ceu", "Céu", "ceu", 800),
        new("ouro", "Ouro", "ouro", 2000),
    ];

    public static bool IsUnlocked(IReadOnlyList<AvatarItem> list, string slug, int xp) =>
        list.Any(i => i.Slug == slug && i.XpRequired <= xp);
}
