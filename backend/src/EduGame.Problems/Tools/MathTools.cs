namespace EduGame.Problems.Tools;

public static class MathTools
{
    public static int Gcd(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    public static int Lcm(int a, int b) => a / Gcd(a, b) * b;

    public static string Superscript(int n) =>
        string.Concat(n.ToString().Select(c => "⁰¹²³⁴⁵⁶⁷⁸⁹"[c - '0']));

    /// <summary>Formata um inteiro com o sinal de menos tipográfico e parênteses quando negativo (para expressões).</summary>
    public static string Signed(int n) => n < 0 ? $"(−{-n})" : n.ToString();

    public static string Minus(int n) => n < 0 ? $"−{-n}" : n.ToString();
}

/// <summary>Fração sempre guardada com denominador positivo.</summary>
public readonly record struct Fraction(int Num, int Den)
{
    public Fraction Simplify()
    {
        var g = MathTools.Gcd(Num, Den);
        var sign = Den < 0 ? -1 : 1;
        return new(sign * Num / g, sign * Den / g);
    }

    public static Fraction operator +(Fraction a, Fraction b) => new Fraction(a.Num * b.Den + b.Num * a.Den, a.Den * b.Den).Simplify();
    public static Fraction operator -(Fraction a, Fraction b) => new Fraction(a.Num * b.Den - b.Num * a.Den, a.Den * b.Den).Simplify();
    public static Fraction operator *(Fraction a, Fraction b) => new Fraction(a.Num * b.Num, a.Den * b.Den).Simplify();
    public static Fraction operator /(Fraction a, Fraction b) => new Fraction(a.Num * b.Den, a.Den * b.Num).Simplify();

    public decimal Value => (decimal)Num / Den;

    public override string ToString() => Den == 1 ? MathTools.Minus(Num) : $"{MathTools.Minus(Num)}/{Den}";
}
