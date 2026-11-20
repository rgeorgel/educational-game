namespace EduGame.Domain.Players;

/// <summary>
/// Perfil de um jogador dentro da conta de um responsável.
/// Guarda o mínimo de dados (LGPD): apelido, avatar, ano de nascimento e série — opcionais.
/// </summary>
public class PlayerProfile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public required string Nickname { get; set; }
    public string Avatar { get; set; } = "🦊";
    public int? BirthYear { get; set; }
    public int? SchoolGrade { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Domínio do jogador numa habilidade (rating estilo Elo).</summary>
public class SkillMastery
{
    public Guid ProfileId { get; set; }
    public int SkillId { get; set; }
    public int Rating { get; set; }
    public int AttemptsCount { get; set; }
    public int CorrectCount { get; set; }
    public DateTimeOffset? LastPracticedAt { get; set; }
}

/// <summary>Progresso do jogador num tópico: nível conquistado e XP acumulado.</summary>
public class SubjectProgress
{
    public Guid ProfileId { get; set; }
    public int SubjectId { get; set; }
    public int CurrentLevelId { get; set; }
    public int Xp { get; set; }
    public DateTimeOffset LevelReachedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Melhor resultado do jogador numa etapa.</summary>
public class StageProgress
{
    public Guid ProfileId { get; set; }
    public int StageId { get; set; }
    public int BestStars { get; set; }
    public int TimesCompleted { get; set; }
    public DateTimeOffset LastCompletedAt { get; set; }
}
