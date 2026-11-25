using EduGame.Problems.Tools;

using static EduGame.Problems.Lang;

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
            Tr("Complete o Sudoku 6×6: cada linha, cada coluna e cada bloco 2×3 deve ter os números de 1 a 6.", "Complete the 6×6 Sudoku: every row, every column and every 2×3 box must have the numbers 1 to 6.", "Completa el Sudoku 6×6: cada fila, cada columna y cada bloque de 2×3 debe tener los números del 1 al 6."),
            string.Join(',', solution),
            [
                Tr("Comece pelas linhas, colunas ou blocos mais cheios.", "Start with the fullest rows, columns or boxes.", "Empieza por las filas, columnas o bloques más llenos."),
                Tr("Para uma célula vazia, elimine os números que já aparecem na linha, na coluna e no bloco dela.", "For an empty cell, rule out the numbers that already appear in its row, column and box.", "Para una celda vacía, descarta los números que ya aparecen en su fila, su columna y su bloque."),
            ],
            Tr("Cada linha, coluna e bloco 2×3 tem 1 a 6 sem repetir.", "Every row, column and 2×3 box has 1 to 6 with no repeats.", "Cada fila, columna y bloque de 2×3 tiene del 1 al 6 sin repetir."))
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

    private static readonly (Text Text, bool Value)[] Facts =
    [
        (new("7 é um número primo", "7 is a prime number", "7 es un número primo"), true), (new("9 é um número primo", "9 is a prime number", "9 es un número primo"), false), (new("12 é par", "12 is even", "12 es par"), true), (new("15 é par", "15 is even", "15 es par"), false),
        (new("3 × 4 = 12", "3 × 4 = 12", "3 × 4 = 12"), true), (new("5 + 5 = 11", "5 + 5 = 11", "5 + 5 = 11"), false), (new("100 é maior que 99", "100 is greater than 99", "100 es mayor que 99"), true), (new("0,5 é maior que 0,7", "0.5 is greater than 0.7", "0,5 es mayor que 0,7"), false),
        (new("um triângulo tem três lados", "a triangle has three sides", "un triángulo tiene tres lados"), true), (new("um quadrado tem cinco lados", "a square has five sides", "un cuadrado tiene cinco lados"), false), (new("20 é múltiplo de 4", "20 is a multiple of 4", "20 es múltiplo de 4"), true), (new("18 é múltiplo de 5", "18 is a multiple of 5", "18 es múltiplo de 5"), false),
    ];

    public override GeneratedProblem Generate(Random rng)
    {
        var two = Facts.OrderBy(_ => rng.Next()).Take(2).ToArray();
        var (pText, pv) = two[0];
        var (qText, qv) = two[1];
        string p = pText, q = qText;
        string Tv(bool v) => v ? Tr("verdadeira", "true", "verdadera") : Tr("falsa", "false", "falsa");
        string BothParts() => Tr($"A primeira parte é {Tv(pv)} e a segunda é {Tv(qv)}", $"The first part is {Tv(pv)} and the second is {Tv(qv)}", $"La primera parte es {Tv(pv)} y la segunda es {Tv(qv)}");
        var judge = Tr("Julgue cada parte separadamente.", "Judge each part separately.", "Juzga cada parte por separado.");

        switch (rng.Next(5))
        {
            case 0:
                return new(TrueFalse(Tr($"“{p}” E “{q}”.", $"“{p}” AND “{q}”.", $"“{p}” Y “{q}”."), pv && qv,
                    [Tr("Uma frase com E só é verdadeira se as duas partes forem verdadeiras.", "A sentence with AND is only true if both parts are true.", "Una frase con Y solo es verdadera si las dos partes son verdaderas."), judge],
                    Tr($"{BothParts()}; com E, o resultado é {Tv(pv && qv)}.", $"{BothParts()}; with AND, the result is {Tv(pv && qv)}.", $"{BothParts()}; con Y, el resultado es {Tv(pv && qv)}.")), 1450);
            case 1:
                return new(TrueFalse(Tr($"“{p}” OU “{q}”.", $"“{p}” OR “{q}”.", $"“{p}” O “{q}”."), pv || qv,
                    [Tr("Uma frase com OU é verdadeira se pelo menos uma das partes for verdadeira.", "A sentence with OR is true if at least one of the parts is true.", "Una frase con O es verdadera si al menos una de las partes es verdadera."), judge],
                    Tr($"{BothParts()}; com OU, o resultado é {Tv(pv || qv)}.", $"{BothParts()}; with OR, the result is {Tv(pv || qv)}.", $"{BothParts()}; con O, el resultado es {Tv(pv || qv)}.")), 1550);
            case 2:
                return new(TrueFalse(Tr($"Não é verdade que {p}.", $"It is not true that {p}.", $"No es verdad que {p}."), !pv,
                    [
                        Tr("\"Não é verdade que…\" inverte o valor da frase.", "\"It is not true that…\" flips the value of the sentence.", "\"No es verdad que…\" invierte el valor de la frase."),
                        Tr($"Primeiro decida se \"{p}\" é verdadeira.", $"First decide whether \"{p}\" is true.", $"Primero decide si \"{p}\" es verdadera."),
                    ],
                    Tr($"\"{p}\" é {Tv(pv)}, então a negação é {Tv(!pv)}.", $"\"{p}\" is {Tv(pv)}, so the negation is {Tv(!pv)}.", $"\"{p}\" es {Tv(pv)}, así que la negación es {Tv(!pv)}.")), 1500);
            case 3:
                return new(TrueFalse(Tr($"Se {p}, então {q}.", $"If {p}, then {q}.", $"Si {p}, entonces {q}."), !pv || qv,
                    [
                        Tr("\"Se A, então B\" só é falsa quando A é verdadeira e B é falsa.", "\"If A, then B\" is only false when A is true and B is false.", "\"Si A, entonces B\" solo es falsa cuando A es verdadera y B es falsa."),
                        Tr("Se A for falsa, a frase inteira é verdadeira (a promessa não foi quebrada).", "If A is false, the whole sentence is true (the promise wasn't broken).", "Si A es falsa, la frase entera es verdadera (la promesa no se rompió)."),
                    ],
                    Tr($"{BothParts()}; \"se…então\" fica {Tv(!pv || qv)}.", $"{BothParts()}; \"if…then\" is {Tv(!pv || qv)}.", $"{BothParts()}; \"si…entonces\" queda {Tv(!pv || qv)}.")), pv ? 1750 : 1900);
            default:
                return Contrapositive(rng);
        }
    }

    private static GeneratedProblem Contrapositive(Random rng)
    {
        (string Rule, string Fact, string Conclusion, string Wrong1, string Wrong2)[] cases = Current switch
        {
            En =>
            [
                ("If it rains, the street gets wet.", "the street is not wet", "So it didn't rain.", "So it rained.", "We can't know anything."),
                ("If a number is a multiple of 4, it is even.", "the number 10 is not a multiple of 4", "With this rule alone, we can't tell whether 10 is even.", "So 10 is odd.", "So 10 is a multiple of 2 and of 4."),
                ("If Leo studies, he passes the test.", "Leo didn't pass the test", "So Leo didn't study.", "So Leo studied.", "So the test was hard."),
                ("Every athlete on the team trains on Mondays.", "Rita doesn't train on Mondays", "So Rita is not an athlete on the team.", "So Rita is an athlete on the team.", "We can't know anything."),
            ],
            Es =>
            [
                ("Si llueve, la calle se moja.", "la calle no está mojada", "Entonces no llovió.", "Entonces llovió.", "No se puede saber nada."),
                ("Si un número es múltiplo de 4, es par.", "el número 10 no es múltiplo de 4", "Solo con esta regla, no se puede saber si 10 es par.", "Entonces 10 es impar.", "Entonces 10 es múltiplo de 2 y de 4."),
                ("Si Leo estudia, aprueba el examen.", "Leo no aprobó el examen", "Entonces Leo no estudió.", "Entonces Leo estudió.", "Entonces el examen fue difícil."),
                ("Todo atleta del equipo entrena los lunes.", "Rita no entrena los lunes", "Entonces Rita no es atleta del equipo.", "Entonces Rita es atleta del equipo.", "No se puede saber nada."),
            ],
            _ =>
            [
                ("Se chove, a rua fica molhada.", "a rua não está molhada", "Então não choveu.", "Então choveu.", "Não dá para saber nada."),
                ("Se um número é múltiplo de 4, ele é par.", "o número 10 não é múltiplo de 4", "Só com essa regra, não dá para saber se 10 é par.", "Então 10 é ímpar.", "Então 10 é múltiplo de 2 e de 4."),
                ("Se Léo estuda, ele passa na prova.", "Léo não passou na prova", "Então Léo não estudou.", "Então Léo estudou.", "Então a prova foi difícil."),
                ("Todo atleta da equipe treina às segundas.", "Rita não treina às segundas", "Então Rita não é atleta da equipe.", "Então Rita é atleta da equipe.", "Não dá para saber nada."),
            ],
        };
        var (rule, a, conclusion, wrong1, wrong2) = Pick(rng, cases);
        var content = MultipleChoice(rng, Tr($"{rule} Sabemos que {a}. O que podemos concluir?", $"{rule} We know that {a}. What can we conclude?", $"{rule} Sabemos que {a}. ¿Qué podemos concluir?"), conclusion, [wrong1, wrong2],
            [
                Tr("\"Se A, então B\" também quer dizer \"se não B, então não A\".", "\"If A, then B\" also means \"if not B, then not A\".", "\"Si A, entonces B\" también quiere decir \"si no B, entonces no A\"."),
                Tr("Cuidado: \"se não A\" não garante \"não B\".", "Careful: \"if not A\" doesn't guarantee \"not B\".", "Cuidado: \"si no A\" no garantiza \"no B\"."),
            ],
            Tr($"A conclusão correta é: {conclusion}", $"The correct conclusion is: {conclusion}", $"La conclusión correcta es: {conclusion}"), count: 3);
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
        (string Thing, string A, string B)[] groups = Current switch
        {
            En => [("students", "soccer", "volleyball"), ("people", "dogs", "cats"), ("children", "juice", "milk"), ("students", "drawing", "music")],
            Es => [("alumnos", "el fútbol", "el vóley"), ("personas", "los perros", "los gatos"), ("niños", "el jugo", "la leche"), ("alumnos", "el dibujo", "la música")],
            _ => [("alunos", "futebol", "vôlei"), ("pessoas", "cachorro", "gato"), ("crianças", "suco", "leite"), ("alunos", "desenho", "música")],
        };
        var (thing, a, b) = Pick(rng, groups);
        int both = Between(rng, 2, 10), onlyA = Between(rng, 3, 15), onlyB = Between(rng, 3, 15), none = Between(rng, 0, 10);
        int totalA = onlyA + both, totalB = onlyB + both, total = onlyA + onlyB + both + none;
        var intro = Tr($"Numa turma de {total} {thing}, {totalA} gostam de {a}, {totalB} gostam de {b} e {both} gostam dos dois.",
                       $"In a group of {total} {thing}, {totalA} like {a}, {totalB} like {b} and {both} like both.",
                       $"En un grupo de {total} {thing}, a {totalA} les gusta {a}, a {totalB} les gusta {b} y a {both} les gustan los dos.");
        List<string> venn =
        [
            Tr("Desenhe dois círculos que se cruzam: o meio é quem gosta dos dois.", "Draw two overlapping circles: the middle is whoever likes both.", "Dibuja dos círculos que se cruzan: el centro es quien gusta de los dos."),
            Tr($"Só {a}: {totalA} − {both} = {onlyA}.", $"Only {a}: {totalA} − {both} = {onlyA}.", $"Solo {a}: {totalA} − {both} = {onlyA}."),
        ];

        return rng.Next(3) switch
        {
            0 => new(Numeric(Tr($"{intro} Quantos gostam só de {a}?", $"{intro} How many like only {a}?", $"{intro} ¿A cuántos les gusta solo {a}?"), onlyA, venn, $"{totalA} − {both} = {onlyA}"), 1500),
            1 => new(Numeric(Tr($"{intro} Quantos gostam de pelo menos um dos dois?", $"{intro} How many like at least one of the two?", $"{intro} ¿A cuántos les gusta al menos uno de los dos?"), onlyA + onlyB + both,
                [.. venn, Tr("Não conte duas vezes quem gosta dos dois!", "Don't count twice the ones who like both!", "¡No cuentes dos veces a quien le gustan los dos!")], $"{totalA} + {totalB} − {both} = {onlyA + onlyB + both}"), 1700),
            _ => new(Numeric(Tr($"{intro} Quantos não gostam de nenhum dos dois?", $"{intro} How many like neither of the two?", $"{intro} ¿A cuántos no les gusta ninguno de los dos?"), none,
                [.. venn, Tr($"Quem gosta de pelo menos um: {totalA} + {totalB} − {both}.", $"Those who like at least one: {totalA} + {totalB} − {both}.", $"A quienes les gusta al menos uno: {totalA} + {totalB} − {both}.")], $"{total} − ({totalA} + {totalB} − {both}) = {none}"), 1900),
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
                return new(Numeric(Tr($"Se {s[0]} + {s[0]} = {2 * x} e {s[0]} + {s[1]} = {x + y}, quanto vale {s[1]}?", $"If {s[0]} + {s[0]} = {2 * x} and {s[0]} + {s[1]} = {x + y}, what is {s[1]}?", $"Si {s[0]} + {s[0]} = {2 * x} y {s[0]} + {s[1]} = {x + y}, ¿cuánto vale {s[1]}?"), y,
                    [Tr($"Descubra {s[0]} primeiro: dois {s[0]} valem {2 * x}.", $"Find {s[0]} first: two {s[0]} are worth {2 * x}.", $"Descubre {s[0]} primero: dos {s[0]} valen {2 * x}."), $"{s[0]} = {x}."], $"{s[0]} = {x}; {s[1]} = {x + y} − {x} = {y}")
                    with { Visual = $"{s[0]} + {s[0]} = {2 * x}\n{s[0]} + {s[1]} = {x + y}\n{s[1]} = ?" }, 1450);
            case 1:
                return new(Numeric(Tr($"Se {s[0]} × {s[0]} = {x * x} e {s[0]} + {s[1]} = {x + y + 5}, quanto vale {s[1]}?", $"If {s[0]} × {s[0]} = {x * x} and {s[0]} + {s[1]} = {x + y + 5}, what is {s[1]}?", $"Si {s[0]} × {s[0]} = {x * x} y {s[0]} + {s[1]} = {x + y + 5}, ¿cuánto vale {s[1]}?"), y + 5,
                    [Tr($"Que número multiplicado por ele mesmo dá {x * x}?", $"Which number multiplied by itself makes {x * x}?", $"¿Qué número multiplicado por sí mismo da {x * x}?"), $"{s[0]} = {x}."], $"{s[0]} = {x}; {s[1]} = {x + y + 5} − {x} = {y + 5}")
                    with { Visual = $"{s[0]} × {s[0]} = {x * x}\n{s[0]} + {s[1]} = {x + y + 5}\n{s[1]} = ?" }, 1650);
            case 2:
                return new(Numeric(Tr($"Se {s[0]} + {s[1]} = {x + y}, {s[1]} + {s[2]} = {y + z} e {s[0]} = {x}, quanto vale {s[2]}?", $"If {s[0]} + {s[1]} = {x + y}, {s[1]} + {s[2]} = {y + z} and {s[0]} = {x}, what is {s[2]}?", $"Si {s[0]} + {s[1]} = {x + y}, {s[1]} + {s[2]} = {y + z} y {s[0]} = {x}, ¿cuánto vale {s[2]}?"), z,
                    [Tr($"Use {s[0]} = {x} para descobrir {s[1]}.", $"Use {s[0]} = {x} to find {s[1]}.", $"Usa {s[0]} = {x} para descubrir {s[1]}."), $"{s[1]} = {y}."], $"{s[1]} = {x + y} − {x} = {y}; {s[2]} = {y + z} − {y} = {z}")
                    with { Visual = $"{s[0]} + {s[1]} = {x + y}\n{s[1]} + {s[2]} = {y + z}\n{s[0]} = {x}\n{s[2]} = ?" }, 1700);
            default:
            {
                var big = x + y;
                var small = Math.Min(x, y);
                var bigger = Math.Max(x, y);
                if (bigger == small) bigger++;
                return new(Numeric(Tr($"Se {s[0]} + {s[1]} = {bigger + small} e {s[0]} − {s[1]} = {bigger - small}, quanto vale {s[0]}?", $"If {s[0]} + {s[1]} = {bigger + small} and {s[0]} − {s[1]} = {bigger - small}, what is {s[0]}?", $"Si {s[0]} + {s[1]} = {bigger + small} y {s[0]} − {s[1]} = {bigger - small}, ¿cuánto vale {s[0]}?"), bigger,
                    [Tr($"Some as duas igualdades: o {s[1]} desaparece.", $"Add the two equations: the {s[1]} disappears.", $"Suma las dos igualdades: el {s[1]} desaparece."), $"{s[0]} + {s[0]} = {bigger + small} + {bigger - small}."],
                    Tr($"2 × {s[0]} = {2 * bigger}, então {s[0]} = {bigger}", $"2 × {s[0]} = {2 * bigger}, so {s[0]} = {bigger}", $"2 × {s[0]} = {2 * bigger}, así que {s[0]} = {bigger}"))
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

    private static readonly string[] ColorsPt = ["azul", "verde", "vermelho", "amarelo", "roxo"];
    private static readonly string[] ColorsEn = ["blue", "green", "red", "yellow", "purple"];
    private static readonly string[] ColorsEs = ["azul", "verde", "rojo", "amarillo", "morado"];
    private static string[] Colors => Current switch { En => ColorsEn, Es => ColorsEs, _ => ColorsPt };

    public override GeneratedProblem Generate(Random rng)
    {
        var people = LogicTableGenerator.Names.OrderBy(_ => rng.Next()).Take(3).ToArray();
        var pets = LogicTableGenerator.Pets.OrderBy(_ => rng.Next()).Take(3).ToArray();
        var colors = Colors.OrderBy(_ => rng.Next()).Take(3).ToArray();
        LogicCategory[] cats =
        [
            new(pets, i => Tr($"tem {i}", $"has {i}", $"tiene {i}"), i => Tr($"não tem {i}", $"doesn't have {i}", $"no tiene {i}")),
            new(colors, i => Tr($"gosta de {i}", $"likes {i}", $"prefiere el {i}"), i => Tr($"não gosta de {i}", $"doesn't like {i}", $"no prefiere el {i}")),
        ];

        var askedPet = rng.Next(3);
        // Pergunta: de que cor gosta quem tem o animal X?
        string Answer(int[][] a) => colors[a[1][Array.IndexOf(a[0], askedPet)]];
        var puzzle = LogicGridTools.Generate(rng, 3, 2,
            forbidden: c => c is LinkClue { CategoryA: 0, ItemA: var ia, Positive: true } && ia == askedPet,
            questionAnswer: Answer);

        var answer = Answer(puzzle.Solution);
        var clues = puzzle.Clues.Select(c => c.Text(people, cats)).ToList();
        var who = LogicTableGenerator.JoinAnd(people);
        var prompt = Tr(
            $"{who} têm, cada um, um animal diferente ({string.Join(", ", pets)}) e uma cor favorita diferente ({string.Join(", ", colors)}). Pistas: {string.Join(" ", clues)} De que cor gosta quem tem {pets[askedPet]}?",
            $"{who} each have a different pet ({string.Join(", ", pets)}) and a different favorite color ({string.Join(", ", colors)}). Clues: {string.Join(" ", clues)} Which color does the person with {pets[askedPet]} like?",
            $"{who} tienen, cada uno, un animal distinto ({string.Join(", ", pets)}) y un color favorito distinto ({string.Join(", ", colors)}). Pistas: {string.Join(" ", clues)} ¿Qué color prefiere quien tiene {pets[askedPet]}?");
        var content = MultipleChoice(rng, prompt, answer, colors,
            [
                Tr("Monte duas tabelas: pessoa × animal e pessoa × cor.", "Make two tables: person × pet and person × color.", "Arma dos tablas: persona × animal y persona × color."),
                Tr("Marque X onde não pode. Uma linha com uma só opção livre está resolvida.", "Mark an X where it can't be. A row with only one free option is solved.", "Marca X donde no puede ser. Una fila con una sola opción libre está resuelta."),
                Tr("Descubra primeiro quem tem o animal da pergunta.", "First find out who has the pet in the question.", "Descubre primero quién tiene el animal de la pregunta."),
            ],
            Tr($"Juntando as pistas, quem tem {pets[askedPet]} gosta de {answer}.", $"Putting the clues together, the person with {pets[askedPet]} likes {answer}.", $"Juntando las pistas, quien tiene {pets[askedPet]} prefiere el {answer}."), count: 3);
        return new(content, Math.Min(2100, 1300 + clues.Count * 85));
    }
}
