using System.Globalization;
using System.Text;
using EduGame.Api.Common;
using EduGame.Domain.Players;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Profiles;

public static class ProfileEndpoints
{
    public const int MaxProfilesPerAccount = 6;

    public static readonly string[] Avatars = ["🦊", "🐼", "🐯", "🐸", "🦉", "🐙", "🦄", "🐢", "🐧", "🦁", "🐨", "🐳"];

    // Lista curta e propositalmente simples; pode crescer com o tempo.
    private static readonly string[] Blocked = ["merda", "porra", "caralho", "puta", "foda", "bosta", "cu", "buceta", "viado", "idiota", "burro"];

    public sealed record ProfileDto(Guid Id, string Nickname, string Avatar, int? BirthYear, int? SchoolGrade);
    public sealed record CreateProfileRequest(string Nickname, string Avatar, int? BirthYear, int? SchoolGrade);

    public static Task<List<ProfileDto>> ListAsync(AppDbContext db, Guid userId, CancellationToken ct) =>
        db.Profiles.Where(p => p.UserId == userId).OrderBy(p => p.CreatedAt)
            .Select(p => new ProfileDto(p.Id, p.Nickname, p.Avatar, p.BirthYear, p.SchoolGrade))
            .ToListAsync(ct);

    public static void MapProfiles(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/profiles").WithTags("Profiles").RequireAuthorization();

        group.MapGet("/", (HttpContext http, AppDbContext db, CancellationToken ct) => ListAsync(db, http.User.UserId(), ct));

        group.MapGet("/avatars", () => Avatars);

        group.MapPost("/", async (CreateProfileRequest req, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            var nickname = (req.Nickname ?? "").Trim();

            if (nickname.Length is < 2 or > 20 || !nickname.All(c => char.IsLetterOrDigit(c) || c == ' '))
                return ApiResults.Validation("nickname", "O apelido deve ter de 2 a 20 letras ou números.");
            if (IsBlocked(nickname))
                return ApiResults.Validation("nickname", "Escolha outro apelido.");
            if (!Avatars.Contains(req.Avatar))
                return ApiResults.Validation("avatar", "Escolha um dos avatares disponíveis.");
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

            var dto = new ProfileDto(profile.Id, profile.Nickname, profile.Avatar, profile.BirthYear, profile.SchoolGrade);
            return Results.Created($"/api/profiles/{profile.Id}", dto);
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
