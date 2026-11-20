using EduGame.Problems.LogicSkills;
using EduGame.Problems.MathSkills;

namespace EduGame.Problems;

/// <summary>Registro dos geradores e escolha do problema mais próximo da dificuldade desejada.</summary>
public sealed class ProblemFactory
{
    private const int Candidates = 16;
    private readonly Dictionary<string, IProblemGenerator> _byKey;

    public ProblemFactory(IEnumerable<IProblemGenerator> generators)
    {
        _byKey = generators.ToDictionary(g => g.Key);
    }

    public static ProblemFactory Default { get; } = new(
    [
        new CountingGenerator(),
        new AdditionGenerator(),
        new SubtractionGenerator(),
        new PlaceValueGenerator(),
        new MultiplicationGroupsGenerator(),
        new MoneyAndTimeGenerator(),
        new NumberSequenceGenerator(),
        new VisualPatternGenerator(),
        new OddOneOutGenerator(),
        new ComparisonGenerator(),

        // Médio
        new TimesTableGenerator(),
        new DivisionGenerator(),
        new ExpressionsGenerator(),
        new FractionsGenerator(),
        new DecimalsPercentGenerator(),
        new PerimeterAreaGenerator(),
        new CompositeSequenceGenerator(),
        new NumberGridGenerator(),
        new TrueFalseGenerator(),
        new BalanceGenerator(),
        new LogicTableGenerator(),

        // Avançado
        new IntegersGenerator(),
        new PowersRootsGenerator(),
        new GcdLcmGenerator(),
        new FractionOperationsGenerator(),
        new ProportionGenerator(),
        new LinearEquationsGenerator(),
        new Sudoku6Generator(),
        new PropositionsGenerator(),
        new SetsGenerator(),
        new SymbolPuzzleGenerator(),
        new WhoIsWhoGenerator(),

        // Expert
        new SystemsGenerator(),
        new QuadraticGenerator(),
        new InterestGenerator(),
        new PythagorasGenerator(),
        new CombinatoricsGenerator(),
        new TruthTableGenerator(),
        new KnightsKnavesGenerator(),
        new ComputationalThinkingGenerator(),
    ]);

    public IReadOnlyCollection<IProblemGenerator> Generators => _byKey.Values;

    public IProblemGenerator Get(string key) =>
        _byKey.TryGetValue(key, out var g) ? g : throw new KeyNotFoundException($"Gerador '{key}' não existe.");

    public IEnumerable<IProblemGenerator> ForSkill(string skillSlug) => _byKey.Values.Where(g => g.SkillSlug == skillSlug);

    /// <summary>
    /// Gera vários candidatos com a mesma semente e devolve o de dificuldade mais próxima do alvo.
    /// A mesma (key, alvo, semente, evitar) sempre produz o mesmo problema.
    /// </summary>
    public GeneratedProblem Generate(string key, int targetDifficulty, int seed, IReadOnlySet<string>? avoidPrompts = null)
    {
        var generator = Get(key);
        var rng = new Random(seed);

        GeneratedProblem? best = null;
        GeneratedProblem? bestRepeated = null;
        for (var i = 0; i < Candidates; i++)
        {
            var p = generator.Generate(rng);
            var repeated = avoidPrompts?.Contains(Fingerprint(p.Content)) == true;
            ref var slot = ref repeated ? ref bestRepeated : ref best;
            if (slot is null || Distance(p) < Distance(slot)) slot = p;
        }
        return best ?? bestRepeated!;

        int Distance(GeneratedProblem p) => Math.Abs(p.Difficulty - targetDifficulty);
    }

    /// <summary>Identifica um problema para evitar repetições na mesma sessão.</summary>
    public static string Fingerprint(ProblemContent c) =>
        $"{c.Prompt}|{c.Visual}|{c.Code}|{string.Join(',', c.Sequence ?? [])}";

    /// <summary>Chave dos problemas curados (escritos à mão, guardados no banco).</summary>
    public const string CuratedKey = "curado";
}
