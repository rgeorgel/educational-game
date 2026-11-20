using System.Security.Cryptography;
using EduGame.Api.Common;
using EduGame.Api.Features.Auth;
using EduGame.Domain.Players;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Api.Features.Reports;

/// <summary>
/// Painel do responsável e turmas de professor.
/// O professor cria a turma e compartilha o código; o responsável decide quais perfis entram
/// (consentimento explícito) e pode tirá-los a qualquer momento. O professor vê só apelido e progresso.
/// </summary>
public static class ClassroomEndpoints
{
    public const int MaxClassroomsPerUser = 20;
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sem 0/O, 1/I

    public sealed record CreateClassroomRequest(string Name);
    public sealed record JoinRequest(string Code);
    public sealed record ClassroomDto(Guid Id, string Name, string Code, int Members, DateTimeOffset CreatedAt);
    public sealed record JoinedClassroomDto(Guid Id, string Name, DateTimeOffset JoinedAt);
    public sealed record ClassroomReport(Guid Id, string Name, string Code, IReadOnlyList<ProfileReport> Students);

    public static string NewCode() =>
        string.Concat(Enumerable.Range(0, 8).Select(_ => CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)]));

    public static void MapReports(this RouteGroupBuilder api)
    {
        // ---------- Responsável ----------
        api.MapGet("/guardian/report", async (HttpContext http, AppDbContext db, ReportService reports, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            var ids = await db.Profiles.Where(p => p.UserId == userId).Select(p => p.Id).ToListAsync(ct);
            return Results.Ok(await reports.BuildAsync(ids, ct));
        }).RequireAuthorization().WithTags("Reports");

        // ---------- Professor ----------
        var classes = api.MapGroup("/classrooms").WithTags("Classrooms").RequireAuthorization();

        classes.MapGet("/", async (HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            return await db.Classrooms.Where(c => c.OwnerUserId == userId).OrderBy(c => c.CreatedAt)
                .Select(c => new ClassroomDto(c.Id, c.Name, c.Code, db.ClassroomMembers.Count(m => m.ClassroomId == c.Id), c.CreatedAt))
                .ToListAsync(ct);
        });

        classes.MapPost("/", async (CreateClassroomRequest req, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            if (await db.IsGuestAsync(userId, ct))
                return ApiResults.Forbidden("Para criar turmas, crie uma conta (salve o progresso) primeiro.");
            var name = (req.Name ?? "").Trim();
            if (name.Length is < 2 or > 60)
                return ApiResults.Validation("name", "O nome da turma deve ter de 2 a 60 caracteres.");
            if (await db.Classrooms.CountAsync(c => c.OwnerUserId == userId, ct) >= MaxClassroomsPerUser)
                return ApiResults.Conflict($"Cada conta pode ter até {MaxClassroomsPerUser} turmas.");

            string code;
            do code = NewCode(); while (await db.Classrooms.AnyAsync(c => c.Code == code, ct));
            var classroom = new Classroom { OwnerUserId = userId, Name = name, Code = code };
            db.Classrooms.Add(classroom);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/classrooms/{classroom.Id}", new ClassroomDto(classroom.Id, classroom.Name, classroom.Code, 0, classroom.CreatedAt));
        });

        classes.MapGet("/{classroomId:guid}/report", async (Guid classroomId, HttpContext http, AppDbContext db, ReportService reports, CancellationToken ct) =>
        {
            var classroom = await OwnedAsync(db, http, classroomId, ct);
            if (classroom is null) return ApiResults.NotFound("Turma não encontrada.");
            var ids = await db.ClassroomMembers.Where(m => m.ClassroomId == classroomId).Select(m => m.ProfileId).ToListAsync(ct);
            return Results.Ok(new ClassroomReport(classroom.Id, classroom.Name, classroom.Code, await reports.BuildAsync(ids, ct)));
        });

        classes.MapDelete("/{classroomId:guid}", async (Guid classroomId, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var classroom = await OwnedAsync(db, http, classroomId, ct);
            if (classroom is null) return ApiResults.NotFound("Turma não encontrada.");
            db.Classrooms.Remove(classroom);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        classes.MapDelete("/{classroomId:guid}/members/{profileId:guid}", async (Guid classroomId, Guid profileId, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var classroom = await OwnedAsync(db, http, classroomId, ct);
            if (classroom is null) return ApiResults.NotFound("Turma não encontrada.");
            await db.ClassroomMembers.Where(m => m.ClassroomId == classroomId && m.ProfileId == profileId).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });

        // ---------- Responsável: perfis nas turmas ----------
        var memberships = api.MapGroup("/profiles/{profileId:guid}/classrooms").WithTags("Classrooms").RequireAuthorization();

        memberships.MapGet("/", async (Guid profileId, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            return Results.Ok(await db.ClassroomMembers.Where(m => m.ProfileId == profileId)
                .Join(db.Classrooms, m => m.ClassroomId, c => c.Id, (m, c) => new JoinedClassroomDto(c.Id, c.Name, m.JoinedAt))
                .ToListAsync(ct));
        });

        memberships.MapPost("/", async (Guid profileId, JoinRequest req, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            if (await db.IsGuestAsync(http.User.UserId(), ct))
                return ApiResults.Forbidden("Para entrar numa turma, um responsável precisa criar a conta (salvar o progresso) e autorizar.");
            var code = (req.Code ?? "").Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
            var classroom = await db.Classrooms.SingleOrDefaultAsync(c => c.Code == code, ct);
            if (classroom is null) return ApiResults.Validation("code", "Código de turma não encontrado. Confira com o professor.");
            if (!await db.ClassroomMembers.AnyAsync(m => m.ClassroomId == classroom.Id && m.ProfileId == profileId, ct))
            {
                db.ClassroomMembers.Add(new ClassroomMember { ClassroomId = classroom.Id, ProfileId = profileId });
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(new JoinedClassroomDto(classroom.Id, classroom.Name, DateTimeOffset.UtcNow));
        }).RequireRateLimiting(RateLimits.Auth);

        memberships.MapDelete("/{classroomId:guid}", async (Guid profileId, Guid classroomId, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            await db.ClassroomMembers.Where(m => m.ClassroomId == classroomId && m.ProfileId == profileId).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });
    }

    private static Task<Classroom?> OwnedAsync(AppDbContext db, HttpContext http, Guid classroomId, CancellationToken ct)
    {
        var userId = http.User.UserId();
        return db.Classrooms.SingleOrDefaultAsync(c => c.Id == classroomId && c.OwnerUserId == userId, ct);
    }
}
