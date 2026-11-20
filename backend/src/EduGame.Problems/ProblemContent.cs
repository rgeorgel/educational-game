namespace EduGame.Problems;

/// <summary>Tipos de desafio. Cada um tem um renderizador no front e um validador aqui.</summary>
public static class ProblemTypes
{
    /// <summary>Digitar um número.</summary>
    public const string Numero = "numero";

    /// <summary>Escolher uma entre 2–4 opções.</summary>
    public const string MultiplaEscolha = "multipla-escolha";

    /// <summary>Completar a lacuna de uma sequência (por número digitado ou por opções).</summary>
    public const string CompletarSequencia = "completar-sequencia";

    /// <summary>Julgar uma afirmação: "Verdadeiro" ou "Falso".</summary>
    public const string VerdadeiroFalso = "verdadeiro-falso";

    /// <summary>Colocar itens (em <see cref="ProblemContent.Choices"/>) na ordem certa. Resposta: itens separados por "|".</summary>
    public const string Ordenar = "ordenar";

    /// <summary>Preencher uma grade (Sudoku, quadrado mágico). Resposta: células separadas por ",".</summary>
    public const string Grade = "grade";

    /// <summary>Pintar partes de uma figura. Resposta: quantidade de partes pintadas.</summary>
    public const string FracaoVisual = "fracao-visual";

    public static readonly IReadOnlyList<string> All = [Numero, MultiplaEscolha, CompletarSequencia, VerdadeiroFalso, Ordenar, Grade, FracaoVisual];

    public const string True = "Verdadeiro";
    public const string False = "Falso";
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

    /// <summary>Opções (múltipla escolha, sequência com opções) ou itens a ordenar.</summary>
    public IReadOnlyList<string>? Choices { get; init; }

    /// <summary>Itens da sequência; <c>null</c> marca a lacuna.</summary>
    public IReadOnlyList<string?>? Sequence { get; init; }

    /// <summary>Grade a preencher (tipo <see cref="ProblemTypes.Grade"/>).</summary>
    public GridPuzzle? Grid { get; init; }

    /// <summary>Figura dividida em partes (para ler ou pintar frações).</summary>
    public FractionFigure? Figure { get; init; }

    /// <summary>A resposta numérica pode ser negativa (o teclado mostra "−").</summary>
    public bool AcceptsNegative { get; init; }

    /// <summary>A resposta numérica pode ter vírgula (o teclado mostra ",").</summary>
    public bool AcceptsDecimal { get; init; }
}

public static class GridKinds
{
    public const string Sudoku = "sudoku";
    public const string MagicSquare = "quadrado-magico";
}

/// <summary>
/// Grade quadrada. <see cref="Cells"/> tem Size×Size posições (linha a linha); <c>null</c> = célula a preencher.
/// Para Sudoku: cada linha, coluna e bloco (BoxRows×BoxCols) tem 1..Size.
/// Para quadrado mágico: números 1..Size² sem repetir e todas as linhas, colunas e diagonais somam <see cref="Target"/>.
/// </summary>
public sealed record GridPuzzle(string Kind, int Size, int BoxRows, int BoxCols, int MaxValue, int? Target, IReadOnlyList<int?> Cells);

/// <summary>Figura ("barra" ou "circulo") dividida em <see cref="Parts"/> partes iguais, com <see cref="Filled"/> pintadas.</summary>
public sealed record FractionFigure(string Shape, int Parts, int Filled);

public sealed record GeneratedProblem(ProblemContent Content, int Difficulty);
