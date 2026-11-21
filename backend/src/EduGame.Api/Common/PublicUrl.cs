namespace EduGame.Api.Common;

/// <summary>
/// Endereço público do site para links em e-mails (App:PublicBaseUrl; no Docker: PUBLIC_BASE_URL).
/// Em produção ele é obrigatório: montar o link a partir do cabeçalho Host da requisição permitiria
/// que alguém pedisse um e-mail de nova senha com um link apontando para outro site.
/// Fora da produção, sem configuração, usa o endereço da própria requisição.
/// </summary>
public sealed class PublicUrl(IConfiguration config, IHostEnvironment env)
{
    public string? Configured => config["App:PublicBaseUrl"] is { Length: > 0 } url ? url.Trim().TrimEnd('/') : null;

    public string? For(HttpRequest request) =>
        Configured ?? (env.IsProduction() ? null : $"{request.Scheme}://{request.Host}");
}
