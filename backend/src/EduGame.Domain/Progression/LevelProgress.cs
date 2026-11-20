namespace EduGame.Domain.Progression;

public enum SkillStatus
{
    NotStarted,
    Practicing,
    Mastered,
    NeedsReview,
}

/// <summary>Situação do jogador num nível de um tópico.</summary>
/// <param name="Percent">0–100: avanço real no nível (média do avanço de cada habilidade rumo ao domínio).</param>
/// <param name="ReadyForNextLevel">Avanço suficiente para o Desafio do Guardião (<see cref="LevelProgress.GuardianThreshold"/>).</param>
public sealed record LevelProgressResult(
    LevelDefinition Current,
    LevelDefinition? Next,
    int Percent,
    int MasteredSkills,
    int TotalSkills,
    bool ReadyForNextLevel);

public static class LevelProgress
{
    /// <summary>Avanço do nível (a barra) a partir do qual o Desafio do Guardião abre.</summary>
    public const int GuardianThreshold = 85;

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

    public static LevelDefinition BySlug(string slug) => LevelDefinitions.All.Single(l => l.Slug == slug);

    /// <summary>Quanto uma habilidade já andou no nível: 0 = rating inicial, 1 = dominada.</summary>
    public static double SkillProgress(LevelDefinition level, int? rating)
    {
        var start = Rating.InitialFor(level);
        return Math.Clamp(((rating ?? start) - start) / (double)(TargetRating(level) - start), 0, 1);
    }

    /// <summary>
    /// Calcula o progresso no nível atual: a média do avanço de cada habilidade (cada uma vale no máximo 100%).
    /// A barra mostra exatamente esse número, e o Guardião abre quando ela chega a <see cref="GuardianThreshold"/>%.
    /// A prova do Guardião mistura todas as habilidades, então nenhuma pode ficar muito para trás.
    /// </summary>
    public static LevelProgressResult Compute(LevelDefinition current, IReadOnlyCollection<int?> skillRatings)
    {
        var next = NextOf(current);
        var total = skillRatings.Count;
        if (total == 0) return new(current, next, 0, 0, 0, false);

        var target = TargetRating(current);
        var mastered = skillRatings.Count(r => r >= target);
        var percent = (int)Math.Floor(skillRatings.Average(r => SkillProgress(current, r)) * 100);

        return new(current, next, percent, mastered, total, percent >= GuardianThreshold && next is not null);
    }
}
