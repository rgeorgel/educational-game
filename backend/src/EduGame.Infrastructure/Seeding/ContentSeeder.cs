using EduGame.Domain.Content;
using EduGame.Domain.Progression;
using EduGame.Problems;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Infrastructure.Seeding;

/// <summary>
/// Sincroniza o <see cref="ContentCatalog"/> com o banco (insere e atualiza pelo slug).
/// É idempotente: roda a cada inicialização. Itens removidos do catálogo não são apagados
/// (o histórico dos jogadores pode depender deles); geradores removidos são desativados.
/// </summary>
public static class ContentSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var def in ContentCatalog.Subjects)
        {
            var subject = await UpsertSubjectAsync(db, def, ct);
            await db.SaveChangesAsync(ct);

            var levels = subject.Levels.ToDictionary(l => l.Slug);
            var skills = await UpsertSkillsAsync(db, subject, def, levels, ct);
            await db.SaveChangesAsync(ct);

            await UpsertTemplatesAsync(db, skills.Values, ct);
            await UpsertCuratedAsync(db, skills, ct);
            await UpsertRegionsAsync(db, subject, def, levels, skills, ct);
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task<Subject> UpsertSubjectAsync(AppDbContext db, ContentCatalog.SubjectDef def, CancellationToken ct)
    {
        var subject = await db.Subjects.Include(s => s.Levels).SingleOrDefaultAsync(s => s.Slug == def.Slug, ct);
        if (subject is null)
        {
            subject = new Subject { Slug = def.Slug, Name = def.Name };
            db.Subjects.Add(subject);
        }
        subject.Name = def.Name;
        subject.Icon = def.Icon;
        subject.Order = def.Order;

        foreach (var levelDef in LevelDefinitions.All)
        {
            var level = subject.Levels.SingleOrDefault(l => l.Slug == levelDef.Slug);
            if (level is null)
            {
                level = new Level { Slug = levelDef.Slug, Name = levelDef.Name };
                subject.Levels.Add(level);
            }
            level.Name = levelDef.Name;
            level.Icon = levelDef.Icon;
            level.Order = levelDef.Order;
            level.MinRating = levelDef.MinRating;
        }
        return subject;
    }

    private static async Task<Dictionary<string, Skill>> UpsertSkillsAsync(
        AppDbContext db, Subject subject, ContentCatalog.SubjectDef def, Dictionary<string, Level> levels, CancellationToken ct)
    {
        var existing = await db.Skills.Include(s => s.Templates).Where(s => s.SubjectId == subject.Id).ToDictionaryAsync(s => s.Slug, ct);
        var order = 0;
        foreach (var skillDef in def.Skills)
        {
            if (!existing.TryGetValue(skillDef.Slug, out var skill))
            {
                skill = new Skill { Slug = skillDef.Slug, Name = skillDef.Name, SubjectId = subject.Id };
                db.Skills.Add(skill);
                existing[skillDef.Slug] = skill;
            }
            skill.Name = skillDef.Name;
            skill.Description = skillDef.Description;
            skill.LevelId = levels[skillDef.Level].Id;
            skill.Order = ++order;
        }
        return existing;
    }

    private static Task UpsertTemplatesAsync(AppDbContext db, IEnumerable<Skill> skills, CancellationToken ct)
    {
        foreach (var skill in skills)
        {
            var keys = ProblemFactory.Default.ForSkill(skill.Slug).Select(g => g.Key).ToHashSet();
            if (CuratedCatalog.ForSkill(skill.Slug).Any()) keys.Add(ProblemFactory.CuratedKey);
            foreach (var template in skill.Templates) template.Active = keys.Contains(template.GeneratorKey);
            foreach (var key in keys.Where(k => skill.Templates.All(t => t.GeneratorKey != k)))
                skill.Templates.Add(new ProblemTemplate { GeneratorKey = key, SkillId = skill.Id });
        }
        return Task.CompletedTask;
    }

    private static async Task UpsertCuratedAsync(AppDbContext db, Dictionary<string, Skill> skills, CancellationToken ct)
    {
        var existing = await db.CuratedProblems.ToDictionaryAsync(p => p.Slug, ct);
        foreach (var def in CuratedCatalog.All.Where(d => skills.ContainsKey(d.Skill)))
        {
            if (!existing.TryGetValue(def.Slug, out var problem))
            {
                problem = new CuratedProblem { Slug = def.Slug, Content = def.Content };
                db.CuratedProblems.Add(problem);
            }
            problem.SkillId = skills[def.Skill].Id;
            problem.Difficulty = def.Difficulty;
            problem.Content = def.Content;
            problem.Active = true;
        }
    }

    private static async Task UpsertRegionsAsync(
        AppDbContext db, Subject subject, ContentCatalog.SubjectDef def,
        Dictionary<string, Level> levels, Dictionary<string, Skill> skills, CancellationToken ct)
    {
        var regions = await db.Regions.Include(r => r.Stages).Where(r => r.SubjectId == subject.Id).ToDictionaryAsync(r => r.Slug, ct);
        var regionOrder = 0;
        foreach (var regionDef in def.Regions)
        {
            if (!regions.TryGetValue(regionDef.Slug, out var region))
            {
                region = new Region { Slug = regionDef.Slug, Name = regionDef.Name, SubjectId = subject.Id };
                db.Regions.Add(region);
            }
            region.Name = regionDef.Name;
            region.Description = regionDef.Description;
            region.Icon = regionDef.Icon;
            region.LevelId = levels[regionDef.Level].Id;
            region.Order = ++regionOrder;

            var stageOrder = 0;
            foreach (var stageDef in regionDef.Stages)
            {
                var stage = region.Stages.SingleOrDefault(s => s.Slug == stageDef.Slug);
                if (stage is null)
                {
                    stage = new Stage { Slug = stageDef.Slug, Name = stageDef.Name };
                    region.Stages.Add(stage);
                }
                stage.Name = stageDef.Name;
                stage.Order = ++stageOrder;
                stage.ProblemCount = stageDef.ProblemCount;
                stage.SkillIds = [.. stageDef.Skills.Select(slug => skills[slug].Id)];
            }
        }
    }
}
