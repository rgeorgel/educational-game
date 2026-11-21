using System.Security.Cryptography;
using EduGame.Api.Common;
using EduGame.Api.Features.Auth;
using EduGame.Api.Features.Billing;
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
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sem 0/O, 1/I

    public sealed record CreateClassroomRequest(string Name);
    public sealed record JoinRequest(string Code);
    public sealed record ClassroomDto(Guid Id, string Name, string Code, int Members, DateTimeOffset CreatedAt);
    public sealed record JoinedClassroomDto(Guid Id, string Name, DateTimeOffset JoinedAt);
    public sealed record ClassroomReport(
        Guid Id,
        string Name,
        string Code,
        IReadOnlyList<ProfileReport> Students,
        IReadOnlyList<AssignmentDto> Assignments,
        IReadOnlyList<SkillClassDto> Skills,
        PlanInfo Plan);

    public sealed record ChoosePlanRequest(string Plan);
    public sealed record CreateAssignmentRequest(int StageId, int MinStars = 1, DateOnly? DueDate = null, string? Note = null);

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

        classes.MapPost("/", async (CreateClassroomRequest req, HttpContext http, AppDbContext db, PlanService plans, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            if (await db.IsGuestAsync(userId, ct))
                return ApiResults.Forbidden("Para criar turmas, crie uma conta (salve o progresso) primeiro.");
            var name = (req.Name ?? "").Trim();
            if (name.Length is < 2 or > 60)
                return ApiResults.Validation("name", "O nome da turma deve ter de 2 a 60 caracteres.");
            var plan = await plans.ForUserAsync(userId, ct);
            var limit = plan.MaxClassrooms;
            if (await db.Classrooms.CountAsync(c => c.OwnerUserId == userId, ct) >= limit)
                return ApiResults.Conflict(PlanService.LimitMessage(plan, $"Sua conta pode ter até {limit} {(limit == 1 ? "turma" : "turmas")}."));

            string code;
            do code = NewCode(); while (await db.Classrooms.AnyAsync(c => c.Code == code, ct));
            var classroom = new Classroom { OwnerUserId = userId, Name = name, Code = code };
            db.Classrooms.Add(classroom);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/classrooms/{classroom.Id}", new ClassroomDto(classroom.Id, classroom.Name, classroom.Code, 0, classroom.CreatedAt));
        });

        classes.MapGet("/{classroomId:guid}/report", async (Guid classroomId, HttpContext http, AppDbContext db, ReportService reports,
            ClassroomInsights insights, PlanService plans, CancellationToken ct) =>
        {
            var classroom = await OwnedAsync(db, http, classroomId, ct);
            if (classroom is null) return ApiResults.NotFound("Turma não encontrada.");
            var students = await StudentsAsync(db, classroomId, ct);
            var ids = students.Select(p => p.Id).ToList();
            return Results.Ok(new ClassroomReport(classroom.Id, classroom.Name, classroom.Code,
                await reports.BuildAsync(ids, ct),
                await insights.AssignmentsAsync(classroomId, students, ct),
                await insights.SkillsAsync(ids, ct),
                await plans.ForUserAsync(classroom.OwnerUserId, ct)));
        });

        // ---------- Tarefas da turma ----------
        classes.MapPost("/{classroomId:guid}/assignments", async (Guid classroomId, CreateAssignmentRequest req, HttpContext http, AppDbContext db,
            PlanService plans, GameClock clock, CancellationToken ct) =>
        {
            var classroom = await OwnedAsync(db, http, classroomId, ct);
            if (classroom is null) return ApiResults.NotFound("Turma não encontrada.");
            if (!await db.Stages.AnyAsync(s => s.Id == req.StageId, ct)) return ApiResults.Validation("stageId", "Etapa não encontrada.");
            if (req.MinStars is < 1 or > 3) return ApiResults.Validation("minStars", "Escolha de 1 a 3 estrelas.");
            var today = clock.Today;
            if (req.DueDate is { } due && (due < today || due > today.AddYears(1)))
                return ApiResults.Validation("dueDate", "O prazo deve ser de hoje até daqui a um ano.");
            var note = req.Note?.Trim();
            if (note is { Length: > 140 }) return ApiResults.Validation("note", "O recado pode ter até 140 caracteres.");

            var plan = await plans.ForUserAsync(classroom.OwnerUserId, ct);
            var active = await db.ClassroomAssignments.CountAsync(a => a.ClassroomId == classroomId && (a.DueDate == null || a.DueDate >= today), ct);
            if (active >= plan.MaxActiveAssignmentsPerClassroom)
                return ApiResults.Conflict(PlanService.LimitMessage(plan, $"Esta turma já tem {active} tarefas em aberto (o limite é {plan.MaxActiveAssignmentsPerClassroom})."));

            var assignment = new ClassroomAssignment
            {
                ClassroomId = classroomId,
                StageId = req.StageId,
                MinStars = req.MinStars,
                DueDate = req.DueDate,
                Note = string.IsNullOrEmpty(note) ? null : note,
            };
            db.ClassroomAssignments.Add(assignment);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/classrooms/{classroomId}/assignments/{assignment.Id}", new { assignment.Id });
        });

        classes.MapDelete("/{classroomId:guid}/assignments/{assignmentId:guid}", async (Guid classroomId, Guid assignmentId, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var classroom = await OwnedAsync(db, http, classroomId, ct);
            if (classroom is null) return ApiResults.NotFound("Turma não encontrada.");
            await db.ClassroomAssignments.Where(a => a.Id == assignmentId && a.ClassroomId == classroomId).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });

        // ---------- Exportar (planilha) ----------
        classes.MapGet("/{classroomId:guid}/export.csv", async (Guid classroomId, string? tipo, HttpContext http, AppDbContext db,
            ReportService reports, ClassroomInsights insights, PlanService plans, CancellationToken ct) =>
        {
            var classroom = await OwnedAsync(db, http, classroomId, ct);
            if (classroom is null) return ApiResults.NotFound("Turma não encontrada.");
            var plan = await plans.ForUserAsync(classroom.OwnerUserId, ct);
            if (!plan.CanExport) return ApiResults.Forbidden(PlanService.LimitMessage(plan, "Exportar a planilha da turma é um recurso do Professor Pro."));

            var students = await StudentsAsync(db, classroomId, ct);
            var ids = students.Select(p => p.Id).ToList();
            var kind = tipo == "habilidades" ? "habilidades" : "alunos";
            var csv = kind == "habilidades"
                ? ClassroomCsv.Skills(await insights.SkillsAsync(ids, ct))
                : ClassroomCsv.Students(await reports.BuildAsync(ids, ct), await insights.AssignmentsAsync(classroomId, students, ct));
            var file = $"turma-{ClassroomCsv.Slug(classroom.Name)}-{kind}-{DateTime.UtcNow:yyyy-MM-dd}.csv";
            return Results.File(csv, "text/csv; charset=utf-8", file);
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

        // Tarefas das turmas em que o perfil está (aparecem na tela inicial do aluno).
        api.MapGet("/profiles/{profileId:guid}/assignments", async (Guid profileId, HttpContext http, AppDbContext db, ClassroomInsights insights, CancellationToken ct) =>
        {
            var profile = await db.FindOwnedProfileAsync(http.User, profileId, ct);
            if (profile is null) return ApiResults.NotFound("Perfil não encontrado.");
            return Results.Ok(await insights.ForStudentAsync(profileId, ct));
        }).RequireAuthorization().WithTags("Classrooms");

        // ---------- Planos ----------
        api.MapGet("/plans", () => Results.Ok(PlanService.Offers)).WithTags("Billing");

        api.MapGet("/me/plan", async (HttpContext http, PlanService plans, CancellationToken ct) =>
            Results.Ok(await plans.ForUserAsync(http.User.UserId(), ct))).RequireAuthorization().WithTags("Billing");

        // Escolher o plano (por enquanto sem pagamento; depois o Stripe).
        api.MapPut("/me/plan", async (ChoosePlanRequest req, HttpContext http, AppDbContext db, PlanService plans, StripeSettings stripe, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            if (await db.IsGuestAsync(userId, ct))
                return ApiResults.Forbidden("Para escolher um plano, salve o progresso criando uma conta.");
            var plan = Domain.Billing.Plans.BySlug(req.Plan);
            if (plan is null || !plan.SelfService)
                return ApiResults.Validation("plan", "Escolha o plano Grátis, Professor Pro ou Família. Para escolas, fale com a gente.");
            // Com o Stripe ligado, os planos pagos só entram pelo pagamento (Checkout).
            if (stripe.Enabled && plan.Slug != Domain.Billing.Plans.FreeSlug)
                return ApiResults.Conflict("Para assinar, use o botão \"Assinar\" na página de planos (pagamento seguro pelo Stripe).");
            return Results.Ok(await plans.ChooseAsync(userId, plan, ct));
        }).RequireAuthorization().RequireRateLimiting(RateLimits.Auth).WithTags("Billing");

        // Teste grátis de 7 dias de um plano pago, sem cartão (só com o Stripe ligado: no lançamento a escolha já é grátis).
        api.MapPost("/me/plan/trial", async (ChoosePlanRequest req, HttpContext http, AppDbContext db, PlanService plans, StripeSettings stripe, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            if (await db.IsGuestAsync(userId, ct))
                return ApiResults.Forbidden("Para testar um plano, salve o progresso criando uma conta.");
            var plan = Domain.Billing.Plans.BySlug(req.Plan);
            if (plan is null || !Domain.Billing.Plans.HasSelfServiceTrial(plan))
                return ApiResults.Validation("plan", "O teste grátis é para os planos Professor Pro e Família.");
            if (!stripe.Enabled)
                return ApiResults.Conflict("Durante o lançamento os planos já são grátis: é só escolher o plano.");
            if (await plans.StartTrialAsync(userId, plan, ct) is { } error) return ApiResults.Conflict(error);
            return Results.Ok(await plans.ForUserAsync(userId, ct));
        }).RequireAuthorization().RequireRateLimiting(RateLimits.Auth).WithTags("Billing");

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

    private static Task<List<PlayerProfile>> StudentsAsync(AppDbContext db, Guid classroomId, CancellationToken ct) =>
        db.ClassroomMembers.Where(m => m.ClassroomId == classroomId)
            .Join(db.Profiles, m => m.ProfileId, p => p.Id, (m, p) => p)
            .OrderBy(p => p.Nickname).AsNoTracking().ToListAsync(ct);

    private static Task<Classroom?> OwnedAsync(AppDbContext db, HttpContext http, Guid classroomId, CancellationToken ct)
    {
        var userId = http.User.UserId();
        return db.Classrooms.SingleOrDefaultAsync(c => c.Id == classroomId && c.OwnerUserId == userId, ct);
    }
}
