namespace EduGame.Domain.Progression;

public enum SkillStatus
{
    NotStarted,
    Practicing,
    Mastered,
    NeedsReview,
}

/// <summary>Situação do jogador num nível de um tópico.</summary>
/// <param name="Percent">0–100: quanto falta para o próximo nível.</param>
/// <param name="ReadyForNextLevel">Domínio suficiente para o Desafio do Guardião.</param>
public sealed record LevelProgressResult(
    LevelDefinition Current,
    LevelDefinition? Next,
    int Percent,
    int MasteredSkills,
    int TotalSkills,
    bool ReadyForNextLevel);

public static class LevelProgress
{
    /// <summary>Fração das habilidades do nível que precisa estar dominada para subir.</summary>
    public const double MasteryShareToAdvance = 0.8;

    /// <summary>Quantos dias sem praticar até uma habilidade dominada pedir revisão.</summary>
    public const int ReviewAfterDays = 21;

    public static SkillStatus StatusOf(int? rating, int attempts, DateTimeOffset? lastPracticed, LevelDefinition level, DateTimeOffset now)
    {
        if (rating is null || attempts == 0) return SkillStatus.NotStarted;

        var target = TargetRating(level);
        if (rating >= target)
        {
            return lastPracticed is { } last && (now - last).TotalDays > ReviewAfterDays
                ? SkillStatus.NeedsReview
                : SkillStatus.Mastered;
        }
        return SkillStatus.Practicing;
    }

    /// <summary>Rating que conta como "dominada" para uma habilidade deste nível (= início do próximo nível).</summary>
    public static int TargetRating(LevelDefinition level)
    {
        var next = NextOf(level);
        return next?.MinRating ?? level.MinRating + 500;
    }

    public static LevelDefinition? NextOf(LevelDefinition level) =>
        LevelDefinitions.All.FirstOrDefault(l => l.Order == level.Order + 1);

    /// <summary>
    /// Calcula o progresso dentro do nível atual a partir dos ratings das habilidades desse nível.
    /// Cada habilidade contribui com quanto já andou entre o rating inicial e o rating-alvo.
    /// </summary>
    public static LevelProgressResult Compute(LevelDefinition current, IReadOnlyCollection<int?> skillRatings)
    {
        var next = NextOf(current);
        var total = skillRatings.Count;
        if (total == 0) return new(current, next, 0, 0, 0, false);

        var start = Math.Max(current.MinRating, current.Order == 1 ? Rating.Initial : current.MinRating);
        var target = TargetRating(current);

        var mastered = skillRatings.Count(r => r >= target);
        var needed = (int)Math.Ceiling(total * MasteryShareToAdvance);

        // Média do avanço de cada habilidade, limitada a 100% por habilidade.
        var avg = skillRatings
            .Select(r => Math.Clamp(((r ?? start) - start) / (double)(target - start), 0, 1))
            .Average();

        // O progresso só chega a 100% quando o critério de domínio é atingido.
        var ready = mastered >= needed;
        var percent = ready ? 100 : Math.Min(99, (int)Math.Floor(avg * 100 / MasteryShareToAdvance));

        return new(current, next, percent, mastered, total, ready && next is not null);
    }
}
