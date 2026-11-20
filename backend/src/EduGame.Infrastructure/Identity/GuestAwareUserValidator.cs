using Microsoft.AspNetCore.Identity;

namespace EduGame.Infrastructure.Identity;

/// <summary>
/// Validador de usuários que aceita convidados sem e-mail (o padrão do Identity exige e-mail
/// quando RequireUniqueEmail está ligado). Contas normais seguem a validação completa.
/// </summary>
public sealed class GuestAwareUserValidator(IdentityErrorDescriber? describer = null) : UserValidator<AppUser>(describer)
{
    public override async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
    {
        if (!user.IsGuest) return await base.ValidateAsync(manager, user);

        if (string.IsNullOrWhiteSpace(user.UserName))
            return IdentityResult.Failed(Describer.InvalidUserName(user.UserName));
        var owner = await manager.FindByNameAsync(user.UserName);
        return owner is not null && owner.Id != user.Id
            ? IdentityResult.Failed(Describer.DuplicateUserName(user.UserName))
            : IdentityResult.Success;
    }
}
