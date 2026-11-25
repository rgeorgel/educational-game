using EduGame.Problems;

namespace EduGame.Infrastructure.Seeding;

/// <summary>
/// Definição do currículo (tópicos → níveis → habilidades, regiões e etapas).
/// É a fonte da verdade do conteúdo; o <see cref="ContentSeeder"/> sincroniza com o banco.
/// Para adicionar um tópico novo no futuro, basta acrescentar um <see cref="SubjectDef"/> aqui
/// e os geradores correspondentes em EduGame.Problems.
/// </summary>
public static class ContentCatalog
{
    // Nomes e descrições nos três idiomas (português, inglês, espanhol). O banco guarda o português;
    // a tradução é feita na leitura (ver CatalogText).
    public sealed record SubjectDef(string Slug, Text Name, string Icon, int Order, IReadOnlyList<SkillDef> Skills, IReadOnlyList<RegionDef> Regions);

    public sealed record SkillDef(string Slug, Text Name, Text Description, string Level);

    public sealed record RegionDef(string Slug, Text Name, Text Description, string Icon, string Level, IReadOnlyList<StageDef> Stages);

    /// <param name="Skills">Slugs das habilidades sorteadas nesta etapa.</param>
    public sealed record StageDef(string Slug, Text Name, IReadOnlyList<string> Skills, int ProblemCount = 8);

    public static IReadOnlyList<SubjectDef> Subjects { get; } =
    [
        new("matematica", new("Matemática", "Math", "Matemáticas"), "➗", 1,
            Skills:
            [
                new("contagem-comparacao", new("Contagem e comparação", "Counting and comparing", "Contar y comparar"), new("Contar objetos, achar o número antes e depois e comparar números.", "Count objects, find the number before and after, and compare numbers.", "Contar objetos, encontrar el número anterior y el siguiente y comparar números."), "iniciante"),
                new("dezenas-unidades", new("Dezenas e unidades", "Tens and ones", "Decenas y unidades"), new("Entender o valor de cada algarismo: unidades, dezenas e centenas.", "Understand the value of each digit: ones, tens and hundreds.", "Entender el valor de cada cifra: unidades, decenas y centenas."), "iniciante"),
                new("adicao-ate-100", new("Adição até 100", "Addition up to 100", "Suma hasta 100"), new("Somar números até 100, com e sem \"vai um\".", "Add numbers up to 100, with and without carrying.", "Sumar números hasta 100, con y sin llevadas."), "iniciante"),
                new("subtracao-ate-100", new("Subtração até 100", "Subtraction up to 100", "Resta hasta 100"), new("Subtrair números até 100, com e sem \"empresta um\".", "Subtract numbers up to 100, with and without borrowing.", "Restar números hasta 100, con y sin préstamo."), "iniciante"),
                new("multiplicacao-grupos", new("Grupos iguais", "Equal groups", "Grupos iguales"), new("Primeiros passos na multiplicação: grupos iguais, dobro, triplo e tabuada até 5.", "First steps in multiplication: equal groups, double, triple and times tables up to 5.", "Primeros pasos en la multiplicación: grupos iguales, doble, triple y tablas hasta el 5."), "iniciante"),
                new("dinheiro-e-horas", new("Dinheiro e horas", "Money and time", "Dinero y horas"), new("Contas com dinheiro, troco, relógio e minutos.", "Sums with money, change, clocks and minutes.", "Cuentas con dinero, cambio, reloj y minutos."), "iniciante"),

                new("tabuada-completa", new("Tabuada completa", "Full times tables", "Tablas de multiplicar"), new("Multiplicação até 10 × 10 e o fator que falta.", "Multiplication up to 10 × 10 and the missing factor.", "Multiplicación hasta 10 × 10 y el factor que falta."), "medio"),
                new("divisao", new("Divisão", "Division", "División"), new("Divisão exata e com resto, em contas e situações do dia a dia.", "Exact division and division with remainders, in sums and everyday situations.", "División exacta y con resto, en cuentas y situaciones cotidianas."), "medio"),
                new("expressoes", new("Expressões", "Expressions", "Expresiones"), new("Contas com as quatro operações, parênteses e ordem das operações.", "Calculations with the four operations, parentheses and order of operations.", "Cuentas con las cuatro operaciones, paréntesis y orden de las operaciones."), "medio"),
                new("fracoes-basicas", new("Frações", "Fractions", "Fracciones"), new("Ler, pintar, comparar e somar frações; fração de uma quantidade.", "Read, shade, compare and add fractions; fraction of a quantity.", "Leer, pintar, comparar y sumar fracciones; fracción de una cantidad."), "medio"),
                new("decimais-porcentagem", new("Decimais e porcentagem", "Decimals and percentages", "Decimales y porcentajes"), new("Comparar, ordenar e somar decimais; porcentagens simples.", "Compare, order and add decimals; simple percentages.", "Comparar, ordenar y sumar decimales; porcentajes sencillos."), "medio"),
                new("perimetro-area", new("Perímetro e área", "Perimeter and area", "Perímetro y área"), new("Perímetro e área de retângulos, quadrados e figuras compostas.", "Perimeter and area of rectangles, squares and composite shapes.", "Perímetro y área de rectángulos, cuadrados y figuras compuestas."), "medio"),

                new("numeros-inteiros", new("Números inteiros", "Integers", "Números enteros"), new("Operações com números negativos e situações como temperatura.", "Operations with negative numbers and situations like temperature.", "Operaciones con números negativos y situaciones como la temperatura."), "avancado"),
                new("potencias-raizes", new("Potências e raízes", "Powers and roots", "Potencias y raíces"), new("Potências, quadrados perfeitos e raízes quadradas.", "Powers, perfect squares and square roots.", "Potencias, cuadrados perfectos y raíces cuadradas."), "avancado"),
                new("mmc-mdc", new("MMC e MDC", "LCM and GCD", "MCM y MCD"), new("Mínimo múltiplo comum e máximo divisor comum, com aplicações.", "Least common multiple and greatest common divisor, with applications.", "Mínimo común múltiplo y máximo común divisor, con aplicaciones."), "avancado"),
                new("fracoes-operacoes", new("Operações com frações", "Operations with fractions", "Operaciones con fracciones"), new("Somar, multiplicar, dividir e simplificar frações.", "Add, multiply, divide and simplify fractions.", "Sumar, multiplicar, dividir y simplificar fracciones."), "avancado"),
                new("proporcao-porcentagem", new("Proporção e porcentagem", "Proportion and percentages", "Proporción y porcentajes"), new("Regra de três, aumentos e descontos.", "Rule of three, increases and discounts.", "Regla de tres, aumentos y descuentos."), "avancado"),
                new("equacoes-1-grau", new("Equações do 1º grau", "Linear equations", "Ecuaciones de primer grado"), new("Encontrar o valor de x em equações e problemas.", "Find the value of x in equations and word problems.", "Encontrar el valor de x en ecuaciones y problemas."), "avancado"),

                new("sistemas-equacoes", new("Sistemas de equações", "Systems of equations", "Sistemas de ecuaciones"), new("Duas equações, duas incógnitas: idades, preços, rodas e muito mais.", "Two equations, two unknowns: ages, prices, wheels and much more.", "Dos ecuaciones, dos incógnitas: edades, precios, ruedas y mucho más."), "expert"),
                new("equacoes-2-grau", new("Equações do 2º grau", "Quadratic equations", "Ecuaciones de segundo grado"), new("Raízes, fatoração, soma e produto.", "Roots, factoring, sum and product.", "Raíces, factorización, suma y producto."), "expert"),
                new("juros-porcentagem", new("Juros e porcentagens", "Interest and percentages", "Intereses y porcentajes"), new("Juros simples e compostos, aumentos e descontos seguidos.", "Simple and compound interest, successive increases and discounts.", "Interés simple y compuesto, aumentos y descuentos sucesivos."), "expert"),
                new("geometria-pitagoras", new("Teorema de Pitágoras", "Pythagorean theorem", "Teorema de Pitágoras"), new("Triângulos retângulos, escadas e distâncias no plano.", "Right triangles, ladders and distances on the plane.", "Triángulos rectángulos, escaleras y distancias en el plano."), "expert"),
                new("combinatoria", new("Contagem", "Counting", "Conteo"), new("Combinações, filas, senhas e apertos de mão.", "Combinations, lines, passwords and handshakes.", "Combinaciones, filas, contraseñas y apretones de manos."), "expert"),
            ],
            Regions:
            [
                new("vale-dos-numeros", new("Vale dos Números", "Number Valley", "Valle de los Números"), new("Onde tudo começa: contar, comparar e entender dezenas.", "Where it all begins: counting, comparing and understanding tens.", "Donde todo empieza: contar, comparar y entender las decenas."), "🏞️", "iniciante",
                [
                    new("contando", new("Contando e comparando", "Counting and comparing", "Contando y comparando"), ["contagem-comparacao"]),
                    new("dezenas", new("Dezenas e unidades", "Tens and ones", "Decenas y unidades"), ["dezenas-unidades"]),
                    new("desafio-do-vale", new("Desafio do Vale", "Valley Challenge", "Desafío del Valle"), ["contagem-comparacao", "dezenas-unidades"], 10),
                ]),
                new("rio-das-contas", new("Rio das Contas", "Sum River", "Río de las Cuentas"), new("Atravesse o rio somando e subtraindo.", "Cross the river by adding and subtracting.", "Cruza el río sumando y restando."), "🌊", "iniciante",
                [
                    new("somando", new("Somando", "Adding", "Sumando"), ["adicao-ate-100"]),
                    new("subtraindo", new("Subtraindo", "Subtracting", "Restando"), ["subtracao-ate-100"]),
                    new("desafio-do-rio", new("Desafio do Rio", "River Challenge", "Desafío del Río"), ["adicao-ate-100", "subtracao-ate-100"], 10),
                ]),
                new("feira-e-relogio", new("Feira e Relógio", "Market and Clock", "Mercadillo y Reloj"), new("Grupos iguais, dinheiro e horas do dia a dia.", "Equal groups, money and everyday time.", "Grupos iguales, dinero y horas del día a día."), "🕰️", "iniciante",
                [
                    new("grupos-iguais", new("Grupos iguais", "Equal groups", "Grupos iguales"), ["multiplicacao-grupos"]),
                    new("dinheiro-e-horas", new("Dinheiro e horas", "Money and time", "Dinero y horas"), ["dinheiro-e-horas"]),
                    new("grande-desafio-iniciante", new("Grande Desafio", "Big Challenge", "Gran Desafío"), ["contagem-comparacao", "dezenas-unidades", "adicao-ate-100", "subtracao-ate-100", "multiplicacao-grupos", "dinheiro-e-horas"], 12),
                ]),

                new("cidade-da-multiplicacao", new("Cidade da Multiplicação", "Multiplication City", "Ciudad de la Multiplicación"), new("Tabuada e divisão para andar pela cidade.", "Times tables and division to get around the city.", "Tablas y división para recorrer la ciudad."), "🏙️", "medio",
                [
                    new("tabuada", new("Tabuada completa", "Full times tables", "Tablas de multiplicar"), ["tabuada-completa"]),
                    new("dividindo", new("Dividindo", "Dividing", "Dividiendo"), ["divisao"]),
                    new("desafio-da-cidade", new("Desafio da Cidade", "City Challenge", "Desafío de la Ciudad"), ["tabuada-completa", "divisao"], 10),
                ]),
                new("laboratorio-das-fracoes", new("Laboratório das Frações", "Fraction Lab", "Laboratorio de las Fracciones"), new("Partes, pedaços, decimais e porcentagens.", "Parts, pieces, decimals and percentages.", "Partes, trozos, decimales y porcentajes."), "🧪", "medio",
                [
                    new("fracoes", new("Frações", "Fractions", "Fracciones"), ["fracoes-basicas"]),
                    new("decimais", new("Decimais e porcentagem", "Decimals and percentages", "Decimales y porcentajes"), ["decimais-porcentagem"]),
                    new("desafio-do-laboratorio", new("Desafio do Laboratório", "Lab Challenge", "Desafío del Laboratorio"), ["fracoes-basicas", "decimais-porcentagem"], 10),
                ]),
                new("oficina-das-formas", new("Oficina das Formas", "Shape Workshop", "Taller de las Formas"), new("Expressões, medidas e figuras.", "Expressions, measurements and shapes.", "Expresiones, medidas y figuras."), "📐", "medio",
                [
                    new("expressoes", new("Expressões", "Expressions", "Expresiones"), ["expressoes"]),
                    new("perimetro-e-area", new("Perímetro e área", "Perimeter and area", "Perímetro y área"), ["perimetro-area"]),
                    new("grande-desafio-medio", new("Grande Desafio", "Big Challenge", "Gran Desafío"), ["tabuada-completa", "divisao", "expressoes", "fracoes-basicas", "decimais-porcentagem", "perimetro-area"], 12),
                ]),

                new("montanhas-dos-inteiros", new("Montanhas dos Inteiros", "Integer Mountains", "Montañas de los Enteros"), new("Abaixo de zero e acima das nuvens: inteiros e potências.", "Below zero and above the clouds: integers and powers.", "Bajo cero y sobre las nubes: enteros y potencias."), "⛰️", "avancado",
                [
                    new("inteiros", new("Números inteiros", "Integers", "Números enteros"), ["numeros-inteiros"]),
                    new("potencias", new("Potências e raízes", "Powers and roots", "Potencias y raíces"), ["potencias-raizes"]),
                    new("desafio-das-montanhas", new("Desafio das Montanhas", "Mountain Challenge", "Desafío de las Montañas"), ["numeros-inteiros", "potencias-raizes"], 10),
                ]),
                new("mercado-das-proporcoes", new("Mercado das Proporções", "Proportion Market", "Mercado de las Proporciones"), new("Frações, preços, descontos e regra de três.", "Fractions, prices, discounts and the rule of three.", "Fracciones, precios, descuentos y regla de tres."), "⚖️", "avancado",
                [
                    new("operacoes-com-fracoes", new("Operações com frações", "Operations with fractions", "Operaciones con fracciones"), ["fracoes-operacoes"]),
                    new("proporcoes", new("Proporção e porcentagem", "Proportion and percentages", "Proporción y porcentajes"), ["proporcao-porcentagem"]),
                    new("desafio-do-mercado", new("Desafio do Mercado", "Market Challenge", "Desafío del Mercado"), ["fracoes-operacoes", "proporcao-porcentagem"], 10),
                ]),
                new("observatorio-da-algebra", new("Observatório da Álgebra", "Algebra Observatory", "Observatorio del Álgebra"), new("Múltiplos, divisores e equações.", "Multiples, divisors and equations.", "Múltiplos, divisores y ecuaciones."), "🔭", "avancado",
                [
                    new("mmc-e-mdc", new("MMC e MDC", "LCM and GCD", "MCM y MCD"), ["mmc-mdc"]),
                    new("equacoes", new("Equações", "Equations", "Ecuaciones"), ["equacoes-1-grau"]),
                    new("grande-desafio-avancado", new("Grande Desafio", "Big Challenge", "Gran Desafío"), ["numeros-inteiros", "potencias-raizes", "mmc-mdc", "fracoes-operacoes", "proporcao-porcentagem", "equacoes-1-grau"], 12),
                ]),

                new("ilha-da-algebra", new("Ilha da Álgebra", "Algebra Island", "Isla del Álgebra"), new("Sistemas, equações do 2º grau e o dinheiro que cresce.", "Systems, quadratic equations and money that grows.", "Sistemas, ecuaciones de segundo grado y el dinero que crece."), "🏝️", "expert",
                [
                    new("sistemas", new("Sistemas de equações", "Systems of equations", "Sistemas de ecuaciones"), ["sistemas-equacoes"]),
                    new("segundo-grau", new("Equações do 2º grau", "Quadratic equations", "Ecuaciones de segundo grado"), ["equacoes-2-grau"]),
                    new("juros", new("Juros e porcentagens", "Interest and percentages", "Intereses y porcentajes"), ["juros-porcentagem"]),
                    new("desafio-da-ilha", new("Desafio da Ilha", "Island Challenge", "Desafío de la Isla"), ["sistemas-equacoes", "equacoes-2-grau", "juros-porcentagem"], 10),
                ]),
                new("cume-da-geometria", new("Cume da Geometria", "Geometry Summit", "Cumbre de la Geometría"), new("Pitágoras e a arte de contar possibilidades.", "Pythagoras and the art of counting possibilities.", "Pitágoras y el arte de contar posibilidades."), "🗻", "expert",
                [
                    new("pitagoras", new("Teorema de Pitágoras", "Pythagorean theorem", "Teorema de Pitágoras"), ["geometria-pitagoras"]),
                    new("contagem", new("Contagem", "Counting", "Conteo"), ["combinatoria"]),
                    new("grande-desafio-expert", new("Grande Desafio", "Big Challenge", "Gran Desafío"), ["sistemas-equacoes", "equacoes-2-grau", "juros-porcentagem", "geometria-pitagoras", "combinatoria"], 12),
                ]),
            ]),

        new("logica", new("Lógica", "Logic", "Lógica"), "🧩", 2,
            Skills:
            [
                new("padroes-visuais", new("Padrões visuais", "Visual patterns", "Patrones visuales"), new("Descobrir o pedaço que se repete numa sequência de figuras.", "Find the piece that repeats in a sequence of pictures.", "Descubrir el trozo que se repite en una secuencia de figuras."), "iniciante"),
                new("sequencias-simples", new("Sequências de números", "Number sequences", "Secuencias de números"), new("Encontrar a regra de uma sequência numérica.", "Find the rule of a number sequence.", "Encontrar la regla de una secuencia numérica."), "iniciante"),
                new("qual-e-diferente", new("Qual é o diferente?", "Which one is different?", "¿Cuál es el diferente?"), new("Classificar e achar o item que não segue o grupo.", "Sort things and find the item that doesn't fit the group.", "Clasificar y encontrar el elemento que no sigue al grupo."), "iniciante"),
                new("maior-menor-igual", new("Maior, menor ou igual", "Greater, less or equal", "Mayor, menor o igual"), new("Comparar números e contas usando >, < e =.", "Compare numbers and sums using >, < and =.", "Comparar números y cuentas usando >, < e =."), "iniciante"),

                new("sequencias-compostas", new("Sequências compostas", "Compound sequences", "Secuencias compuestas"), new("Sequências com multiplicação, quadrados e diferenças que mudam.", "Sequences with multiplication, squares and changing differences.", "Secuencias con multiplicación, cuadrados y diferencias que cambian."), "medio"),
                new("grades-numericas", new("Sudoku e quadrado mágico", "Sudoku and magic squares", "Sudoku y cuadrado mágico"), new("Sudoku 4×4 e quadrados mágicos 3×3.", "4×4 Sudoku and 3×3 magic squares.", "Sudoku 4×4 y cuadrados mágicos 3×3."), "medio"),
                new("verdadeiro-ou-falso", new("Verdadeiro ou falso", "True or false", "Verdadero o falso"), new("Julgar afirmações e encontrar contraexemplos.", "Judge statements and find counterexamples.", "Juzgar afirmaciones y encontrar contraejemplos."), "medio"),
                new("balancas", new("Balanças", "Balance scales", "Balanzas"), new("Descobrir pesos equivalentes e trocar uma coisa por outra.", "Find equivalent weights and swap one thing for another.", "Descubrir pesos equivalentes y cambiar una cosa por otra."), "medio"),
                new("tabelas-logicas", new("Tabelas lógicas", "Logic grids", "Tablas lógicas"), new("Descobrir quem tem o quê a partir de pistas.", "Work out who has what from clues.", "Descubrir quién tiene qué a partir de pistas."), "medio"),

                new("sudoku-6x6", new("Sudoku 6×6", "6×6 Sudoku", "Sudoku 6×6"), new("Sudokus maiores, com blocos de 2×3.", "Bigger Sudokus, with 2×3 boxes.", "Sudokus más grandes, con bloques de 2×3."), "avancado"),
                new("proposicoes", new("Proposições", "Propositions", "Proposiciones"), new("E, OU, NÃO e SE…ENTÃO: quando uma frase é verdadeira.", "AND, OR, NOT and IF…THEN: when a sentence is true.", "Y, O, NO y SI…ENTONCES: cuándo una frase es verdadera."), "avancado"),
                new("conjuntos", new("Conjuntos", "Sets", "Conjuntos"), new("Diagramas de Venn: só um, os dois ou nenhum.", "Venn diagrams: only one, both or neither.", "Diagramas de Venn: solo uno, los dos o ninguno."), "avancado"),
                new("enigmas-simbolos", new("Enigmas com símbolos", "Symbol puzzles", "Enigmas con símbolos"), new("Descobrir o valor de cada símbolo a partir de igualdades.", "Find the value of each symbol from equations.", "Descubrir el valor de cada símbolo a partir de igualdades."), "avancado"),
                new("quem-e-quem", new("Quem é quem", "Who is who", "Quién es quién"), new("Cruzar pistas de duas categorias para resolver o enigma.", "Cross clues from two categories to solve the puzzle.", "Cruzar pistas de dos categorías para resolver el enigma."), "avancado"),

                new("tabelas-verdade", new("Tabelas-verdade", "Truth tables", "Tablas de verdad"), new("Avaliar expressões com ¬, ∧, ∨ e →.", "Evaluate expressions with ¬, ∧, ∨ and →.", "Evaluar expresiones con ¬, ∧, ∨ y →."), "expert"),
                new("cavaleiros-mentirosos", new("Cavaleiros e mentirosos", "Knights and knaves", "Caballeros y mentirosos"), new("Quem diz a verdade? Quem mente? Deduza pelas falas.", "Who tells the truth? Who lies? Work it out from what they say.", "¿Quién dice la verdad? ¿Quién miente? Dedúcelo por lo que dicen."), "expert"),
                new("pensamento-computacional", new("Pensamento computacional", "Computational thinking", "Pensamiento computacional"), new("Seguir programas passo a passo: variáveis, repetições e decisões.", "Follow programs step by step: variables, loops and decisions.", "Seguir programas paso a paso: variables, repeticiones y decisiones."), "expert"),
                new("desafios-olimpicos", new("Desafios de raciocínio", "Reasoning challenges", "Desafíos de razonamiento"), new("Quebra-cabeças clássicos que pedem uma ideia esperta.", "Classic puzzles that call for a clever idea.", "Rompecabezas clásicos que piden una idea ingeniosa."), "expert"),
            ],
            Regions:
            [
                new("floresta-dos-padroes", new("Floresta dos Padrões", "Pattern Forest", "Bosque de los Patrones"), new("Descubra as regras escondidas nas sequências.", "Discover the rules hidden in the sequences.", "Descubre las reglas escondidas en las secuencias."), "🌲", "iniciante",
                [
                    new("padroes-de-figuras", new("Padrões de figuras", "Picture patterns", "Patrones de figuras"), ["padroes-visuais"]),
                    new("sequencias-de-numeros", new("Sequências de números", "Number sequences", "Secuencias de números"), ["sequencias-simples"]),
                    new("desafio-da-floresta", new("Desafio da Floresta", "Forest Challenge", "Desafío del Bosque"), ["padroes-visuais", "sequencias-simples"], 10),
                ]),
                new("torre-dos-enigmas", new("Torre dos Enigmas", "Puzzle Tower", "Torre de los Enigmas"), new("Classifique, compare e resolva enigmas.", "Sort, compare and solve puzzles.", "Clasifica, compara y resuelve enigmas."), "🗼", "iniciante",
                [
                    new("qual-e-o-diferente", new("Qual é o diferente?", "Which one is different?", "¿Cuál es el diferente?"), ["qual-e-diferente"]),
                    new("maior-menor-igual", new("Maior, menor ou igual", "Greater, less or equal", "Mayor, menor o igual"), ["maior-menor-igual"]),
                    new("grande-desafio-logica", new("Grande Desafio", "Big Challenge", "Gran Desafío"), ["padroes-visuais", "sequencias-simples", "qual-e-diferente", "maior-menor-igual"], 12),
                ]),

                new("labirinto-dos-numeros", new("Labirinto dos Números", "Number Maze", "Laberinto de los Números"), new("Regras escondidas e grades para completar.", "Hidden rules and grids to complete.", "Reglas escondidas y cuadrículas para completar."), "🌀", "medio",
                [
                    new("sequencias-compostas", new("Sequências compostas", "Compound sequences", "Secuencias compuestas"), ["sequencias-compostas"]),
                    new("sudoku-e-quadrado-magico", new("Sudoku e quadrado mágico", "Sudoku and magic squares", "Sudoku y cuadrado mágico"), ["grades-numericas"]),
                    new("desafio-do-labirinto", new("Desafio do Labirinto", "Maze Challenge", "Desafío del Laberinto"), ["sequencias-compostas", "grades-numericas"], 10),
                ]),
                new("praca-dos-enigmas", new("Praça dos Enigmas", "Puzzle Square", "Plaza de los Enigmas"), new("Verdades, balanças e pistas.", "Truths, scales and clues.", "Verdades, balanzas y pistas."), "🎭", "medio",
                [
                    new("verdadeiro-ou-falso", new("Verdadeiro ou falso", "True or false", "Verdadero o falso"), ["verdadeiro-ou-falso"]),
                    new("balancas", new("Balanças", "Balance scales", "Balanzas"), ["balancas"]),
                    new("tabelas-logicas", new("Tabelas lógicas", "Logic grids", "Tablas lógicas"), ["tabelas-logicas"]),
                    new("grande-desafio-logica-medio", new("Grande Desafio", "Big Challenge", "Gran Desafío"), ["sequencias-compostas", "grades-numericas", "verdadeiro-ou-falso", "balancas", "tabelas-logicas"], 12),
                ]),

                new("castelo-dos-quebra-cabecas", new("Castelo dos Quebra-cabeças", "Puzzle Castle", "Castillo de los Rompecabezas"), new("Sudokus grandes e símbolos misteriosos.", "Big Sudokus and mysterious symbols.", "Sudokus grandes y símbolos misteriosos."), "🏰", "avancado",
                [
                    new("sudoku-6x6", new("Sudoku 6×6", "6×6 Sudoku", "Sudoku 6×6"), ["sudoku-6x6"]),
                    new("enigmas-com-simbolos", new("Enigmas com símbolos", "Symbol puzzles", "Enigmas con símbolos"), ["enigmas-simbolos"]),
                    new("desafio-do-castelo", new("Desafio do Castelo", "Castle Challenge", "Desafío del Castillo"), ["sudoku-6x6", "enigmas-simbolos"], 8),
                ]),
                new("biblioteca-da-logica", new("Biblioteca da Lógica", "Logic Library", "Biblioteca de la Lógica"), new("Proposições, conjuntos e enigmas de detetive.", "Propositions, sets and detective puzzles.", "Proposiciones, conjuntos y enigmas de detective."), "📚", "avancado",
                [
                    new("proposicoes", new("Proposições", "Propositions", "Proposiciones"), ["proposicoes"]),
                    new("conjuntos", new("Conjuntos", "Sets", "Conjuntos"), ["conjuntos"]),
                    new("quem-e-quem", new("Quem é quem", "Who is who", "Quién es quién"), ["quem-e-quem"]),
                    new("grande-desafio-logica-avancado", new("Grande Desafio", "Big Challenge", "Gran Desafío"), ["sudoku-6x6", "proposicoes", "conjuntos", "enigmas-simbolos", "quem-e-quem"], 12),
                ]),

                new("ilha-dos-cavaleiros", new("Ilha dos Cavaleiros", "Knights Island", "Isla de los Caballeros"), new("Onde uns sempre falam a verdade e outros sempre mentem.", "Where some always tell the truth and others always lie.", "Donde unos siempre dicen la verdad y otros siempre mienten."), "🛡️", "expert",
                [
                    new("tabelas-verdade", new("Tabelas-verdade", "Truth tables", "Tablas de verdad"), ["tabelas-verdade"]),
                    new("cavaleiros-e-mentirosos", new("Cavaleiros e mentirosos", "Knights and knaves", "Caballeros y mentirosos"), ["cavaleiros-mentirosos"]),
                    new("desafio-da-ilha-logica", new("Desafio da Ilha", "Island Challenge", "Desafío de la Isla"), ["tabelas-verdade", "cavaleiros-mentirosos"], 10),
                ]),
                new("laboratorio-do-programador", new("Laboratório do Programador", "Programmer's Lab", "Laboratorio del Programador"), new("Programas para seguir e enigmas para resolver.", "Programs to follow and puzzles to solve.", "Programas para seguir y enigmas para resolver."), "💻", "expert",
                [
                    new("programas", new("Pensamento computacional", "Computational thinking", "Pensamiento computacional"), ["pensamento-computacional"]),
                    new("desafios-de-raciocinio", new("Desafios de raciocínio", "Reasoning challenges", "Desafíos de razonamiento"), ["desafios-olimpicos"], 6),
                    new("grande-desafio-logica-expert", new("Grande Desafio", "Big Challenge", "Gran Desafío"), ["tabelas-verdade", "cavaleiros-mentirosos", "pensamento-computacional", "desafios-olimpicos"], 12),
                ]),
            ]),
    ];
}
