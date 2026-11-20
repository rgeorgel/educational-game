namespace EduGame.Domain.Progression;

/// <summary>Regras de XP e estrelas.</summary>
public static class Rewards
{
    /// <summary>XP por tentar, mesmo errando: o esforço também conta.</summary>
    public const int EffortXp = 1;

    public static int Xp(bool correct, int difficulty, int hintsUsed)
    {
        if (!correct) return EffortXp;

        var baseXp = 10 + Math.Max(0, difficulty - Rating.Initial) / 50;
        var multiplier = hintsUsed switch
        {
            0 => 1.0,
            1 => 0.7,
            2 => 0.5,
            _ => 0.3,
        };
        return Math.Max(EffortXp + 1, (int)Math.Round(baseXp * multiplier));
    }

    /// <summary>Estrelas de uma etapa: 1 com ≥60%, 2 com ≥80%, 3 com 100% de acertos.</summary>
    public static int Stars(int correct, int total)
    {
        if (total <= 0) return 0;
        var ratio = (double)correct / total;
        return ratio switch
        {
            >= 1.0 => 3,
            >= 0.8 => 2,
            >= 0.6 => 1,
            _ => 0,
        };
    }
}
