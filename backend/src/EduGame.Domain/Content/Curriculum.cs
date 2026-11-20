using EduGame.Problems;

namespace EduGame.Domain.Content;

/// <summary>Uma habilidade específica (ex.: "adição até 100"). Unidade de medida do domínio do jogador.</summary>
public class Skill
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public int LevelId { get; set; }
    public Level? Level { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public int Order { get; set; }

    public List<ProblemTemplate> Templates { get; set; } = [];
}

/// <summary>Liga uma habilidade a um gerador de problemas (código em EduGame.Problems).</summary>
public class ProblemTemplate
{
    public int Id { get; set; }
    public int SkillId { get; set; }
    public Skill? Skill { get; set; }
    public required string GeneratorKey { get; set; }
    public bool Active { get; set; } = true;

    /// <summary>Ajuste de dificuldade aprendido com as respostas reais (ver Calibration).</summary>
    public int DifficultyOffset { get; set; }
    public DateTimeOffset? CalibratedAt { get; set; }
}

/// <summary>Uma região do mapa: agrupa etapas de um nível de um tópico.</summary>
public class Region
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public int LevelId { get; set; }
    public Level? Level { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Order { get; set; }

    public List<Stage> Stages { get; set; } = [];
}

/// <summary>Uma etapa jogável: uma sessão de N problemas das habilidades indicadas.</summary>
public class Stage
{
    public int Id { get; set; }
    public int RegionId { get; set; }
    public Region? Region { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }
    public int ProblemCount { get; set; } = 8;
    public List<int> SkillIds { get; set; } = [];
}

/// <summary>Problema escrito à mão (ver EduGame.Problems.CuratedCatalog), sorteado pela chave "curado".</summary>
public class CuratedProblem
{
    public int Id { get; set; }
    public int SkillId { get; set; }
    public required string Slug { get; set; }
    public int Difficulty { get; set; }
    public required ProblemContent Content { get; set; }
    public bool Active { get; set; } = true;
}
