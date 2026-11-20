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
            ]),

        new("logica", "Lógica", "🧩", 2,
            Skills:
            [
                new("padroes-visuais", "Padrões visuais", "Descobrir o pedaço que se repete numa sequência de figuras.", "iniciante"),
                new("sequencias-simples", "Sequências de números", "Encontrar a regra de uma sequência numérica.", "iniciante"),
                new("qual-e-diferente", "Qual é o diferente?", "Classificar e achar o item que não segue o grupo.", "iniciante"),
                new("maior-menor-igual", "Maior, menor ou igual", "Comparar números e contas usando >, < e =.", "iniciante"),
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
            ]),
    ];
}
