namespace EduGame.Domain.Content;

/// <summary>Um tópico de estudo (Matemática, Lógica… e, no futuro, outros).</summary>
public class Subject
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string Icon { get; set; } = "";
    public int Order { get; set; }

    public List<Level> Levels { get; set; } = [];
}

/// <summary>Um nível de um tópico (Iniciante, Médio, Avançado, Expert).</summary>
public class Level
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string Icon { get; set; } = "";
    public int Order { get; set; }

    /// <summary>Rating mínimo de domínio que caracteriza este nível.</summary>
    public int MinRating { get; set; }
}
