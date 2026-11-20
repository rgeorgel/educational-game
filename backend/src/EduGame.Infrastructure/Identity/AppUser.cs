using Microsoft.AspNetCore.Identity;

namespace EduGame.Infrastructure.Identity;

/// <summary>
/// Conta de quem joga. Pode ser:
/// - convidado (<see cref="IsGuest"/>): criada só com um apelido, presa a este aparelho pelo cookie,
///   sem e-mail nem senha; vira conta normal quando a pessoa "salva o progresso";
/// - conta do responsável: com e-mail, senha e consentimento (LGPD art. 14).
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Quando o responsável aceitou os termos e consentiu com o uso dos dados. Vazio em convidados.</summary>
    public DateTimeOffset? ConsentAcceptedAt { get; set; }

    /// <summary>Conta de convidado: joga sem cadastro neste aparelho.</summary>
    public bool IsGuest { get; set; }

    /// <summary>Último acesso (atualizado no máximo de hora em hora). Usado para apagar convidados abandonados.</summary>
    public DateTimeOffset? LastSeenAt { get; set; }
}
