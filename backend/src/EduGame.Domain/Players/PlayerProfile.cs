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

    /// <summary>Acessório do avatar (ver AvatarItems.Accessories).</summary>
    public string Accessory { get; set; } = "nenhum";

    /// <summary>Cor de fundo do avatar (ver AvatarItems.Colors).</summary>
    public string Color { get; set; } = "padrao";
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

    /// <summary>O jogador já fez (ou dispensou) o teste de nivelamento deste tópico.</summary>
    public bool PlacementDone { get; set; }
}

/// <summary>Atividade de um dia (para sequência de dias e gráfico de histórico).</summary>
public class DailyActivity
{
    public Guid ProfileId { get; set; }
    public DateOnly Date { get; set; }
    public int ProblemsSolved { get; set; }
    public int Correct { get; set; }
    public int Xp { get; set; }
}

/// <summary>Medalha conquistada (o catálogo fica em EduGame.Domain.Progression.Achievements).</summary>
public class ProfileAchievement
{
    public Guid ProfileId { get; set; }
    public required string Slug { get; set; }
    public DateTimeOffset EarnedAt { get; set; } = DateTimeOffset.UtcNow;
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

/// <summary>
/// Turma de um professor. O responsável escolhe, com o código da turma, quais perfis participam
/// (consentimento explícito) e pode sair a qualquer momento.
/// </summary>
public class Classroom
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OwnerUserId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ClassroomMember
{
    public Guid ClassroomId { get; set; }
    public Guid ProfileId { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
