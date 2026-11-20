using EduGame.Domain.Content;
using EduGame.Domain.Progression;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Infrastructure.Seeding;

/// <summary>
/// Garante que o conteúdo base (tópicos e níveis) exista no banco.
/// É idempotente: pode rodar a cada inicialização.
/// </summary>
public static class ContentSeeder
{
    private static readonly (string Slug, string Name, string Icon, int Order)[] Subjects =
    [
        ("matematica", "Matemática", "➗", 1),
        ("logica", "Lógica", "🧩", 2),
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var (slug, name, icon, order) in Subjects)
        {
            var subject = await db.Subjects.Include(s => s.Levels).SingleOrDefaultAsync(s => s.Slug == slug, ct);
            if (subject is null)
            {
                subject = new Subject { Slug = slug, Name = name };
                db.Subjects.Add(subject);
            }
            subject.Name = name;
            subject.Icon = icon;
            subject.Order = order;

            foreach (var def in LevelDefinitions.All)
            {
                var level = subject.Levels.SingleOrDefault(l => l.Slug == def.Slug);
                if (level is null)
                {
                    level = new Level { Slug = def.Slug, Name = def.Name };
                    subject.Levels.Add(level);
                }
                level.Name = def.Name;
                level.Icon = def.Icon;
                level.Order = def.Order;
                level.MinRating = def.MinRating;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
