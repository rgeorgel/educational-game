namespace EduGame.Domain.Progression;

/// <summary>
/// Desafio do Guardião: prova de fim de nível com problemas de todas as habilidades do nível.
/// Passar promove o jogador ao próximo nível do tópico.
/// </summary>
public static class Guardian
{
    public const int ProblemCount = 12;

    /// <summary>Fração mínima de acertos para passar (9 de 12).</summary>
    public const double PassShare = 0.75;

    public static bool Passed(int correct, int total) => total > 0 && (double)correct / total >= PassShare;

    /// <summary>Dificuldade-alvo: perto do rating que caracteriza o domínio do nível.</summary>
    public static int TargetDifficulty(LevelDefinition level, Random rng) =>
        LevelProgress.TargetRating(level) - 150 + rng.Next(-100, 101);
}

/// <summary>
/// Teste de nivelamento: uma busca adaptativa. Começa num palpite (pela série escolar),
/// sobe quando o jogador acerta e desce quando erra, com passos cada vez menores.
/// </summary>
public static class Placement
{
    public const int ProblemCount = 10;

    public static int StartRating(int? schoolGrade) => schoolGrade switch
    {
        null => 450,
        <= 2 => 300,
        3 => 500,
        <= 5 => 800,
        <= 7 => 1200,
        <= 9 => 1550,
        _ => 1850,
    };

    /// <summary>Tamanho do passo na pergunta <paramref name="index"/> (0, 1, 2…).</summary>
    public static int Step(int index) => Math.Max(80, (int)(260 * Math.Pow(0.82, index)));

    public static int Next(int estimate, bool correct, int index) =>
        Math.Clamp(estimate + (correct ? Step(index) : -Step(index)), Rating.Min, Rating.Max);

    /// <summary>Nível em que a estimativa cabe, limitado aos níveis que já têm conteúdo.</summary>
    public static LevelDefinition LevelFor(int estimate, int maxLevelOrder)
    {
        var result = LevelDefinitions.Iniciante;
        foreach (var level in LevelDefinitions.All.Where(l => l.Order <= maxLevelOrder))
            if (estimate >= level.MinRating + 100) result = level;
        return result;
    }

    /// <summary>Nível em que um problema de nivelamento deve ser sorteado, dada a estimativa atual.</summary>
    public static LevelDefinition LevelToAsk(int estimate, int maxLevelOrder)
    {
        var result = LevelDefinitions.Iniciante;
        foreach (var level in LevelDefinitions.All.Where(l => l.Order <= maxLevelOrder))
            if (estimate >= level.MinRating) result = level;
        return result;
    }
}

/// <summary>Sequência de dias de prática, com um "dia de folga" permitido entre dias jogados.</summary>
public static class Streaks
{
    /// <summary>Maior intervalo (em dias) entre dois dias jogados sem quebrar a sequência: 2 = pode faltar 1 dia.</summary>
    public const int MaxGap = 2;

    public static int Current(IEnumerable<DateOnly> activeDays, DateOnly today)
    {
        var days = activeDays.Distinct().OrderDescending().ToList();
        if (days.Count == 0 || today.DayNumber - days[0].DayNumber > MaxGap) return 0;

        var count = 1;
        for (var i = 1; i < days.Count && days[i - 1].DayNumber - days[i].DayNumber <= MaxGap; i++) count++;
        return count;
    }

    public static int Best(IEnumerable<DateOnly> activeDays)
    {
        var days = activeDays.Distinct().Order().ToList();
        int best = 0, run = 0;
        for (var i = 0; i < days.Count; i++)
        {
            run = i > 0 && days[i].DayNumber - days[i - 1].DayNumber <= MaxGap ? run + 1 : 1;
            best = Math.Max(best, run);
        }
        return best;
    }
}

/// <summary>
/// Calibração automática: ajusta um deslocamento de dificuldade por gerador com base nas respostas reais.
/// Se os jogadores acertam mais do que o esperado, o problema é mais fácil do que o calculado (deslocamento negativo).
/// </summary>
public static class Calibration
{
    public const int MinSamples = 50;
    public const int MaxOffset = 300;
    public const int MaxStep = 50;

    public sealed record Sample(int Rating, int Difficulty, double Outcome);

    public static int NewOffset(int currentOffset, IReadOnlyCollection<Sample> samples)
    {
        if (samples.Count < MinSamples) return currentOffset;

        // Resíduo médio: positivo = acertaram mais que o esperado.
        var residual = samples.Average(s => s.Outcome - Rating.Expected(s.Rating, s.Difficulty));
        // Perto de p = 0,5 a curva Elo tem inclinação ≈ ln(10)/1600 por ponto; 1/inclinação ≈ 700.
        var step = Math.Clamp((int)Math.Round(-residual * 700 * 0.5), -MaxStep, MaxStep);
        return Math.Clamp(currentOffset + step, -MaxOffset, MaxOffset);
    }
}
