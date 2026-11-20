using EduGame.Problems;

namespace EduGame.Domain.Sessions;

public static class SessionModes
{
    /// <summary>Uma etapa do mapa.</summary>
    public const string Stage = "etapa";

    /// <summary>Desafio do Guardião (prova de fim de nível).</summary>
    public const string Guardian = "guardiao";

    /// <summary>Teste de nivelamento.</summary>
    public const string Placement = "nivelamento";
}

/// <summary>Uma sessão de jogo: uma etapa, um Desafio do Guardião ou um teste de nivelamento.</summary>
public class GameSession
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ProfileId { get; set; }
    public int SubjectId { get; set; }

    /// <summary>Etapa jogada (só no modo <see cref="SessionModes.Stage"/>).</summary>
    public int? StageId { get; set; }

    /// <summary>Nível avaliado (modo <see cref="SessionModes.Guardian"/>).</summary>
    public int? LevelId { get; set; }

    public string Mode { get; set; } = SessionModes.Stage;

    /// <summary>Habilidades sorteadas nesta sessão.</summary>
    public List<int> SkillIds { get; set; } = [];

    /// <summary>Estimativa de rating do teste de nivelamento (atualizada a cada resposta).</summary>
    public int? PlacementEstimate { get; set; }

    /// <summary>Nível alcançado ao final (Guardião aprovado ou nivelamento).</summary>
    public int? ResultLevelId { get; set; }
    public int ProblemCount { get; set; }
    public int CorrectCount { get; set; }
    public int XpEarned { get; set; }
    public int? Stars { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }

    public List<SessionProblem> Problems { get; set; } = [];

    public bool IsFinished => FinishedAt is not null;
}

/// <summary>Um problema servido numa sessão. O conteúdo gerado é guardado para validação e auditoria.</summary>
public class SessionProblem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SessionId { get; set; }
    public int Order { get; set; }
    public int SkillId { get; set; }
    public required string GeneratorKey { get; set; }
    public int Seed { get; set; }
    public int Difficulty { get; set; }
    public required ProblemContent Content { get; set; }
    public int HintsRevealed { get; set; }
    public DateTimeOffset ServedAt { get; set; } = DateTimeOffset.UtcNow;

    public Attempt? Attempt { get; set; }
}

/// <summary>A resposta do jogador a um problema (uma por problema).</summary>
public class Attempt
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SessionProblemId { get; set; }
    public required string Answer { get; set; }
    public bool IsCorrect { get; set; }
    public int HintsUsed { get; set; }
    public int TimeMs { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
    public int XpEarned { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
