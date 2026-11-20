namespace EduGame.Problems.MathSkills;

/// <summary>Medidas do dia a dia: dinheiro (soma e troco) e horas (relógio e minutos).</summary>
public sealed class MoneyAndTimeGenerator : ProblemGenerator
{
    public override string Key => "mat.dinheiro-horas";
    public override string SkillSlug => "dinheiro-e-horas";

    public override GeneratedProblem Generate(Random rng) => rng.Next(6) switch
    {
        0 => MoneyGain(rng),
        1 => Change(rng),
        2 => HoursLater(rng),
        3 => Duration(rng),
        4 => Minutes(rng),
        _ => Coins(rng),
    };

    private static GeneratedProblem MoneyGain(Random rng)
    {
        var a = Between(rng, 1, 20);
        var b = Between(rng, 1, 20);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Você tinha R$ {a} e ganhou R$ {b}. Com quantos reais você ficou?",
            (a + b).ToString(),
            ["Ganhar dinheiro é somar.", $"{a} + {b} = ?"],
            $"R$ {a} + R$ {b} = R$ {a + b}");
        return new(content, 250 + (a + b) * 8);
    }

    private static GeneratedProblem Change(Random rng)
    {
        var (note, price, difficulty) = rng.Next(3) switch
        {
            0 => (10, Between(rng, 1, 9), 500),
            1 => (20, Between(rng, 11, 19), 650),
            _ => (50, Between(rng, 12, 48), 850),
        };
        var item = Pick(rng, ["um suco", "um lanche", "um caderno", "um brinquedo", "um livro"]);
        var change = note - price;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Você comprou {item} de R$ {price} e pagou com uma nota de R$ {note}. Quanto recebeu de troco?",
            change.ToString(),
            ["Troco é o que sobra: valor pago menos o preço.", $"Quanto falta de {price} para chegar em {note}?"],
            $"R$ {note} − R$ {price} = R$ {change}");
        return new(content, difficulty);
    }

    private static GeneratedProblem HoursLater(Random rng)
    {
        var wraps = rng.Next(3) == 0;
        int h, k;
        if (wraps) { h = Between(rng, 9, 11); k = Between(rng, 2, 5); }
        else { h = Between(rng, 1, 8); k = Between(rng, 1, 12 - h); }

        var answer = (h + k - 1) % 12 + 1;
        var hints = new List<string> { $"Comece no {h} e avance {k} horas no relógio." };
        if (wraps) hints.Add("Depois do 12, o relógio de ponteiros volta para o 1.");
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"No relógio de ponteiros são {h} horas. Que horas o relógio vai marcar daqui a {k} horas?",
            answer.ToString(),
            hints,
            wraps ? $"{h} + {k} = {h + k}; passando do 12, o relógio marca {answer}." : $"{h} + {k} = {answer}");
        return new(content, wraps ? 800 : 400 + k * 15);
    }

    private static GeneratedProblem Duration(Random rng)
    {
        var start = Between(rng, 1, 8);
        var end = Between(rng, start + 1, 11);
        var what = Pick(rng, ["Um passeio", "Uma festa", "Um jogo de futebol", "Uma aula de natação"]);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"{what} começou às {start} horas e terminou às {end} horas. Quantas horas durou?",
            (end - start).ToString(),
            [$"Conte quantas horas há do {start} até o {end}.", $"{end} − {start} = ?"],
            $"{end} − {start} = {end - start} horas");
        return new(content, 600);
    }

    private static GeneratedProblem Minutes(Random rng)
    {
        return rng.Next(3) switch
        {
            0 => Make($"Quantos minutos há em {Hours(out var k)}?", 60 * k, ["1 hora tem 60 minutos.", $"{string.Join(" + ", Enumerable.Repeat(60, k))} = ?"], 700 + k * 50),
            1 => Make("Quantos minutos há em meia hora?", 30, ["1 hora tem 60 minutos.", "Meia hora é a metade de 1 hora."], 750),
            _ => Make("Quantos minutos há em 1 hora e meia?", 90, ["1 hora tem 60 minutos e meia hora tem 30.", "60 + 30 = ?"], 950),
        };

        string Hours(out int hours)
        {
            hours = Between(rng, 1, 3);
            return hours == 1 ? "1 hora" : $"{hours} horas";
        }

        static GeneratedProblem Make(string prompt, int answer, List<string> hints, int difficulty) =>
            new(new ProblemContent(ProblemTypes.Numero, prompt, answer.ToString(), hints, $"A resposta é {answer} minutos."), difficulty);
    }

    private static GeneratedProblem Coins(Random rng)
    {
        var reais = Between(rng, 1, 5);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            $"Quantas moedas de 50 centavos são necessárias para formar R$ {reais},00?",
            (reais * 2).ToString(),
            ["Duas moedas de 50 centavos formam R$ 1,00.", $"Para R$ {reais},00 você precisa de 2 moedas para cada real."],
            $"{reais} × 2 = {reais * 2} moedas");
        return new(content, 850 + reais * 30);
    }
}
