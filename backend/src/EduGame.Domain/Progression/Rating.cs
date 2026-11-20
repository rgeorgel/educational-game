namespace EduGame.Domain.Progression;

/// <summary>
/// Sistema de rating inspirado no Elo. Cada habilidade do jogador tem um rating,
/// cada problema tem uma dificuldade na mesma escala.
/// </summary>
public static class Rating
{
    /// <summary>Rating inicial de uma habilidade nunca praticada.</summary>
    public const int Initial = 300;

    public const int Min = 0;
    public const int Max = 3000;

    /// <summary>Resultado de uma tentativa, de 0 (erro) a 1 (acerto sem ajuda).</summary>
    public static double Outcome(bool correct, int hintsUsed) => (correct, hintsUsed) switch
    {
        (false, _) => 0.0,
        (true, 0) => 1.0,
        (true, 1) => 0.7,
        (true, _) => 0.5,
    };

    /// <summary>Probabilidade esperada de acerto de um jogador com <paramref name="rating"/> num problema de <paramref name="difficulty"/>.</summary>
    public static double Expected(int rating, int difficulty) =>
        1.0 / (1.0 + Math.Pow(10, (difficulty - rating) / 400.0));

    /// <summary>
    /// Fator K: grande no começo (o jogo descobre rápido o nível real),
    /// menor depois (o rating fica estável).
    /// </summary>
    public static int KFactor(int attemptsSoFar) => attemptsSoFar switch
    {
        < 20 => 64,
        < 60 => 40,
        _ => 24,
    };

    public static int Update(int rating, int difficulty, double outcome, int attemptsSoFar)
    {
        var delta = KFactor(attemptsSoFar) * (outcome - Expected(rating, difficulty));
        return Math.Clamp((int)Math.Round(rating + delta), Min, Max);
    }
}
