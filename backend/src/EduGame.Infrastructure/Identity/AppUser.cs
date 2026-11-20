using Microsoft.AspNetCore.Identity;

namespace EduGame.Infrastructure.Identity;

/// <summary>Conta do responsável (adulto). As crianças jogam por perfis dentro desta conta.</summary>
public class AppUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Quando o responsável aceitou os termos e consentiu com o uso dos dados (LGPD art. 14).</summary>
    public DateTimeOffset ConsentAcceptedAt { get; set; }
}
