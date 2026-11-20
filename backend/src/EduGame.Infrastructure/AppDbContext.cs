using EduGame.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace EduGame.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Level> Levels => Set<Level>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Subject>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Icon).HasMaxLength(16);
        });

        b.Entity<Level>(e =>
        {
            e.HasIndex(x => new { x.SubjectId, x.Slug }).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Icon).HasMaxLength(16);
            e.HasOne(x => x.Subject).WithMany(s => s.Levels).HasForeignKey(x => x.SubjectId);
        });
    }
}
