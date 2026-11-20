using EduGame.Problems.Tools;

namespace EduGame.Problems.LogicSkills;

/// <summary>Sudoku 6×6 (blocos de 2×3) com solução única.</summary>
public sealed class Sudoku6Generator : ProblemGenerator
{
    public override string Key => "log.sudoku-6x6";
    public override string SkillSlug => "sudoku-6x6";
    public override string Level => "avancado";

    public override GeneratedProblem Generate(Random rng)
    {
        var target = Between(rng, 10, 22);
        var solution = SudokuTools.Solution(rng, 2, 3);
        var cells = SudokuTools.MakePuzzle(rng, solution, 2, 3, target);
        var blanks = cells.Count(c => c is null);
        var content = new ProblemContent(
            ProblemTypes.Grade,
            "Complete o Sudoku 6×6: cada linha, cada coluna e cada bloco 2×3 deve ter os números de 1 a 6.",
            string.Join(',', solution),
            ["Comece pelas linhas, colunas ou blocos mais cheios.", "Para uma célula vazia, elimine os números que já aparecem na linha, na coluna e no bloco dela."],
            "Cada linha, coluna e bloco 2×3 tem 1 a 6 sem repetir.")
        { Grid = new GridPuzzle(GridKinds.Sudoku, 6, 2, 3, 6, null, cells) };
        return new(content, 1450 + Math.Max(0, blanks - 10) * 45);
    }
}

/// <summary>Proposições: e, ou, não, se…então (tabelas-verdade aplicadas a frases sobre números).</summary>
public sealed class PropositionsGenerator : ProblemGenerator
{
    public override string Key => "log.proposicoes";
    public override string SkillSlug => "proposicoes";
    public override string Level => "avancado";

    private static readonly (string Text, bool Value)[] Facts =
    [
        ("7 é um número primo", true), ("9 é um número primo", false), ("12 é par", true), ("15 é par", false),
        ("3 × 4 = 12", true), ("5 + 5 = 11", false), ("100 é maior que 99", true), ("0,5 é maior que 0,7", false),
        ("um triângulo tem três lados", true), ("um quadrado tem cinco lados", false), ("20 é múltiplo de 4", true), ("18 é múltiplo de 5", false),
    ];

    public override GeneratedProblem Generate(Random rng)
    {
        var two = Facts.OrderBy(_ => rng.Next()).Take(2).ToArray();
        var (p, pv) = two[0];
        var (q, qv) = two[1];
        string Tv(bool v) => v ? "verdadeira" : "falsa";

        switch (rng.Next(5))
        {
            case 0:
                return new(TrueFalse($"“{p}” E “{q}”.", pv && qv,
                    ["Uma frase com E só é verdadeira se as duas partes forem verdadeiras.", "Julgue cada parte separadamente."],
                    $"A primeira parte é {Tv(pv)} e a segunda é {Tv(qv)}; com E, o resultado é {Tv(pv && qv)}."), 1450);
            case 1:
                return new(TrueFalse($"“{p}” OU “{q}”.", pv || qv,
                    ["Uma frase com OU é verdadeira se pelo menos uma das partes for verdadeira.", "Julgue cada parte separadamente."],
                    $"A primeira parte é {Tv(pv)} e a segunda é {Tv(qv)}; com OU, o resultado é {Tv(pv || qv)}."), 1550);
            case 2:
                return new(TrueFalse($"Não é verdade que {p}.", !pv,
                    ["\"Não é verdade que…\" inverte o valor da frase.", $"Primeiro decida se \"{p}\" é verdadeira."],
                    $"\"{p}\" é {Tv(pv)}, então a negação é {Tv(!pv)}."), 1500);
            case 3:
                return new(TrueFalse($"Se {p}, então {q}.", !pv || qv,
                    ["\"Se A, então B\" só é falsa quando A é verdadeira e B é falsa.", "Se A for falsa, a frase inteira é verdadeira (a promessa não foi quebrada)."],
                    $"A primeira parte é {Tv(pv)} e a segunda é {Tv(qv)}; \"se…então\" fica {Tv(!pv || qv)}."), pv ? 1750 : 1900);
            default:
                return Contrapositive(rng);
        }
    }

    private static GeneratedProblem Contrapositive(Random rng)
    {
        var (rule, a, conclusion, wrong1, wrong2) = Pick(rng,
        [
            ("Se chove, a rua fica molhada.", "a rua não está molhada", "Então não choveu.", "Então choveu.", "Não dá para saber nada."),
            ("Se um número é múltiplo de 4, ele é par.", "o número 10 não é múltiplo de 4", "Só com essa regra, não dá para saber se 10 é par.", "Então 10 é ímpar.", "Então 10 é múltiplo de 2 e de 4."),
            ("Se Léo estuda, ele passa na prova.", "Léo não passou na prova", "Então Léo não estudou.", "Então Léo estudou.", "Então a prova foi difícil."),
            ("Todo atleta da equipe treina às segundas.", "Rita não treina às segundas", "Então Rita não é atleta da equipe.", "Então Rita é atleta da equipe.", "Não dá para saber nada."),
        ]);
        var content = MultipleChoice(rng, $"{rule} Sabemos que {a}. O que podemos concluir?", conclusion, [wrong1, wrong2],
            ["\"Se A, então B\" também quer dizer \"se não B, então não A\".", "Cuidado: \"se não A\" não garante \"não B\"."],
            $"A conclusão correta é: {conclusion}", count: 3);
        return new(content, 1950);
    }
}

/// <summary>Conjuntos e diagramas de Venn com duas categorias.</summary>
public sealed class SetsGenerator : ProblemGenerator
{
    public override string Key => "log.conjuntos";
    public override string SkillSlug => "conjuntos";
    public override string Level => "avancado";

    public override GeneratedProblem Generate(Random rng)
    {
        var (thing, a, b) = Pick(rng, [("alunos", "futebol", "vôlei"), ("pessoas", "cachorro", "gato"), ("crianças", "suco", "leite"), ("alunos", "desenho", "música")]);
        int both = Between(rng, 2, 10), onlyA = Between(rng, 3, 15), onlyB = Between(rng, 3, 15), none = Between(rng, 0, 10);
        int totalA = onlyA + both, totalB = onlyB + both, total = onlyA + onlyB + both + none;
        var intro = $"Numa turma de {total} {thing}, {totalA} gostam de {a}, {totalB} gostam de {b} e {both} gostam dos dois.";
        List<string> venn = ["Desenhe dois círculos que se cruzam: o meio é quem gosta dos dois.", $"Só {a}: {totalA} − {both} = {onlyA}."];

        return rng.Next(3) switch
        {
            0 => new(Numeric($"{intro} Quantos gostam só de {a}?", onlyA, venn, $"{totalA} − {both} = {onlyA}"), 1500),
            1 => new(Numeric($"{intro} Quantos gostam de pelo menos um dos dois?", onlyA + onlyB + both, [.. venn, "Não conte duas vezes quem gosta dos dois!"], $"{totalA} + {totalB} − {both} = {onlyA + onlyB + both}"), 1700),
            _ => new(Numeric($"{intro} Quantos não gostam de nenhum dos dois?", none, [.. venn, $"Quem gosta de pelo menos um: {totalA} + {totalB} − {both}."], $"{total} − ({totalA} + {totalB} − {both}) = {none}"), 1900),
        };
    }
}

/// <summary>Enigmas com símbolos: sistemas de equações simples escritos com figuras.</summary>
public sealed class SymbolPuzzleGenerator : ProblemGenerator
{
    public override string Key => "log.enigmas-simbolos";
    public override string SkillSlug => "enigmas-simbolos";
    public override string Level => "avancado";

    private static readonly string[] Symbols = ["▲", "■", "●", "◆", "★"];

    public override GeneratedProblem Generate(Random rng)
    {
        var s = Symbols.OrderBy(_ => rng.Next()).Take(3).ToArray();
        int x = Between(rng, 2, 9), y = Between(rng, 1, 9), z = Between(rng, 1, 9);

        switch (rng.Next(4))
        {
            case 0:
                return new(Numeric($"Se {s[0]} + {s[0]} = {2 * x} e {s[0]} + {s[1]} = {x + y}, quanto vale {s[1]}?", y,
                    [$"Descubra {s[0]} primeiro: dois {s[0]} valem {2 * x}.", $"{s[0]} = {x}."], $"{s[0]} = {x}; {s[1]} = {x + y} − {x} = {y}")
                    with { Visual = $"{s[0]} + {s[0]} = {2 * x}\n{s[0]} + {s[1]} = {x + y}\n{s[1]} = ?" }, 1450);
            case 1:
                return new(Numeric($"Se {s[0]} × {s[0]} = {x * x} e {s[0]} + {s[1]} = {x + y + 5}, quanto vale {s[1]}?", y + 5,
                    [$"Que número multiplicado por ele mesmo dá {x * x}?", $"{s[0]} = {x}."], $"{s[0]} = {x}; {s[1]} = {x + y + 5} − {x} = {y + 5}")
                    with { Visual = $"{s[0]} × {s[0]} = {x * x}\n{s[0]} + {s[1]} = {x + y + 5}\n{s[1]} = ?" }, 1650);
            case 2:
                return new(Numeric($"Se {s[0]} + {s[1]} = {x + y}, {s[1]} + {s[2]} = {y + z} e {s[0]} = {x}, quanto vale {s[2]}?", z,
                    [$"Use {s[0]} = {x} para descobrir {s[1]}.", $"{s[1]} = {y}."], $"{s[1]} = {x + y} − {x} = {y}; {s[2]} = {y + z} − {y} = {z}")
                    with { Visual = $"{s[0]} + {s[1]} = {x + y}\n{s[1]} + {s[2]} = {y + z}\n{s[0]} = {x}\n{s[2]} = ?" }, 1700);
            default:
            {
                var big = x + y;
                var small = Math.Min(x, y);
                var bigger = Math.Max(x, y);
                if (bigger == small) bigger++;
                return new(Numeric($"Se {s[0]} + {s[1]} = {bigger + small} e {s[0]} − {s[1]} = {bigger - small}, quanto vale {s[0]}?", bigger,
                    ["Some as duas igualdades: o " + s[1] + " desaparece.", $"{s[0]} + {s[0]} = {bigger + small} + {bigger - small}."], $"2 × {s[0]} = {2 * bigger}, então {s[0]} = {bigger}")
                    with { Visual = $"{s[0]} + {s[1]} = {bigger + small}\n{s[0]} − {s[1]} = {bigger - small}\n{s[0]} = ?" }, 1850 + (big > 12 ? 50 : 0));
            }
        }
    }
}

/// <summary>"Quem é quem": três pessoas, duas categorias (animal e cor) e pistas cruzadas.</summary>
public sealed class WhoIsWhoGenerator : ProblemGenerator
{
    public override string Key => "log.quem-e-quem";
    public override string SkillSlug => "quem-e-quem";
    public override string Level => "avancado";

    private static readonly string[] Colors = ["azul", "verde", "vermelho", "amarelo", "roxo"];

    public override GeneratedProblem Generate(Random rng)
    {
        var people = LogicTableGenerator.Names.OrderBy(_ => rng.Next()).Take(3).ToArray();
        var pets = LogicTableGenerator.Pets.OrderBy(_ => rng.Next()).Take(3).ToArray();
        var colors = Colors.OrderBy(_ => rng.Next()).Take(3).ToArray();
        LogicCategory[] cats =
        [
            new(pets, i => $"tem {i}", i => $"não tem {i}"),
            new(colors, i => $"gosta de {i}", i => $"não gosta de {i}"),
        ];

        var askedPet = rng.Next(3);
        // Pergunta: de que cor gosta quem tem o animal X?
        string Answer(int[][] a) => colors[a[1][Array.IndexOf(a[0], askedPet)]];
        var puzzle = LogicGridTools.Generate(rng, 3, 2,
            forbidden: c => c is LinkClue { CategoryA: 0, ItemA: var ia, Positive: true } && ia == askedPet,
            questionAnswer: Answer);

        var answer = Answer(puzzle.Solution);
        var clues = puzzle.Clues.Select(c => c.Text(people, cats)).ToList();
        var prompt = $"{people[0]}, {people[1]} e {people[2]} têm, cada um, um animal diferente ({string.Join(", ", pets)}) e uma cor favorita diferente ({string.Join(", ", colors)}). Pistas: {string.Join(" ", clues)} De que cor gosta quem tem {pets[askedPet]}?";
        var content = MultipleChoice(rng, prompt, answer, colors,
            ["Monte duas tabelas: pessoa × animal e pessoa × cor.", "Marque X onde não pode. Uma linha com uma só opção livre está resolvida.", "Descubra primeiro quem tem o animal da pergunta."],
            $"Juntando as pistas, quem tem {pets[askedPet]} gosta de {answer}.", count: 3);
        return new(content, Math.Min(2100, 1300 + clues.Count * 85));
    }
}
