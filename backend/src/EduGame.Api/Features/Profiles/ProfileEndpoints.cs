using System.Globalization;
using System.Text;
using EduGame.Api.Common;
using EduGame.Domain.Players;
using EduGame.Domain.Progression;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Profiles;

public static class ProfileEndpoints
{
    public const int MaxProfilesPerAccount = 6;

    public static readonly string[] Avatars = ["🦊", "🐼", "🐯", "🐸", "🦉", "🐙", "🦄", "🐢", "🐧", "🦁", "🐨", "🐳"];

    // Lista curta e propositalmente simples; pode crescer com o tempo.
    private static readonly string[] Blocked = ["merda", "porra", "caralho", "puta", "foda", "bosta", "cu", "buceta", "viado", "idiota", "burro"];

    public sealed record ProfileDto(Guid Id, string Nickname, string Avatar, string Accessory, string AccessoryIcon, string Color, int? BirthYear, int? SchoolGrade);

    public static ProfileDto ToDto(PlayerProfile p) => new(
        p.Id, p.Nickname, p.Avatar, p.Accessory,
        AvatarItems.Accessories.FirstOrDefault(a => a.Slug == p.Accessory)?.Value ?? "",
        p.Color, p.BirthYear, p.SchoolGrade);
    public sealed record CreateProfileRequest(string Nickname, string Avatar, int? BirthYear, int? SchoolGrade);

    public static async Task<List<ProfileDto>> ListAsync(AppDbContext db, Guid userId, CancellationToken ct) =>
        [.. (await db.Profiles.Where(p => p.UserId == userId).OrderBy(p => p.CreatedAt).ToListAsync(ct)).Select(ToDto)];

    public static void MapProfiles(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/profiles").WithTags("Profiles").RequireAuthorization();

        group.MapGet("/", (HttpContext http, AppDbContext db, CancellationToken ct) => ListAsync(db, http.User.UserId(), ct));

        // Público: a tela "Jogar agora" mostra os avatares antes de existir qualquer conta.
        group.MapGet("/avatars", () => Avatars).AllowAnonymous();

        group.MapPost("/", async (CreateProfileRequest req, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            var nickname = (req.Nickname ?? "").Trim();

            if (ValidateNicknameAndAvatar(nickname, req.Avatar) is { } invalid) return invalid;
            var year = DateTime.UtcNow.Year;
            if (req.BirthYear is { } by && (by < year - 20 || by > year - 4))
                return ApiResults.Validation("birthYear", "Ano de nascimento fora da faixa esperada.");
            if (req.SchoolGrade is { } g && g is < 1 or > 12)
                return ApiResults.Validation("schoolGrade", "Série/ano deve estar entre 1 e 12.");
            if (await db.Profiles.CountAsync(p => p.UserId == userId, ct) >= MaxProfilesPerAccount)
                return ApiResults.Conflict($"Cada conta pode ter até {MaxProfilesPerAccount} perfis.");

            var profile = new PlayerProfile
            {
                UserId = userId,
                Nickname = nickname,
                Avatar = req.Avatar,
                BirthYear = req.BirthYear,
                SchoolGrade = req.SchoolGrade,
            };
            db.Profiles.Add(profile);
            await db.SaveChangesAsync(ct);

            var dto = ToDto(profile);
            return Results.Created($"/api/profiles/{profile.Id}", dto);
        });

        group.MapGet("/{profileId:guid}/avatar", async (Guid profileId, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            var xp = await TotalXpAsync(db, profileId, ct);
            return Results.Ok(new AvatarOptions(xp, ToDto(profile), Avatars,
                [.. AvatarItems.Accessories.Select(i => new AvatarItemDto(i.Slug, i.Name, i.Value, i.XpRequired, i.XpRequired <= xp))],
                [.. AvatarItems.Colors.Select(i => new AvatarItemDto(i.Slug, i.Name, i.Value, i.XpRequired, i.XpRequired <= xp))]));
        });

        group.MapPut("/{profileId:guid}/avatar", async (Guid profileId, UpdateAvatarRequest req, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            var xp = await TotalXpAsync(db, profileId, ct);
            if (!Avatars.Contains(req.Avatar))
                return ApiResults.Validation("avatar", "Escolha um dos avatares disponíveis.");
            if (!AvatarItems.IsUnlocked(AvatarItems.Accessories, req.Accessory, xp))
                return ApiResults.Validation("accessory", "Este acessório ainda não foi desbloqueado.");
            if (!AvatarItems.IsUnlocked(AvatarItems.Colors, req.Color, xp))
                return ApiResults.Validation("color", "Esta cor ainda não foi desbloqueada.");

            profile.Avatar = req.Avatar;
            profile.Accessory = req.Accessory;
            profile.Color = req.Color;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToDto(profile));
        });

        group.MapDelete("/{profileId:guid}", async (Guid profileId, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            db.Profiles.Remove(profile);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    public sealed record AvatarItemDto(string Slug, string Name, string Value, int XpRequired, bool Unlocked);
    public sealed record AvatarOptions(int Xp, ProfileDto Profile, IReadOnlyList<string> Avatars, IReadOnlyList<AvatarItemDto> Accessories, IReadOnlyList<AvatarItemDto> Colors);
    public sealed record UpdateAvatarRequest(string Avatar, string Accessory, string Color);

    private static async Task<int> TotalXpAsync(AppDbContext db, Guid profileId, CancellationToken ct) =>
        await db.SubjectProgresses.Where(p => p.ProfileId == profileId).SumAsync(p => p.Xp, ct);

    /// <summary>Valida apelido e avatar (usado ao criar perfil e ao começar como convidado).</summary>
    public static IResult? ValidateNicknameAndAvatar(string nickname, string? avatar)
    {
        if (nickname.Length is < 2 or > 20 || !nickname.All(c => char.IsLetterOrDigit(c) || c == ' '))
            return ApiResults.Validation("nickname", "O apelido deve ter de 2 a 20 letras ou números.");
        if (IsBlocked(nickname))
            return ApiResults.Validation("nickname", "Escolha outro apelido.");
        if (avatar is null || !Avatars.Contains(avatar))
            return ApiResults.Validation("avatar", "Escolha um dos avatares disponíveis.");
        return null;
    }

    private static bool IsBlocked(string nickname)
    {
        var normalized = RemoveAccents(nickname.ToLowerInvariant());
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // Só palavras inteiras, para não bloquear nomes inocentes (ex.: "computador").
        return Blocked.Any(b => words.Contains(b) || normalized.Replace(" ", "") == b);
    }

    private static string RemoveAccents(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
