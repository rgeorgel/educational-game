namespace EduGame.Infrastructure.Seeding;

/// <summary>
/// Definição do currículo (tópicos → níveis → habilidades, regiões e etapas).
/// É a fonte da verdade do conteúdo; o <see cref="ContentSeeder"/> sincroniza com o banco.
/// Para adicionar um tópico novo no futuro, basta acrescentar um <see cref="SubjectDef"/> aqui
/// e os geradores correspondentes em EduGame.Problems.
/// </summary>
public static class ContentCatalog
{
    public sealed record SubjectDef(string Slug, string Name, string Icon, int Order, IReadOnlyList<SkillDef> Skills, IReadOnlyList<RegionDef> Regions);

    public sealed record SkillDef(string Slug, string Name, string Description, string Level);

    public sealed record RegionDef(string Slug, string Name, string Description, string Icon, string Level, IReadOnlyList<StageDef> Stages);

    /// <param name="Skills">Slugs das habilidades sorteadas nesta etapa.</param>
    public sealed record StageDef(string Slug, string Name, IReadOnlyList<string> Skills, int ProblemCount = 8);

    public static IReadOnlyList<SubjectDef> Subjects { get; } =
    [
        new("matematica", "Matemática", "➗", 1,
            Skills:
            [
                new("contagem-comparacao", "Contagem e comparação", "Contar objetos, achar o número antes e depois e comparar números.", "iniciante"),
                new("dezenas-unidades", "Dezenas e unidades", "Entender o valor de cada algarismo: unidades, dezenas e centenas.", "iniciante"),
                new("adicao-ate-100", "Adição até 100", "Somar números até 100, com e sem \"vai um\".", "iniciante"),
                new("subtracao-ate-100", "Subtração até 100", "Subtrair números até 100, com e sem \"empresta um\".", "iniciante"),
                new("multiplicacao-grupos", "Grupos iguais", "Primeiros passos na multiplicação: grupos iguais, dobro, triplo e tabuada até 5.", "iniciante"),
                new("dinheiro-e-horas", "Dinheiro e horas", "Contas com dinheiro, troco, relógio e minutos.", "iniciante"),

                new("tabuada-completa", "Tabuada completa", "Multiplicação até 10 × 10 e o fator que falta.", "medio"),
                new("divisao", "Divisão", "Divisão exata e com resto, em contas e situações do dia a dia.", "medio"),
                new("expressoes", "Expressões", "Contas com as quatro operações, parênteses e ordem das operações.", "medio"),
                new("fracoes-basicas", "Frações", "Ler, pintar, comparar e somar frações; fração de uma quantidade.", "medio"),
                new("decimais-porcentagem", "Decimais e porcentagem", "Comparar, ordenar e somar decimais; porcentagens simples.", "medio"),
                new("perimetro-area", "Perímetro e área", "Perímetro e área de retângulos, quadrados e figuras compostas.", "medio"),

                new("numeros-inteiros", "Números inteiros", "Operações com números negativos e situações como temperatura.", "avancado"),
                new("potencias-raizes", "Potências e raízes", "Potências, quadrados perfeitos e raízes quadradas.", "avancado"),
                new("mmc-mdc", "MMC e MDC", "Mínimo múltiplo comum e máximo divisor comum, com aplicações.", "avancado"),
                new("fracoes-operacoes", "Operações com frações", "Somar, multiplicar, dividir e simplificar frações.", "avancado"),
                new("proporcao-porcentagem", "Proporção e porcentagem", "Regra de três, aumentos e descontos.", "avancado"),
                new("equacoes-1-grau", "Equações do 1º grau", "Encontrar o valor de x em equações e problemas.", "avancado"),

                new("sistemas-equacoes", "Sistemas de equações", "Duas equações, duas incógnitas: idades, preços, rodas e muito mais.", "expert"),
                new("equacoes-2-grau", "Equações do 2º grau", "Raízes, fatoração, soma e produto.", "expert"),
                new("juros-porcentagem", "Juros e porcentagens", "Juros simples e compostos, aumentos e descontos seguidos.", "expert"),
                new("geometria-pitagoras", "Teorema de Pitágoras", "Triângulos retângulos, escadas e distâncias no plano.", "expert"),
                new("combinatoria", "Contagem", "Combinações, filas, senhas e apertos de mão.", "expert"),
            ],
            Regions:
            [
                new("vale-dos-numeros", "Vale dos Números", "Onde tudo começa: contar, comparar e entender dezenas.", "🏞️", "iniciante",
                [
                    new("contando", "Contando e comparando", ["contagem-comparacao"]),
                    new("dezenas", "Dezenas e unidades", ["dezenas-unidades"]),
                    new("desafio-do-vale", "Desafio do Vale", ["contagem-comparacao", "dezenas-unidades"], 10),
                ]),
                new("rio-das-contas", "Rio das Contas", "Atravesse o rio somando e subtraindo.", "🌊", "iniciante",
                [
                    new("somando", "Somando", ["adicao-ate-100"]),
                    new("subtraindo", "Subtraindo", ["subtracao-ate-100"]),
                    new("desafio-do-rio", "Desafio do Rio", ["adicao-ate-100", "subtracao-ate-100"], 10),
                ]),
                new("feira-e-relogio", "Feira e Relógio", "Grupos iguais, dinheiro e horas do dia a dia.", "🕰️", "iniciante",
                [
                    new("grupos-iguais", "Grupos iguais", ["multiplicacao-grupos"]),
                    new("dinheiro-e-horas", "Dinheiro e horas", ["dinheiro-e-horas"]),
                    new("grande-desafio-iniciante", "Grande Desafio", ["contagem-comparacao", "dezenas-unidades", "adicao-ate-100", "subtracao-ate-100", "multiplicacao-grupos", "dinheiro-e-horas"], 12),
                ]),

                new("cidade-da-multiplicacao", "Cidade da Multiplicação", "Tabuada e divisão para andar pela cidade.", "🏙️", "medio",
                [
                    new("tabuada", "Tabuada completa", ["tabuada-completa"]),
                    new("dividindo", "Dividindo", ["divisao"]),
                    new("desafio-da-cidade", "Desafio da Cidade", ["tabuada-completa", "divisao"], 10),
                ]),
                new("laboratorio-das-fracoes", "Laboratório das Frações", "Partes, pedaços, decimais e porcentagens.", "🧪", "medio",
                [
                    new("fracoes", "Frações", ["fracoes-basicas"]),
                    new("decimais", "Decimais e porcentagem", ["decimais-porcentagem"]),
                    new("desafio-do-laboratorio", "Desafio do Laboratório", ["fracoes-basicas", "decimais-porcentagem"], 10),
                ]),
                new("oficina-das-formas", "Oficina das Formas", "Expressões, medidas e figuras.", "📐", "medio",
                [
                    new("expressoes", "Expressões", ["expressoes"]),
                    new("perimetro-e-area", "Perímetro e área", ["perimetro-area"]),
                    new("grande-desafio-medio", "Grande Desafio", ["tabuada-completa", "divisao", "expressoes", "fracoes-basicas", "decimais-porcentagem", "perimetro-area"], 12),
                ]),

                new("montanhas-dos-inteiros", "Montanhas dos Inteiros", "Abaixo de zero e acima das nuvens: inteiros e potências.", "⛰️", "avancado",
                [
                    new("inteiros", "Números inteiros", ["numeros-inteiros"]),
                    new("potencias", "Potências e raízes", ["potencias-raizes"]),
                    new("desafio-das-montanhas", "Desafio das Montanhas", ["numeros-inteiros", "potencias-raizes"], 10),
                ]),
                new("mercado-das-proporcoes", "Mercado das Proporções", "Frações, preços, descontos e regra de três.", "⚖️", "avancado",
                [
                    new("operacoes-com-fracoes", "Operações com frações", ["fracoes-operacoes"]),
                    new("proporcoes", "Proporção e porcentagem", ["proporcao-porcentagem"]),
                    new("desafio-do-mercado", "Desafio do Mercado", ["fracoes-operacoes", "proporcao-porcentagem"], 10),
                ]),
                new("observatorio-da-algebra", "Observatório da Álgebra", "Múltiplos, divisores e equações.", "🔭", "avancado",
                [
                    new("mmc-e-mdc", "MMC e MDC", ["mmc-mdc"]),
                    new("equacoes", "Equações", ["equacoes-1-grau"]),
                    new("grande-desafio-avancado", "Grande Desafio", ["numeros-inteiros", "potencias-raizes", "mmc-mdc", "fracoes-operacoes", "proporcao-porcentagem", "equacoes-1-grau"], 12),
                ]),

                new("ilha-da-algebra", "Ilha da Álgebra", "Sistemas, equações do 2º grau e o dinheiro que cresce.", "🏝️", "expert",
                [
                    new("sistemas", "Sistemas de equações", ["sistemas-equacoes"]),
                    new("segundo-grau", "Equações do 2º grau", ["equacoes-2-grau"]),
                    new("juros", "Juros e porcentagens", ["juros-porcentagem"]),
                    new("desafio-da-ilha", "Desafio da Ilha", ["sistemas-equacoes", "equacoes-2-grau", "juros-porcentagem"], 10),
                ]),
                new("cume-da-geometria", "Cume da Geometria", "Pitágoras e a arte de contar possibilidades.", "🗻", "expert",
                [
                    new("pitagoras", "Teorema de Pitágoras", ["geometria-pitagoras"]),
                    new("contagem", "Contagem", ["combinatoria"]),
                    new("grande-desafio-expert", "Grande Desafio", ["sistemas-equacoes", "equacoes-2-grau", "juros-porcentagem", "geometria-pitagoras", "combinatoria"], 12),
                ]),
            ]),

        new("logica", "Lógica", "🧩", 2,
            Skills:
            [
                new("padroes-visuais", "Padrões visuais", "Descobrir o pedaço que se repete numa sequência de figuras.", "iniciante"),
                new("sequencias-simples", "Sequências de números", "Encontrar a regra de uma sequência numérica.", "iniciante"),
                new("qual-e-diferente", "Qual é o diferente?", "Classificar e achar o item que não segue o grupo.", "iniciante"),
                new("maior-menor-igual", "Maior, menor ou igual", "Comparar números e contas usando >, < e =.", "iniciante"),

                new("sequencias-compostas", "Sequências compostas", "Sequências com multiplicação, quadrados e diferenças que mudam.", "medio"),
                new("grades-numericas", "Sudoku e quadrado mágico", "Sudoku 4×4 e quadrados mágicos 3×3.", "medio"),
                new("verdadeiro-ou-falso", "Verdadeiro ou falso", "Julgar afirmações e encontrar contraexemplos.", "medio"),
                new("balancas", "Balanças", "Descobrir pesos equivalentes e trocar uma coisa por outra.", "medio"),
                new("tabelas-logicas", "Tabelas lógicas", "Descobrir quem tem o quê a partir de pistas.", "medio"),

                new("sudoku-6x6", "Sudoku 6×6", "Sudokus maiores, com blocos de 2×3.", "avancado"),
                new("proposicoes", "Proposições", "E, OU, NÃO e SE…ENTÃO: quando uma frase é verdadeira.", "avancado"),
                new("conjuntos", "Conjuntos", "Diagramas de Venn: só um, os dois ou nenhum.", "avancado"),
                new("enigmas-simbolos", "Enigmas com símbolos", "Descobrir o valor de cada símbolo a partir de igualdades.", "avancado"),
                new("quem-e-quem", "Quem é quem", "Cruzar pistas de duas categorias para resolver o enigma.", "avancado"),

                new("tabelas-verdade", "Tabelas-verdade", "Avaliar expressões com ¬, ∧, ∨ e →.", "expert"),
                new("cavaleiros-mentirosos", "Cavaleiros e mentirosos", "Quem diz a verdade? Quem mente? Deduza pelas falas.", "expert"),
                new("pensamento-computacional", "Pensamento computacional", "Seguir programas passo a passo: variáveis, repetições e decisões.", "expert"),
                new("desafios-olimpicos", "Desafios de raciocínio", "Quebra-cabeças clássicos que pedem uma ideia esperta.", "expert"),
            ],
            Regions:
            [
                new("floresta-dos-padroes", "Floresta dos Padrões", "Descubra as regras escondidas nas sequências.", "🌲", "iniciante",
                [
                    new("padroes-de-figuras", "Padrões de figuras", ["padroes-visuais"]),
                    new("sequencias-de-numeros", "Sequências de números", ["sequencias-simples"]),
                    new("desafio-da-floresta", "Desafio da Floresta", ["padroes-visuais", "sequencias-simples"], 10),
                ]),
                new("torre-dos-enigmas", "Torre dos Enigmas", "Classifique, compare e resolva enigmas.", "🗼", "iniciante",
                [
                    new("qual-e-o-diferente", "Qual é o diferente?", ["qual-e-diferente"]),
                    new("maior-menor-igual", "Maior, menor ou igual", ["maior-menor-igual"]),
                    new("grande-desafio-logica", "Grande Desafio", ["padroes-visuais", "sequencias-simples", "qual-e-diferente", "maior-menor-igual"], 12),
                ]),

                new("labirinto-dos-numeros", "Labirinto dos Números", "Regras escondidas e grades para completar.", "🌀", "medio",
                [
                    new("sequencias-compostas", "Sequências compostas", ["sequencias-compostas"]),
                    new("sudoku-e-quadrado-magico", "Sudoku e quadrado mágico", ["grades-numericas"]),
                    new("desafio-do-labirinto", "Desafio do Labirinto", ["sequencias-compostas", "grades-numericas"], 10),
                ]),
                new("praca-dos-enigmas", "Praça dos Enigmas", "Verdades, balanças e pistas.", "🎭", "medio",
                [
                    new("verdadeiro-ou-falso", "Verdadeiro ou falso", ["verdadeiro-ou-falso"]),
                    new("balancas", "Balanças", ["balancas"]),
                    new("tabelas-logicas", "Tabelas lógicas", ["tabelas-logicas"]),
                    new("grande-desafio-logica-medio", "Grande Desafio", ["sequencias-compostas", "grades-numericas", "verdadeiro-ou-falso", "balancas", "tabelas-logicas"], 12),
                ]),

                new("castelo-dos-quebra-cabecas", "Castelo dos Quebra-cabeças", "Sudokus grandes e símbolos misteriosos.", "🏰", "avancado",
                [
                    new("sudoku-6x6", "Sudoku 6×6", ["sudoku-6x6"]),
                    new("enigmas-com-simbolos", "Enigmas com símbolos", ["enigmas-simbolos"]),
                    new("desafio-do-castelo", "Desafio do Castelo", ["sudoku-6x6", "enigmas-simbolos"], 8),
                ]),
                new("biblioteca-da-logica", "Biblioteca da Lógica", "Proposições, conjuntos e enigmas de detetive.", "📚", "avancado",
                [
                    new("proposicoes", "Proposições", ["proposicoes"]),
                    new("conjuntos", "Conjuntos", ["conjuntos"]),
                    new("quem-e-quem", "Quem é quem", ["quem-e-quem"]),
                    new("grande-desafio-logica-avancado", "Grande Desafio", ["sudoku-6x6", "proposicoes", "conjuntos", "enigmas-simbolos", "quem-e-quem"], 12),
                ]),

                new("ilha-dos-cavaleiros", "Ilha dos Cavaleiros", "Onde uns sempre falam a verdade e outros sempre mentem.", "🛡️", "expert",
                [
                    new("tabelas-verdade", "Tabelas-verdade", ["tabelas-verdade"]),
                    new("cavaleiros-e-mentirosos", "Cavaleiros e mentirosos", ["cavaleiros-mentirosos"]),
                    new("desafio-da-ilha-logica", "Desafio da Ilha", ["tabelas-verdade", "cavaleiros-mentirosos"], 10),
                ]),
                new("laboratorio-do-programador", "Laboratório do Programador", "Programas para seguir e enigmas para resolver.", "💻", "expert",
                [
                    new("programas", "Pensamento computacional", ["pensamento-computacional"]),
                    new("desafios-de-raciocinio", "Desafios de raciocínio", ["desafios-olimpicos"], 6),
                    new("grande-desafio-logica-expert", "Grande Desafio", ["tabelas-verdade", "cavaleiros-mentirosos", "pensamento-computacional", "desafios-olimpicos"], 12),
                ]),
            ]),
    ];
}
