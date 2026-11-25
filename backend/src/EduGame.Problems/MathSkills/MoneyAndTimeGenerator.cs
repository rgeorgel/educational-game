using static EduGame.Problems.Lang;

namespace EduGame.Problems.MathSkills;

/// <summary>Medidas do dia a dia: dinheiro (soma e troco) e horas (relógio e minutos).</summary>
public sealed class MoneyAndTimeGenerator : ProblemGenerator
{
    public override string Key => "mat.dinheiro-horas";
    public override string SkillSlug => "dinheiro-e-horas";

    /// <summary>Valor em dinheiro: "R$ 5" em português, "$5" em inglês e espanhol.</summary>
    private static string Money(int value) => Tr($"R$ {value}", $"${value}", $"${value}");

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
            Tr($"Você tinha R$ {a} e ganhou R$ {b}. Com quantos reais você ficou?",
               $"You had ${a} and got ${b} more. How many dollars do you have now?",
               $"Tenías ${a} y te dieron ${b}. ¿Cuánto dinero tienes ahora?"),
            (a + b).ToString(),
            [Tr("Ganhar dinheiro é somar.", "Getting money means adding.", "Recibir dinero es sumar."), $"{a} + {b} = ?"],
            $"{Money(a)} + {Money(b)} = {Money(a + b)}");
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
        var item = Pick(rng, Current switch
        {
            En => ["a juice", "a snack", "a notebook", "a toy", "a book"],
            Es => ["un jugo", "una merienda", "un cuaderno", "un juguete", "un libro"],
            _ => (string[])["um suco", "um lanche", "um caderno", "um brinquedo", "um livro"],
        });
        var change = note - price;
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"Você comprou {item} de R$ {price} e pagou com uma nota de R$ {note}. Quanto recebeu de troco?",
               $"You bought {item} for ${price} and paid with a ${note} bill. How much change did you get?",
               $"Compraste {item} de ${price} y pagaste con un billete de ${note}. ¿Cuánto te dieron de cambio?"),
            change.ToString(),
            [
                Tr("Troco é o que sobra: valor pago menos o preço.", "Change is what's left over: the amount paid minus the price.", "El cambio es lo que sobra: lo que pagaste menos el precio."),
                Tr($"Quanto falta de {price} para chegar em {note}?", $"How much do you need to get from {price} to {note}?", $"¿Cuánto falta de {price} para llegar a {note}?"),
            ],
            $"{Money(note)} − {Money(price)} = {Money(change)}");
        return new(content, difficulty);
    }

    private static GeneratedProblem HoursLater(Random rng)
    {
        var wraps = rng.Next(3) == 0;
        int h, k;
        if (wraps) { h = Between(rng, 9, 11); k = Between(rng, 2, 5); }
        else { h = Between(rng, 1, 8); k = Between(rng, 1, 12 - h); }

        var answer = (h + k - 1) % 12 + 1;
        var hints = new List<string> { Tr($"Comece no {h} e avance {k} horas no relógio.", $"Start at {h} and move {k} hours forward on the clock.", $"Empieza en el {h} y avanza {k} horas en el reloj.") };
        if (wraps) hints.Add(Tr("Depois do 12, o relógio de ponteiros volta para o 1.", "After 12, a clock with hands goes back to 1.", "Después del 12, el reloj de agujas vuelve al 1."));
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"No relógio de ponteiros são {h} horas. Que horas o relógio vai marcar daqui a {k} horas?",
               $"A clock with hands shows {h} o'clock. What time will it show {k} hours from now?",
               $"El reloj de agujas marca las {h}. ¿Qué hora marcará dentro de {k} horas?"),
            answer.ToString(),
            hints,
            wraps
                ? Tr($"{h} + {k} = {h + k}; passando do 12, o relógio marca {answer}.", $"{h} + {k} = {h + k}; going past 12, the clock shows {answer}.", $"{h} + {k} = {h + k}; al pasar del 12, el reloj marca las {answer}.")
                : $"{h} + {k} = {answer}");
        return new(content, wraps ? 800 : 400 + k * 15);
    }

    private static GeneratedProblem Duration(Random rng)
    {
        var start = Between(rng, 1, 8);
        var end = Between(rng, start + 1, 11);
        var what = Pick(rng, Current switch
        {
            En => ["A trip", "A party", "A soccer game", "A swimming class"],
            Es => ["Un paseo", "Una fiesta", "Un partido de fútbol", "Una clase de natación"],
            _ => (string[])["Um passeio", "Uma festa", "Um jogo de futebol", "Uma aula de natação"],
        });
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"{what} começou às {start} horas e terminou às {end} horas. Quantas horas durou?",
               $"{what} started at {start} o'clock and ended at {end} o'clock. How many hours did it last?",
               $"{what} empezó a las {start} y terminó a las {end}. ¿Cuántas horas duró?"),
            (end - start).ToString(),
            [Tr($"Conte quantas horas há do {start} até o {end}.", $"Count how many hours there are from {start} to {end}.", $"Cuenta cuántas horas hay del {start} al {end}."), $"{end} − {start} = ?"],
            Tr($"{end} − {start} = {end - start} horas", $"{end} − {start} = {end - start} hours", $"{end} − {start} = {end - start} horas"));
        return new(content, 600);
    }

    private static GeneratedProblem Minutes(Random rng)
    {
        return rng.Next(3) switch
        {
            0 => Make(HoursPrompt(out var k), 60 * k, [OneHour(), $"{string.Join(" + ", Enumerable.Repeat(60, k))} = ?"], 700 + k * 50),
            1 => Make(Tr("Quantos minutos há em meia hora?", "How many minutes are there in half an hour?", "¿Cuántos minutos hay en media hora?"), 30,
                [OneHour(), Tr("Meia hora é a metade de 1 hora.", "Half an hour is half of 1 hour.", "Media hora es la mitad de 1 hora.")], 750),
            _ => Make(Tr("Quantos minutos há em 1 hora e meia?", "How many minutes are there in an hour and a half?", "¿Cuántos minutos hay en 1 hora y media?"), 90,
                [Tr("1 hora tem 60 minutos e meia hora tem 30.", "1 hour has 60 minutes and half an hour has 30.", "1 hora tiene 60 minutos y media hora tiene 30."), "60 + 30 = ?"], 950),
        };

        string HoursPrompt(out int hours)
        {
            hours = Between(rng, 1, 3);
            return hours == 1
                ? Tr("Quantos minutos há em 1 hora?", "How many minutes are there in 1 hour?", "¿Cuántos minutos hay en 1 hora?")
                : Tr($"Quantos minutos há em {hours} horas?", $"How many minutes are there in {hours} hours?", $"¿Cuántos minutos hay en {hours} horas?");
        }

        static string OneHour() => Tr("1 hora tem 60 minutos.", "1 hour has 60 minutes.", "1 hora tiene 60 minutos.");

        static GeneratedProblem Make(string prompt, int answer, List<string> hints, int difficulty) =>
            new(new ProblemContent(ProblemTypes.Numero, prompt, answer.ToString(), hints,
                Tr($"A resposta é {answer} minutos.", $"The answer is {answer} minutes.", $"La respuesta es {answer} minutos.")), difficulty);
    }

    private static GeneratedProblem Coins(Random rng)
    {
        var reais = Between(rng, 1, 5);
        var content = new ProblemContent(
            ProblemTypes.Numero,
            Tr($"Quantas moedas de 50 centavos são necessárias para formar R$ {reais},00?",
               $"How many 50-cent coins do you need to make ${reais}?",
               $"¿Cuántas monedas de 50 centavos hacen falta para formar ${reais}?"),
            (reais * 2).ToString(),
            [
                Tr("Duas moedas de 50 centavos formam R$ 1,00.", "Two 50-cent coins make $1.", "Dos monedas de 50 centavos forman $1."),
                Tr($"Para R$ {reais},00 você precisa de 2 moedas para cada real.", $"For ${reais} you need 2 coins for each dollar.", $"Para ${reais} necesitas 2 monedas por cada unidad."),
            ],
            Tr($"{reais} × 2 = {reais * 2} moedas", $"{reais} × 2 = {reais * 2} coins", $"{reais} × 2 = {reais * 2} monedas"));
        return new(content, 850 + reais * 30);
    }
}
