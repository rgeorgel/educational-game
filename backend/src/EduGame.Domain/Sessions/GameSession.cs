using EduGame.Problems;

namespace EduGame.Domain.Sessions;

/// <summary>Uma sessão de jogo: sequência de problemas de uma etapa.</summary>
public class GameSession
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ProfileId { get; set; }
    public int StageId { get; set; }
    public string Mode { get; set; } = "etapa";
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
