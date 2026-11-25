using EduGame.Problems;

namespace EduGame.Infrastructure.Seeding;

/// <summary>
/// Alinhamento das habilidades do jogo com a BNCC (Base Nacional Comum Curricular).
///
/// - Matemática: habilidades do Ensino Fundamental (EF01MA…EF09MA) e algumas do Ensino Médio (EM13MAT…).
/// - Lógica: parte é coberta pela própria Matemática (sequências, igualdade, fluxogramas) e parte pelo
///   complemento de Computação da BNCC (Resolução CNE/CEB nº 1/2022), eixo Pensamento Computacional (EF0xCO…).
///
/// Os resumos são paráfrases curtas, não o texto oficial; para documentos formais, consulte a BNCC.
/// Algumas habilidades de lógica (ex.: enigmas de detetive) não têm habilidade correspondente direta
/// e ficam com a lista vazia: são raciocínio lógico complementar ao currículo.
/// O mapeamento é uma referência pedagógica e deve ser revisado por professores.
/// </summary>
public static class BnccCatalog
{
    /// <param name="Summary">Resumo em português, inglês e espanhol.</param>
    public sealed record Habilidade(string Code, Text Summary)
    {
        public Text Component => Code.Contains("CO") ? Computing : Math;

        /// <summary>"3º ano" (EF03…) ou "Ensino Médio" (EM13…).</summary>
        public Text Grade => Code.StartsWith("EM") ? HighSchool : GradeName(int.Parse(Code.AsSpan(2, 2)));

        /// <summary>1–9 para o Ensino Fundamental, 10 para o Ensino Médio (para ordenar).</summary>
        public int GradeOrder => Code.StartsWith("EM") ? 10 : int.Parse(Code.AsSpan(2, 2));
    }

    public static readonly Text Math = new("Matemática", "Math", "Matemáticas");
    public static readonly Text Computing = new("Computação", "Computing", "Computación");
    public static readonly Text HighSchool = new("Ensino Médio", "High school", "Educación secundaria superior");

    /// <summary>Ano do Ensino Fundamental ("3º ano", "Year 3", "3.º año").</summary>
    public static Text GradeName(int year) => new($"{year}º ano", $"Year {year}", $"{year}.º año");

    public static IReadOnlyList<Habilidade> Habilidades { get; } =
    [
        // ---------- Matemática, 1º ano ----------
        new("EF01MA01", new("Usar números naturais para indicar quantidade ou ordem no dia a dia.", "Use natural numbers to show quantity or order in everyday life.", "Usar números naturales para indicar cantidad u orden en el día a día.")),
        new("EF01MA04", new("Contar coleções de até 100 objetos e registrar o resultado.", "Count collections of up to 100 objects and record the result.", "Contar colecciones de hasta 100 objetos y registrar el resultado.")),
        new("EF01MA05", new("Comparar números naturais de até duas ordens, com ou sem reta numérica.", "Compare natural numbers with up to two digits, with or without a number line.", "Comparar números naturales de hasta dos cifras, con o sin recta numérica.")),
        new("EF01MA06", new("Construir fatos básicos da adição e usá-los no cálculo.", "Build basic addition facts and use them in calculations.", "Construir hechos básicos de la suma y usarlos en el cálculo.")),
        new("EF01MA07", new("Compor e decompor números de até duas ordens.", "Compose and decompose numbers with up to two digits.", "Componer y descomponer números de hasta dos cifras.")),
        new("EF01MA08", new("Resolver problemas de adição e subtração com números de até dois algarismos.", "Solve addition and subtraction problems with numbers of up to two digits.", "Resolver problemas de suma y resta con números de hasta dos cifras.")),
        new("EF01MA09", new("Organizar e ordenar objetos por atributos como cor, forma e tamanho.", "Organize and order objects by attributes such as color, shape and size.", "Organizar y ordenar objetos por atributos como color, forma y tamaño.")),
        new("EF01MA10", new("Descobrir os elementos que faltam em sequências de números, objetos ou figuras.", "Find the missing elements in sequences of numbers, objects or pictures.", "Descubrir los elementos que faltan en secuencias de números, objetos o figuras.")),
        new("EF01MA19", new("Reconhecer e relacionar valores de moedas e cédulas do real.", "Recognize and relate the values of coins and banknotes.", "Reconocer y relacionar valores de monedas y billetes.")),

        // ---------- Matemática, 2º ano ----------
        new("EF02MA01", new("Comparar e ordenar números até centenas usando o valor posicional.", "Compare and order numbers up to the hundreds using place value.", "Comparar y ordenar números hasta las centenas usando el valor posicional.")),
        new("EF02MA04", new("Compor e decompor números de até três ordens.", "Compose and decompose numbers with up to three digits.", "Componer y descomponer números de hasta tres cifras.")),
        new("EF02MA05", new("Construir fatos básicos da adição e da subtração para o cálculo mental ou escrito.", "Build basic addition and subtraction facts for mental or written calculation.", "Construir hechos básicos de la suma y la resta para el cálculo mental o escrito.")),
        new("EF02MA06", new("Resolver problemas de adição e subtração com números de até três ordens.", "Solve addition and subtraction problems with numbers of up to three digits.", "Resolver problemas de suma y resta con números de hasta tres cifras.")),
        new("EF02MA07", new("Resolver problemas de multiplicação (por 2, 3, 4 e 5) como soma de parcelas iguais.", "Solve multiplication problems (by 2, 3, 4 and 5) as repeated addition.", "Resolver problemas de multiplicación (por 2, 3, 4 y 5) como suma de sumandos iguales.")),
        new("EF02MA08", new("Resolver problemas com dobro, metade, triplo e terça parte.", "Solve problems with double, half, triple and a third.", "Resolver problemas con doble, mitad, triple y tercera parte.")),
        new("EF02MA09", new("Construir sequências crescentes ou decrescentes a partir de uma regra.", "Build increasing or decreasing sequences from a rule.", "Construir secuencias crecientes o decrecientes a partir de una regla.")),
        new("EF02MA10", new("Descrever o padrão de sequências repetitivas e recursivas.", "Describe the pattern of repeating and recursive sequences.", "Describir el patrón de secuencias repetitivas y recursivas.")),
        new("EF02MA11", new("Descobrir os elementos que faltam em sequências repetitivas e recursivas.", "Find the missing elements in repeating and recursive sequences.", "Descubrir los elementos que faltan en secuencias repetitivas y recursivas.")),
        new("EF02MA19", new("Medir intervalos de tempo com relógio e registrar início e fim.", "Measure time intervals with a clock and record the start and end.", "Medir intervalos de tiempo con un reloj y registrar el inicio y el final.")),
        new("EF02MA20", new("Estabelecer equivalências entre moedas e cédulas em situações do dia a dia.", "Establish equivalences between coins and banknotes in everyday situations.", "Establecer equivalencias entre monedas y billetes en situaciones cotidianas.")),

        // ---------- Matemática, 3º ano ----------
        new("EF03MA02", new("Identificar características do sistema decimal compondo e decompondo números.", "Identify features of the decimal system by composing and decomposing numbers.", "Identificar características del sistema decimal componiendo y descomponiendo números.")),
        new("EF03MA03", new("Construir e usar fatos básicos da adição e da multiplicação.", "Build and use basic addition and multiplication facts.", "Construir y usar hechos básicos de la suma y la multiplicación.")),
        new("EF03MA06", new("Resolver problemas de adição e subtração (juntar, retirar, comparar, completar).", "Solve addition and subtraction problems (joining, taking away, comparing, completing).", "Resolver problemas de suma y resta (juntar, quitar, comparar, completar).")),
        new("EF03MA07", new("Resolver problemas de multiplicação (por 2, 3, 4, 5 e 10), inclusive em disposição retangular.", "Solve multiplication problems (by 2, 3, 4, 5 and 10), including rectangular arrays.", "Resolver problemas de multiplicación (por 2, 3, 4, 5 y 10), incluso en disposición rectangular.")),
        new("EF03MA08", new("Resolver problemas de divisão (até 10), com e sem resto.", "Solve division problems (up to 10), with and without remainders.", "Resolver problemas de división (hasta 10), con y sin resto.")),
        new("EF03MA10", new("Identificar regularidades em sequências de somas ou subtrações sucessivas.", "Identify regularities in sequences of repeated additions or subtractions.", "Identificar regularidades en secuencias de sumas o restas sucesivas.")),
        new("EF03MA11", new("Compreender a igualdade escrevendo contas diferentes com o mesmo resultado.", "Understand equality by writing different calculations with the same result.", "Comprender la igualdad escribiendo cuentas distintas con el mismo resultado.")),
        new("EF03MA23", new("Ler horas em relógios digitais e analógicos; relacionar horas, minutos e segundos.", "Read the time on digital and analog clocks; relate hours, minutes and seconds.", "Leer la hora en relojes digitales y analógicos; relacionar horas, minutos y segundos.")),
        new("EF03MA24", new("Resolver problemas de compra, venda e troca comparando valores em reais.", "Solve buying, selling and exchange problems by comparing money values.", "Resolver problemas de compra, venta y cambio comparando valores de dinero.")),

        // ---------- Matemática, 4º ano ----------
        new("EF04MA05", new("Usar as propriedades das operações para criar estratégias de cálculo.", "Use the properties of operations to create calculation strategies.", "Usar las propiedades de las operaciones para crear estrategias de cálculo.")),
        new("EF04MA06", new("Resolver problemas de multiplicação (parcelas iguais, organização retangular, proporcionalidade).", "Solve multiplication problems (equal groups, rectangular arrays, proportionality).", "Resolver problemas de multiplicación (sumandos iguales, organización rectangular, proporcionalidad).")),
        new("EF04MA07", new("Resolver problemas de divisão com divisor de até dois algarismos.", "Solve division problems with divisors of up to two digits.", "Resolver problemas de división con divisor de hasta dos cifras.")),
        new("EF04MA08", new("Resolver problemas simples de contagem combinando elementos de duas coleções.", "Solve simple counting problems by combining elements from two collections.", "Resolver problemas sencillos de conteo combinando elementos de dos colecciones.")),
        new("EF04MA09", new("Reconhecer frações unitárias (1/2, 1/3, 1/4, 1/5, 1/10, 1/100) na reta numérica.", "Recognize unit fractions (1/2, 1/3, 1/4, 1/5, 1/10, 1/100) on the number line.", "Reconocer fracciones unitarias (1/2, 1/3, 1/4, 1/5, 1/10, 1/100) en la recta numérica.")),
        new("EF04MA10", new("Estender o sistema decimal a décimos e centésimos, relacionando com o dinheiro.", "Extend the decimal system to tenths and hundredths, relating it to money.", "Extender el sistema decimal a décimas y centésimas, relacionándolo con el dinero.")),
        new("EF04MA11", new("Identificar regularidades em sequências de múltiplos de um número.", "Identify regularities in sequences of multiples of a number.", "Identificar regularidades en secuencias de múltiplos de un número.")),
        new("EF04MA14", new("Perceber que uma igualdade se mantém ao somar ou subtrair o mesmo número dos dois lados.", "Notice that an equation stays true when adding or subtracting the same number on both sides.", "Notar que una igualdad se mantiene al sumar o restar el mismo número en los dos lados.")),
        new("EF04MA15", new("Descobrir o número desconhecido que torna uma igualdade verdadeira.", "Find the unknown number that makes an equation true.", "Descubrir el número desconocido que hace verdadera una igualdad.")),
        new("EF04MA21", new("Medir e comparar áreas de figuras em malha quadriculada.", "Measure and compare areas of shapes on grid paper.", "Medir y comparar áreas de figuras en una cuadrícula.")),
        new("EF04MA22", new("Ler e registrar medidas e intervalos de tempo em horas, minutos e segundos.", "Read and record measurements and intervals of time in hours, minutes and seconds.", "Leer y registrar medidas e intervalos de tiempo en horas, minutos y segundos.")),
        new("EF04MA25", new("Resolver problemas de compra e venda com troco e desconto.", "Solve buying and selling problems with change and discounts.", "Resolver problemas de compra y venta con cambio y descuento.")),

        // ---------- Matemática, 5º ano ----------
        new("EF05MA02", new("Ler, escrever e ordenar números decimais.", "Read, write and order decimal numbers.", "Leer, escribir y ordenar números decimales.")),
        new("EF05MA03", new("Identificar e representar frações como parte de um todo ou resultado de divisão.", "Identify and represent fractions as part of a whole or the result of a division.", "Identificar y representar fracciones como parte de un todo o resultado de una división.")),
        new("EF05MA04", new("Identificar frações equivalentes.", "Identify equivalent fractions.", "Identificar fracciones equivalentes.")),
        new("EF05MA05", new("Comparar e ordenar frações e decimais, relacionando-os à reta numérica.", "Compare and order fractions and decimals, relating them to the number line.", "Comparar y ordenar fracciones y decimales, relacionándolos con la recta numérica.")),
        new("EF05MA06", new("Associar 10%, 25%, 50%, 75% e 100% a partes de um inteiro e calcular porcentagens.", "Relate 10%, 25%, 50%, 75% and 100% to parts of a whole and calculate percentages.", "Asociar 10 %, 25 %, 50 %, 75 % y 100 % a partes de un entero y calcular porcentajes.")),
        new("EF05MA07", new("Resolver problemas de adição e subtração com naturais e decimais.", "Solve addition and subtraction problems with whole numbers and decimals.", "Resolver problemas de suma y resta con naturales y decimales.")),
        new("EF05MA09", new("Resolver problemas simples de contagem pelo princípio multiplicativo.", "Solve simple counting problems using the multiplication principle.", "Resolver problemas sencillos de conteo con el principio multiplicativo.")),
        new("EF05MA10", new("Perceber que a igualdade se mantém ao operar igualmente os dois lados.", "Notice that an equation stays true when the same operation is done on both sides.", "Notar que la igualdad se mantiene al operar igual en los dos lados.")),
        new("EF05MA11", new("Resolver problemas que viram uma igualdade com um termo desconhecido.", "Solve problems that become an equation with an unknown term.", "Resolver problemas que se convierten en una igualdad con un término desconocido.")),
        new("EF05MA19", new("Resolver problemas com medidas de comprimento, área, massa, tempo e capacidade.", "Solve problems with measurements of length, area, mass, time and capacity.", "Resolver problemas con medidas de longitud, área, masa, tiempo y capacidad.")),
        new("EF05MA20", new("Concluir que figuras de mesmo perímetro podem ter áreas diferentes, e vice-versa.", "Conclude that shapes with the same perimeter can have different areas, and vice versa.", "Concluir que figuras con el mismo perímetro pueden tener áreas distintas, y viceversa.")),

        // ---------- Matemática, 6º ano ----------
        new("EF06MA03", new("Resolver problemas com cálculos de números naturais por estratégias variadas.", "Solve problems with whole-number calculations using varied strategies.", "Resolver problemas con cálculos de números naturales con estrategias variadas.")),
        new("EF06MA04", new("Construir algoritmos em linguagem natural e representá-los por fluxograma.", "Build algorithms in natural language and represent them with flowcharts.", "Construir algoritmos en lenguaje natural y representarlos con diagramas de flujo.")),
        new("EF06MA05", new("Classificar números em primos e compostos; múltiplos, divisores e divisibilidade.", "Classify numbers as prime or composite; multiples, divisors and divisibility.", "Clasificar números en primos y compuestos; múltiplos, divisores y divisibilidad.")),
        new("EF06MA06", new("Resolver problemas com as ideias de múltiplo e divisor.", "Solve problems with the ideas of multiple and divisor.", "Resolver problemas con las ideas de múltiplo y divisor.")),
        new("EF06MA09", new("Calcular a fração de uma quantidade.", "Calculate a fraction of a quantity.", "Calcular la fracción de una cantidad.")),
        new("EF06MA10", new("Resolver problemas de adição e subtração de frações.", "Solve problems adding and subtracting fractions.", "Resolver problemas de suma y resta de fracciones.")),
        new("EF06MA13", new("Resolver problemas de porcentagem pela ideia de proporcionalidade.", "Solve percentage problems using proportionality.", "Resolver problemas de porcentaje con la idea de proporcionalidad.")),
        new("EF06MA14", new("Usar as propriedades da igualdade para encontrar valores desconhecidos.", "Use the properties of equality to find unknown values.", "Usar las propiedades de la igualdad para encontrar valores desconocidos.")),

        // ---------- Matemática, 7º ano ----------
        new("EF07MA01", new("Resolver problemas com múltiplos e divisores, incluindo MMC e MDC.", "Solve problems with multiples and divisors, including LCM and GCD.", "Resolver problemas con múltiplos y divisores, incluidos MCM y MCD.")),
        new("EF07MA02", new("Resolver problemas de porcentagem com acréscimos e decréscimos.", "Solve percentage problems with increases and decreases.", "Resolver problemas de porcentaje con aumentos y disminuciones.")),
        new("EF07MA03", new("Comparar e ordenar números inteiros e usá-los em adições e subtrações.", "Compare and order integers and use them in additions and subtractions.", "Comparar y ordenar números enteros y usarlos en sumas y restas.")),
        new("EF07MA04", new("Resolver problemas com operações de números inteiros.", "Solve problems with operations on integers.", "Resolver problemas con operaciones de números enteros.")),
        new("EF07MA07", new("Representar por fluxograma os passos para resolver um grupo de problemas.", "Represent with a flowchart the steps to solve a group of problems.", "Representar con un diagrama de flujo los pasos para resolver un grupo de problemas.")),
        new("EF07MA11", new("Compreender a multiplicação e a divisão de números racionais.", "Understand multiplication and division of rational numbers.", "Comprender la multiplicación y la división de números racionales.")),
        new("EF07MA12", new("Resolver problemas com operações de números racionais.", "Solve problems with operations on rational numbers.", "Resolver problemas con operaciones de números racionales.")),
        new("EF07MA13", new("Usar letra ou símbolo como variável para expressar relações.", "Use a letter or symbol as a variable to express relationships.", "Usar una letra o símbolo como variable para expresar relaciones.")),
        new("EF07MA14", new("Classificar sequências em recursivas e não recursivas.", "Classify sequences as recursive or non-recursive.", "Clasificar secuencias en recursivas y no recursivas.")),
        new("EF07MA15", new("Usar a linguagem algébrica para expressar regularidades de sequências.", "Use algebraic language to express regularities in sequences.", "Usar el lenguaje algebraico para expresar regularidades de secuencias.")),
        new("EF07MA17", new("Resolver problemas de proporcionalidade direta e inversa.", "Solve direct and inverse proportion problems.", "Resolver problemas de proporcionalidad directa e inversa.")),
        new("EF07MA18", new("Resolver problemas com equações do 1º grau (ax + b = c).", "Solve problems with linear equations (ax + b = c).", "Resolver problemas con ecuaciones de primer grado (ax + b = c).")),

        // ---------- Matemática, 8º ano ----------
        new("EF08MA01", new("Calcular potências de expoente inteiro e usar notação científica.", "Calculate powers with integer exponents and use scientific notation.", "Calcular potencias de exponente entero y usar notación científica.")),
        new("EF08MA02", new("Resolver problemas usando a relação entre potenciação e radiciação.", "Solve problems using the relationship between powers and roots.", "Resolver problemas usando la relación entre potenciación y radicación.")),
        new("EF08MA03", new("Resolver problemas de contagem pelo princípio multiplicativo.", "Solve counting problems using the multiplication principle.", "Resolver problemas de conteo con el principio multiplicativo.")),
        new("EF08MA04", new("Resolver problemas com cálculo de porcentagens.", "Solve problems involving percentage calculations.", "Resolver problemas con cálculo de porcentajes.")),
        new("EF08MA08", new("Resolver problemas com sistemas de equações do 1º grau com duas incógnitas.", "Solve problems with systems of linear equations in two unknowns.", "Resolver problemas con sistemas de ecuaciones de primer grado con dos incógnitas.")),
        new("EF08MA09", new("Resolver problemas com equações do 2º grau do tipo ax² = b.", "Solve problems with quadratic equations of the form ax² = b.", "Resolver problemas con ecuaciones de segundo grado del tipo ax² = b.")),
        new("EF08MA11", new("Identificar a regularidade de sequências recursivas e descrevê-la por um algoritmo.", "Identify the regularity of recursive sequences and describe it with an algorithm.", "Identificar la regularidad de secuencias recursivas y describirla con un algoritmo.")),
        new("EF08MA13", new("Resolver problemas com grandezas direta ou inversamente proporcionais.", "Solve problems with directly or inversely proportional quantities.", "Resolver problemas con magnitudes directa o inversamente proporcionales.")),

        // ---------- Matemática, 9º ano ----------
        new("EF09MA05", new("Resolver problemas de porcentagens sucessivas e taxas, no contexto da educação financeira.", "Solve problems with successive percentages and rates, in a financial education context.", "Resolver problemas de porcentajes sucesivos y tasas, en el contexto de la educación financiera.")),
        new("EF09MA09", new("Usar fatoração e produtos notáveis para resolver equações do 2º grau.", "Use factoring and special products to solve quadratic equations.", "Usar factorización y productos notables para resolver ecuaciones de segundo grado.")),
        new("EF09MA13", new("Demonstrar relações métricas do triângulo retângulo, entre elas o teorema de Pitágoras.", "Prove metric relations in the right triangle, including the Pythagorean theorem.", "Demostrar relaciones métricas del triángulo rectángulo, entre ellas el teorema de Pitágoras.")),
        new("EF09MA14", new("Resolver problemas de aplicação do teorema de Pitágoras.", "Solve problems applying the Pythagorean theorem.", "Resolver problemas de aplicación del teorema de Pitágoras.")),
        new("EF09MA16", new("Calcular ponto médio e distância entre dois pontos no plano cartesiano.", "Calculate the midpoint and the distance between two points on the Cartesian plane.", "Calcular el punto medio y la distancia entre dos puntos en el plano cartesiano.")),

        // ---------- Matemática, Ensino Médio ----------
        new("EM13MAT303", new("Comparar situações de juros simples e juros compostos.", "Compare situations of simple and compound interest.", "Comparar situaciones de interés simple e interés compuesto.")),
        new("EM13MAT310", new("Resolver problemas de contagem (agrupamentos ordenáveis ou não) pelos princípios multiplicativo e aditivo.", "Solve counting problems (ordered or unordered groupings) using the multiplication and addition principles.", "Resolver problemas de conteo (agrupaciones ordenables o no) con los principios multiplicativo y aditivo.")),

        // ---------- Computação (Pensamento Computacional) ----------
        new("EF01CO01", new("Organizar objetos por características, explicitando semelhanças (padrões) e diferenças.", "Organize objects by their features, pointing out similarities (patterns) and differences.", "Organizar objetos por características, señalando semejanzas (patrones) y diferencias.")),
        new("EF02CO01", new("Criar e comparar modelos de objetos, identificando padrões e atributos essenciais.", "Create and compare models of objects, identifying patterns and essential attributes.", "Crear y comparar modelos de objetos, identificando patrones y atributos esenciales.")),
        new("EF03CO01", new("Associar verdadeiro ou falso a frases do dia a dia, inclusive com negação.", "Assign true or false to everyday sentences, including with negation.", "Asociar verdadero o falso a frases cotidianas, incluso con negación.")),
        new("EF05CO03", new("Realizar negação, conjunção (E) e disjunção (OU) com sentenças verdadeiras ou falsas.", "Apply negation, conjunction (AND) and disjunction (OR) to true or false sentences.", "Realizar negación, conjunción (Y) y disyunción (O) con frases verdaderas o falsas.")),
        new("EF05CO04", new("Criar e simular algoritmos com sequências, repetições e decisões (seleções condicionais).", "Create and simulate algorithms with sequences, loops and decisions (conditional selection).", "Crear y simular algoritmos con secuencias, repeticiones y decisiones (selecciones condicionales).")),
        new("EF06CO02", new("Elaborar algoritmos com instruções sequenciais, de repetição e de seleção.", "Write algorithms with sequential, repetition and selection instructions.", "Elaborar algoritmos con instrucciones secuenciales, de repetición y de selección.")),
        new("EF07CO02", new("Analisar programas para encontrar e corrigir erros.", "Analyze programs to find and fix errors.", "Analizar programas para encontrar y corregir errores.")),
    ];

    /// <summary>Habilidade do jogo (slug) → códigos BNCC relacionados. Lista vazia = sem correspondência direta.</summary>
    public static IReadOnlyDictionary<string, string[]> BySkill { get; } = new Dictionary<string, string[]>
    {
        // Matemática — Iniciante
        ["contagem-comparacao"] = ["EF01MA01", "EF01MA04", "EF01MA05", "EF02MA01"],
        ["dezenas-unidades"] = ["EF01MA07", "EF02MA04", "EF03MA02"],
        ["adicao-ate-100"] = ["EF01MA06", "EF01MA08", "EF02MA05", "EF02MA06"],
        ["subtracao-ate-100"] = ["EF01MA08", "EF02MA05", "EF02MA06", "EF03MA06"],
        ["multiplicacao-grupos"] = ["EF02MA07", "EF02MA08", "EF03MA07"],
        ["dinheiro-e-horas"] = ["EF01MA19", "EF02MA19", "EF02MA20", "EF03MA23", "EF03MA24", "EF04MA22", "EF04MA25"],

        // Matemática — Médio
        ["tabuada-completa"] = ["EF03MA03", "EF03MA07", "EF04MA06", "EF04MA15"],
        ["divisao"] = ["EF03MA08", "EF04MA07"],
        ["expressoes"] = ["EF04MA05", "EF06MA03"],
        ["fracoes-basicas"] = ["EF04MA09", "EF05MA03", "EF05MA04", "EF05MA05", "EF06MA09", "EF06MA10"],
        ["decimais-porcentagem"] = ["EF04MA10", "EF05MA02", "EF05MA06", "EF05MA07", "EF06MA13"],
        ["perimetro-area"] = ["EF04MA21", "EF05MA19", "EF05MA20"],

        // Matemática — Avançado
        ["numeros-inteiros"] = ["EF07MA03", "EF07MA04"],
        ["potencias-raizes"] = ["EF08MA01", "EF08MA02"],
        ["mmc-mdc"] = ["EF06MA05", "EF06MA06", "EF07MA01"],
        ["fracoes-operacoes"] = ["EF06MA10", "EF07MA11", "EF07MA12"],
        ["proporcao-porcentagem"] = ["EF07MA02", "EF07MA17", "EF08MA04", "EF08MA13"],
        ["equacoes-1-grau"] = ["EF06MA14", "EF07MA18"],

        // Matemática — Expert
        ["sistemas-equacoes"] = ["EF08MA08"],
        ["equacoes-2-grau"] = ["EF08MA09", "EF09MA09"],
        ["juros-porcentagem"] = ["EF09MA05", "EM13MAT303"],
        ["geometria-pitagoras"] = ["EF09MA13", "EF09MA14", "EF09MA16"],
        ["combinatoria"] = ["EF04MA08", "EF05MA09", "EF08MA03", "EM13MAT310"],

        // Lógica — Iniciante
        ["padroes-visuais"] = ["EF01MA10", "EF02MA10", "EF02MA11", "EF01CO01"],
        ["sequencias-simples"] = ["EF02MA09", "EF02MA11", "EF03MA10", "EF04MA11"],
        ["qual-e-diferente"] = ["EF01MA09", "EF01CO01", "EF02CO01"],
        ["maior-menor-igual"] = ["EF01MA05", "EF02MA01", "EF03MA11"],

        // Lógica — Médio
        ["sequencias-compostas"] = ["EF07MA14", "EF07MA15", "EF08MA11"],
        ["grades-numericas"] = ["EF03MA03", "EF04MA15"],
        ["verdadeiro-ou-falso"] = ["EF03CO01", "EF05CO03"],
        ["balancas"] = ["EF04MA14", "EF05MA10", "EF06MA14"],
        ["tabelas-logicas"] = [],

        // Lógica — Avançado
        ["sudoku-6x6"] = [],
        ["proposicoes"] = ["EF03CO01", "EF05CO03"],
        ["conjuntos"] = [],
        ["enigmas-simbolos"] = ["EF04MA15", "EF05MA11", "EF07MA13"],
        ["quem-e-quem"] = [],

        // Lógica — Expert
        ["tabelas-verdade"] = ["EF05CO03"],
        ["cavaleiros-mentirosos"] = ["EF03CO01", "EF05CO03"],
        ["pensamento-computacional"] = ["EF05CO04", "EF06CO02", "EF06MA04", "EF07MA07", "EF07CO02"],
        ["desafios-olimpicos"] = [],
    };

    public static string[] ForSkill(string slug) => BySkill.TryGetValue(slug, out var codes) ? codes : [];
}
