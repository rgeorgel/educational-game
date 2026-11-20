namespace EduGame.Problems;

/// <summary>Tipos de desafio. Cada um tem um renderizador no front e um validador aqui.</summary>
public static class ProblemTypes
{
    /// <summary>Digitar um número.</summary>
    public const string Numero = "numero";

    /// <summary>Escolher uma entre 3–4 opções.</summary>
    public const string MultiplaEscolha = "multipla-escolha";

    /// <summary>Completar a lacuna de uma sequência (por número digitado ou por opções).</summary>
    public const string CompletarSequencia = "completar-sequencia";
}

/// <summary>Conteúdo completo de um problema gerado (inclui a resposta — nunca enviar ao cliente antes da resposta).</summary>
public sealed record ProblemContent(
    string Type,
    string Prompt,
    string Answer,
    IReadOnlyList<string> Hints,
    string Explanation)
{
    /// <summary>Linha visual opcional exibida em destaque (ex.: emojis para contar).</summary>
    public string? Visual { get; init; }

    /// <summary>Opções (múltipla escolha, ou sequência com opções).</summary>
    public IReadOnlyList<string>? Choices { get; init; }

    /// <summary>Itens da sequência; <c>null</c> marca a lacuna.</summary>
    public IReadOnlyList<string?>? Sequence { get; init; }
}

public sealed record GeneratedProblem(ProblemContent Content, int Difficulty);
