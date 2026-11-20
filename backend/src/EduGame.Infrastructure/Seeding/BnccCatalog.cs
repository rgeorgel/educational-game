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
    public sealed record Habilidade(string Code, string Summary)
    {
        public string Component => Code.Contains("CO") ? "Computação" : "Matemática";

        /// <summary>"3º ano" (EF03…) ou "Ensino Médio" (EM13…).</summary>
        public string Grade => Code.StartsWith("EM") ? "Ensino Médio" : $"{int.Parse(Code.AsSpan(2, 2))}º ano";

        /// <summary>1–9 para o Ensino Fundamental, 10 para o Ensino Médio (para ordenar).</summary>
        public int GradeOrder => Code.StartsWith("EM") ? 10 : int.Parse(Code.AsSpan(2, 2));
    }

    public static IReadOnlyList<Habilidade> Habilidades { get; } =
    [
        // ---------- Matemática, 1º ano ----------
        new("EF01MA01", "Usar números naturais para indicar quantidade ou ordem no dia a dia."),
        new("EF01MA04", "Contar coleções de até 100 objetos e registrar o resultado."),
        new("EF01MA05", "Comparar números naturais de até duas ordens, com ou sem reta numérica."),
        new("EF01MA06", "Construir fatos básicos da adição e usá-los no cálculo."),
        new("EF01MA07", "Compor e decompor números de até duas ordens."),
        new("EF01MA08", "Resolver problemas de adição e subtração com números de até dois algarismos."),
        new("EF01MA09", "Organizar e ordenar objetos por atributos como cor, forma e tamanho."),
        new("EF01MA10", "Descobrir os elementos que faltam em sequências de números, objetos ou figuras."),
        new("EF01MA19", "Reconhecer e relacionar valores de moedas e cédulas do real."),

        // ---------- Matemática, 2º ano ----------
        new("EF02MA01", "Comparar e ordenar números até centenas usando o valor posicional."),
        new("EF02MA04", "Compor e decompor números de até três ordens."),
        new("EF02MA05", "Construir fatos básicos da adição e da subtração para o cálculo mental ou escrito."),
        new("EF02MA06", "Resolver problemas de adição e subtração com números de até três ordens."),
        new("EF02MA07", "Resolver problemas de multiplicação (por 2, 3, 4 e 5) como soma de parcelas iguais."),
        new("EF02MA08", "Resolver problemas com dobro, metade, triplo e terça parte."),
        new("EF02MA09", "Construir sequências crescentes ou decrescentes a partir de uma regra."),
        new("EF02MA10", "Descrever o padrão de sequências repetitivas e recursivas."),
        new("EF02MA11", "Descobrir os elementos que faltam em sequências repetitivas e recursivas."),
        new("EF02MA19", "Medir intervalos de tempo com relógio e registrar início e fim."),
        new("EF02MA20", "Estabelecer equivalências entre moedas e cédulas em situações do dia a dia."),

        // ---------- Matemática, 3º ano ----------
        new("EF03MA02", "Identificar características do sistema decimal compondo e decompondo números."),
        new("EF03MA03", "Construir e usar fatos básicos da adição e da multiplicação."),
        new("EF03MA06", "Resolver problemas de adição e subtração (juntar, retirar, comparar, completar)."),
        new("EF03MA07", "Resolver problemas de multiplicação (por 2, 3, 4, 5 e 10), inclusive em disposição retangular."),
        new("EF03MA08", "Resolver problemas de divisão (até 10), com e sem resto."),
        new("EF03MA10", "Identificar regularidades em sequências de somas ou subtrações sucessivas."),
        new("EF03MA11", "Compreender a igualdade escrevendo contas diferentes com o mesmo resultado."),
        new("EF03MA23", "Ler horas em relógios digitais e analógicos; relacionar horas, minutos e segundos."),
        new("EF03MA24", "Resolver problemas de compra, venda e troca comparando valores em reais."),

        // ---------- Matemática, 4º ano ----------
        new("EF04MA05", "Usar as propriedades das operações para criar estratégias de cálculo."),
        new("EF04MA06", "Resolver problemas de multiplicação (parcelas iguais, organização retangular, proporcionalidade)."),
        new("EF04MA07", "Resolver problemas de divisão com divisor de até dois algarismos."),
        new("EF04MA08", "Resolver problemas simples de contagem combinando elementos de duas coleções."),
        new("EF04MA09", "Reconhecer frações unitárias (1/2, 1/3, 1/4, 1/5, 1/10, 1/100) na reta numérica."),
        new("EF04MA10", "Estender o sistema decimal a décimos e centésimos, relacionando com o dinheiro."),
        new("EF04MA11", "Identificar regularidades em sequências de múltiplos de um número."),
        new("EF04MA14", "Perceber que uma igualdade se mantém ao somar ou subtrair o mesmo número dos dois lados."),
        new("EF04MA15", "Descobrir o número desconhecido que torna uma igualdade verdadeira."),
        new("EF04MA21", "Medir e comparar áreas de figuras em malha quadriculada."),
        new("EF04MA22", "Ler e registrar medidas e intervalos de tempo em horas, minutos e segundos."),
        new("EF04MA25", "Resolver problemas de compra e venda com troco e desconto."),

        // ---------- Matemática, 5º ano ----------
        new("EF05MA02", "Ler, escrever e ordenar números decimais."),
        new("EF05MA03", "Identificar e representar frações como parte de um todo ou resultado de divisão."),
        new("EF05MA04", "Identificar frações equivalentes."),
        new("EF05MA05", "Comparar e ordenar frações e decimais, relacionando-os à reta numérica."),
        new("EF05MA06", "Associar 10%, 25%, 50%, 75% e 100% a partes de um inteiro e calcular porcentagens."),
        new("EF05MA07", "Resolver problemas de adição e subtração com naturais e decimais."),
        new("EF05MA09", "Resolver problemas simples de contagem pelo princípio multiplicativo."),
        new("EF05MA10", "Perceber que a igualdade se mantém ao operar igualmente os dois lados."),
        new("EF05MA11", "Resolver problemas que viram uma igualdade com um termo desconhecido."),
        new("EF05MA19", "Resolver problemas com medidas de comprimento, área, massa, tempo e capacidade."),
        new("EF05MA20", "Concluir que figuras de mesmo perímetro podem ter áreas diferentes, e vice-versa."),

        // ---------- Matemática, 6º ano ----------
        new("EF06MA03", "Resolver problemas com cálculos de números naturais por estratégias variadas."),
        new("EF06MA04", "Construir algoritmos em linguagem natural e representá-los por fluxograma."),
        new("EF06MA05", "Classificar números em primos e compostos; múltiplos, divisores e divisibilidade."),
        new("EF06MA06", "Resolver problemas com as ideias de múltiplo e divisor."),
        new("EF06MA09", "Calcular a fração de uma quantidade."),
        new("EF06MA10", "Resolver problemas de adição e subtração de frações."),
        new("EF06MA13", "Resolver problemas de porcentagem pela ideia de proporcionalidade."),
        new("EF06MA14", "Usar as propriedades da igualdade para encontrar valores desconhecidos."),

        // ---------- Matemática, 7º ano ----------
        new("EF07MA01", "Resolver problemas com múltiplos e divisores, incluindo MMC e MDC."),
        new("EF07MA02", "Resolver problemas de porcentagem com acréscimos e decréscimos."),
        new("EF07MA03", "Comparar e ordenar números inteiros e usá-los em adições e subtrações."),
        new("EF07MA04", "Resolver problemas com operações de números inteiros."),
        new("EF07MA07", "Representar por fluxograma os passos para resolver um grupo de problemas."),
        new("EF07MA11", "Compreender a multiplicação e a divisão de números racionais."),
        new("EF07MA12", "Resolver problemas com operações de números racionais."),
        new("EF07MA13", "Usar letra ou símbolo como variável para expressar relações."),
        new("EF07MA14", "Classificar sequências em recursivas e não recursivas."),
        new("EF07MA15", "Usar a linguagem algébrica para expressar regularidades de sequências."),
        new("EF07MA17", "Resolver problemas de proporcionalidade direta e inversa."),
        new("EF07MA18", "Resolver problemas com equações do 1º grau (ax + b = c)."),

        // ---------- Matemática, 8º ano ----------
        new("EF08MA01", "Calcular potências de expoente inteiro e usar notação científica."),
        new("EF08MA02", "Resolver problemas usando a relação entre potenciação e radiciação."),
        new("EF08MA03", "Resolver problemas de contagem pelo princípio multiplicativo."),
        new("EF08MA04", "Resolver problemas com cálculo de porcentagens."),
        new("EF08MA08", "Resolver problemas com sistemas de equações do 1º grau com duas incógnitas."),
        new("EF08MA09", "Resolver problemas com equações do 2º grau do tipo ax² = b."),
        new("EF08MA11", "Identificar a regularidade de sequências recursivas e descrevê-la por um algoritmo."),
        new("EF08MA13", "Resolver problemas com grandezas direta ou inversamente proporcionais."),

        // ---------- Matemática, 9º ano ----------
        new("EF09MA05", "Resolver problemas de porcentagens sucessivas e taxas, no contexto da educação financeira."),
        new("EF09MA09", "Usar fatoração e produtos notáveis para resolver equações do 2º grau."),
        new("EF09MA13", "Demonstrar relações métricas do triângulo retângulo, entre elas o teorema de Pitágoras."),
        new("EF09MA14", "Resolver problemas de aplicação do teorema de Pitágoras."),
        new("EF09MA16", "Calcular ponto médio e distância entre dois pontos no plano cartesiano."),

        // ---------- Matemática, Ensino Médio ----------
        new("EM13MAT303", "Comparar situações de juros simples e juros compostos."),
        new("EM13MAT310", "Resolver problemas de contagem (agrupamentos ordenáveis ou não) pelos princípios multiplicativo e aditivo."),

        // ---------- Computação (Pensamento Computacional) ----------
        new("EF01CO01", "Organizar objetos por características, explicitando semelhanças (padrões) e diferenças."),
        new("EF02CO01", "Criar e comparar modelos de objetos, identificando padrões e atributos essenciais."),
        new("EF03CO01", "Associar verdadeiro ou falso a frases do dia a dia, inclusive com negação."),
        new("EF05CO03", "Realizar negação, conjunção (E) e disjunção (OU) com sentenças verdadeiras ou falsas."),
        new("EF05CO04", "Criar e simular algoritmos com sequências, repetições e decisões (seleções condicionais)."),
        new("EF06CO02", "Elaborar algoritmos com instruções sequenciais, de repetição e de seleção."),
        new("EF07CO02", "Analisar programas para encontrar e corrigir erros."),
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
