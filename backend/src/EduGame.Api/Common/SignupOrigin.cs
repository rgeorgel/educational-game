using System.Text.RegularExpressions;
using EduGame.Infrastructure.Identity;

namespace EduGame.Api.Common;

/// <summary>
/// De onde a pessoa veio quando chegou ao jogo pela primeira vez (primeiro toque), para saber
/// quais canais de divulgação funcionam. Só dados de campanha (utm_*) e o domínio do site de
/// origem — nunca a URL completa nem nada pessoal.
/// </summary>
public sealed record OriginRequest(string? Source = null, string? Medium = null, string? Campaign = null, string? Referrer = null);

public static partial class SignupOrigin
{
    public const string Direct = "direto";
    private const int MaxLength = 64;

    // Domínio do site de origem → nome do canal (o resto fica com o próprio domínio).
    // A ordem importa: os mais específicos primeiro (gemini.google.com antes de google.).
    private static readonly (string Suffix, string Source)[] KnownHosts =
    [
        ("gemini.google.com", "gemini"), ("copilot.microsoft.com", "copilot"),
        ("google.", "google"), ("bing.com", "bing"), ("duckduckgo.com", "duckduckgo"), ("yahoo.", "yahoo"),
        ("facebook.com", "facebook"), ("fb.com", "facebook"), ("instagram.com", "instagram"),
        ("whatsapp.com", "whatsapp"), ("wa.me", "whatsapp"), ("youtube.com", "youtube"), ("youtu.be", "youtube"),
        ("tiktok.com", "tiktok"), ("t.co", "x"), ("x.com", "x"), ("twitter.com", "x"), ("linkedin.com", "linkedin"),
        ("pinterest.", "pinterest"), ("reddit.com", "reddit"), ("t.me", "telegram"), ("telegram.org", "telegram"),
        ("chatgpt.com", "chatgpt"), ("openai.com", "chatgpt"), ("perplexity.ai", "perplexity"), ("claude.ai", "claude"),
    ];

    [GeneratedRegex("[^a-z0-9._-]+")]
    private static partial Regex Invalid();

    /// <summary>Preenche a origem do usuário novo (só na criação; quem vira conta mantém a origem do convidado).</summary>
    public static void Apply(AppUser user, OriginRequest? origin, string? ownHost)
    {
        var referrer = ReferrerHost(origin?.Referrer, ownHost);
        var source = Clean(origin?.Source) ?? FromHost(referrer) ?? Direct;
        user.SignupSource = source;
        user.SignupMedium = Clean(origin?.Medium);
        user.SignupCampaign = Clean(origin?.Campaign);
        user.SignupReferrer = referrer;
    }

    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        // Tira acentos ("às" → "as") e deixa só letras, números, ponto, hífen e sublinhado.
        var plain = new string(value.Trim().ToLowerInvariant().Replace(' ', '-').Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        var cleaned = Invalid().Replace(plain, "");
        if (cleaned.Length > MaxLength) cleaned = cleaned[..MaxLength];
        return cleaned.Length == 0 ? null : cleaned;
    }

    /// <summary>Só o domínio (sem caminho nem parâmetros); ignora o próprio site.</summary>
    public static string? ReferrerHost(string? referrer, string? ownHost)
    {
        if (string.IsNullOrWhiteSpace(referrer)) return null;
        if (!Uri.TryCreate(referrer.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https" or "android-app")) return null;
        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.")) host = host[4..];
        if (ownHost is not null && host.Equals(ownHost.Split(':')[0].Replace("www.", ""), StringComparison.OrdinalIgnoreCase)) return null;
        return Clean(host);
    }

    public static string? FromHost(string? host)
    {
        if (host is null) return null;
        foreach (var (suffix, source) in KnownHosts)
        {
            if (suffix.EndsWith('.') ? host.Contains(suffix) : host == suffix || host.EndsWith("." + suffix))
                return source;
        }
        return host;
    }
}
