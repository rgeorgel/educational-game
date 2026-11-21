using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EduGame.Api.Features.Billing;
using EduGame.Api.Features.Reports;
using EduGame.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduGame.Api.Tests;

/// <summary>Tarefas da turma, relatório por habilidade (BNCC), planilha e planos.</summary>
[Collection(ApiCollection.Name)]
public class ClassroomTasksTests(ApiFactory factory)
{
    private async Task<(HttpClient Client, string Email)> NewAccountAsync()
    {
        var client = factory.CreateClient();
        var email = $"p-{Guid.NewGuid():N}@exemplo.com";
        var res = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha-segura-123", acceptTerms = true });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (client, email);
    }

    private async Task<string> CorrectAnswerAsync(Guid problemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.SessionProblems.AsNoTracking().SingleAsync(p => p.Id == problemId)).Content.Answer;
    }

    private async Task PlayAllCorrectAsync(HttpClient client, Guid sessionId)
    {
        while (true)
        {
            var next = await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}/next");
            if (next.GetProperty("done").GetBoolean()) break;
            var id = next.GetProperty("problem").GetProperty("id").GetGuid();
            await client.PostAsJsonAsync($"/api/sessions/{sessionId}/problems/{id}/answer", new { answer = await CorrectAnswerAsync(id) });
        }
    }

    private async Task<(HttpClient Teacher, Guid ClassId, HttpClient Guardian, Guid KidId)> ClassWithStudentAsync()
    {
        var (teacher, _) = await NewAccountAsync();
        var created = await (await teacher.PostAsJsonAsync("/api/classrooms", new { name = "5º ano B" })).Content.ReadFromJsonAsync<JsonElement>();
        var classId = created.GetProperty("id").GetGuid();
        var code = created.GetProperty("code").GetString();

        var (guardian, _) = await NewAccountAsync();
        var kid = await (await guardian.PostAsJsonAsync("/api/profiles", new { nickname = "Lia", avatar = "🦊" })).Content.ReadFromJsonAsync<JsonElement>();
        var kidId = kid.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await guardian.PostAsJsonAsync($"/api/profiles/{kidId}/classrooms", new { code })).StatusCode);
        return (teacher, classId, guardian, kidId);
    }

    [Fact]
    public async Task Teacher_assigns_a_stage_the_student_plays_it_even_if_locked_and_the_report_shows_it()
    {
        var (teacher, classId, guardian, kidId) = await ClassWithStudentAsync();

        // Uma etapa do nível Médio: bloqueada para quem acabou de começar.
        var stages = await teacher.GetFromJsonAsync<JsonElement>("/api/catalog/stages");
        var stage = stages.EnumerateArray().First(s => s.GetProperty("subjectSlug").GetString() == "matematica" && s.GetProperty("levelSlug").GetString() == "medio");
        var stageId = stage.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await guardian.PostAsJsonAsync($"/api/profiles/{kidId}/sessions", new { stageId })).StatusCode);

        var due = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)).ToString("yyyy-MM-dd");
        Assert.Equal(HttpStatusCode.BadRequest, (await teacher.PostAsJsonAsync($"/api/classrooms/{classId}/assignments", new { stageId, minStars = 5 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await teacher.PostAsJsonAsync($"/api/classrooms/{classId}/assignments", new { stageId, dueDate = "2020-01-01" })).StatusCode);
        var (stranger, _) = await NewAccountAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync($"/api/classrooms/{classId}/assignments", new { stageId })).StatusCode);

        var create = await teacher.PostAsJsonAsync($"/api/classrooms/{classId}/assignments", new { stageId, minStars = 1, dueDate = due, note = "Para sexta!" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var tasks = await guardian.GetFromJsonAsync<JsonElement>($"/api/profiles/{kidId}/assignments");
        var task = Assert.Single(tasks.EnumerateArray());
        Assert.False(task.GetProperty("done").GetBoolean());
        Assert.Equal("Para sexta!", task.GetProperty("note").GetString());

        // Agora a etapa abre por causa da tarefa.
        var start = await guardian.PostAsJsonAsync($"/api/profiles/{kidId}/sessions", new { stageId });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        await PlayAllCorrectAsync(guardian, (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid());

        tasks = await guardian.GetFromJsonAsync<JsonElement>($"/api/profiles/{kidId}/assignments");
        Assert.True(tasks[0].GetProperty("done").GetBoolean());

        var report = await teacher.GetFromJsonAsync<JsonElement>($"/api/classrooms/{classId}/report");
        var assignment = report.GetProperty("assignments")[0];
        Assert.Equal(1, assignment.GetProperty("done").GetInt32());
        Assert.Equal("feita", assignment.GetProperty("students")[0].GetProperty("status").GetString());
        var skills = report.GetProperty("skills").EnumerateArray().ToList();
        Assert.NotEmpty(skills);
        Assert.Contains(skills, s => s.GetProperty("bncc").GetArrayLength() > 0 && s.GetProperty("practiced").GetInt32() == 1);
        Assert.True(report.GetProperty("plan").GetProperty("canExport").GetBoolean()); // cobrança desligada: tudo liberado

        // Planilhas (Excel em português: BOM + ";").
        var csv = await teacher.GetAsync($"/api/classrooms/{classId}/export.csv?tipo=alunos");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Equal("text/csv", csv.Content.Headers.ContentType?.MediaType);
        var bytes = await csv.Content.ReadAsByteArrayAsync();
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        var text = Encoding.UTF8.GetString(bytes[3..]);
        Assert.StartsWith("Apelido;", text);
        Assert.Contains("Lia;", text);
        Assert.Contains("1 de 1", text);
        var skillsCsv = await teacher.GetStringAsync($"/api/classrooms/{classId}/export.csv?tipo=habilidades");
        Assert.Contains("BNCC", skillsCsv);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/classrooms/{classId}/export.csv")).StatusCode);

        var delete = await teacher.DeleteAsync($"/api/classrooms/{classId}/assignments/{assignment.GetProperty("id").GetGuid()}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(0, (await teacher.GetFromJsonAsync<JsonElement>($"/api/classrooms/{classId}/report")).GetProperty("assignments").GetArrayLength());
    }

    [Fact]
    public async Task Plans_are_granted_and_removed_by_the_terminal_command()
    {
        var (client, email) = await NewAccountAsync();
        var output = new StringWriter();

        Assert.Equal(1, await PlanCommand.RunAsync(factory.Services, ["plano", "conceder", email, "ouro"], output));
        Assert.Equal(0, await PlanCommand.RunAsync(factory.Services, ["plano", "conceder", email, "escola", "30", "Pix de teste"], output));
        var plan = await client.GetFromJsonAsync<JsonElement>("/api/me/plan");
        Assert.Equal("escola", plan.GetProperty("slug").GetString());
        Assert.False(plan.GetProperty("enforced").GetBoolean());
        Assert.Equal(JsonValueKind.String, plan.GetProperty("endsAt").ValueKind);

        Assert.Equal(0, await PlanCommand.RunAsync(factory.Services, ["plano", "listar"], output));
        Assert.Contains(email, output.ToString());

        Assert.Equal(0, await PlanCommand.RunAsync(factory.Services, ["plano", "remover", email], output));
        plan = await client.GetFromJsonAsync<JsonElement>("/api/me/plan");
        Assert.Equal("gratis", plan.GetProperty("slug").GetString());
        Assert.True(plan.GetProperty("canExport").GetBoolean()); // sem cobrança ligada, os recursos continuam liberados
    }

    [Theory]
    [InlineData("Lia", "Lia")]
    [InlineData("a;b", "\"a;b\"")]
    [InlineData("diz \"oi\"", "\"diz \"\"oi\"\"\"")]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("-5", "-5")]
    [InlineData("@soma", "'@soma")]
    public void Csv_cells_are_escaped_and_formulas_neutralized(string value, string expected) =>
        Assert.Equal(expected, ClassroomCsv.Cell(value));
}
