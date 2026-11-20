using System.Text.Json;
using EduGame.Domain.Content;
using EduGame.Domain.Players;
using EduGame.Domain.Sessions;
using EduGame.Infrastructure.Identity;
using EduGame.Problems;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EduGame.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Level> Levels => Set<Level>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<ProblemTemplate> ProblemTemplates => Set<ProblemTemplate>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Stage> Stages => Set<Stage>();

    public DbSet<PlayerProfile> Profiles => Set<PlayerProfile>();
    public DbSet<SkillMastery> SkillMasteries => Set<SkillMastery>();
    public DbSet<SubjectProgress> SubjectProgresses => Set<SubjectProgress>();
    public DbSet<StageProgress> StageProgresses => Set<StageProgress>();

    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<SessionProblem> SessionProblems => Set<SessionProblem>();
    public DbSet<Attempt> Attempts => Set<Attempt>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Tabelas do Identity em snake_case, como o resto do banco.
        b.Entity<AppUser>().ToTable("users");
        b.Entity<IdentityRole<Guid>>().ToTable("roles");
        b.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        b.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        b.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        b.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        b.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        // ---------- Conteúdo ----------
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

        b.Entity<Skill>(e =>
        {
            e.HasIndex(x => new { x.SubjectId, x.Slug }).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Description).HasMaxLength(512);
            e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
            e.HasOne(x => x.Level).WithMany().HasForeignKey(x => x.LevelId);
        });

        b.Entity<ProblemTemplate>(e =>
        {
            e.HasIndex(x => new { x.SkillId, x.GeneratorKey }).IsUnique();
            e.Property(x => x.GeneratorKey).HasMaxLength(64);
            e.HasOne(x => x.Skill).WithMany(s => s.Templates).HasForeignKey(x => x.SkillId);
        });

        b.Entity<Region>(e =>
        {
            e.HasIndex(x => new { x.SubjectId, x.Slug }).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.Icon).HasMaxLength(16);
            e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
            e.HasOne(x => x.Level).WithMany().HasForeignKey(x => x.LevelId);
        });

        b.Entity<Stage>(e =>
        {
            e.HasIndex(x => new { x.RegionId, x.Slug }).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.HasOne(x => x.Region).WithMany(r => r.Stages).HasForeignKey(x => x.RegionId);
        });

        // ---------- Jogadores ----------
        b.Entity<PlayerProfile>(e =>
        {
            e.ToTable("player_profiles");
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Nickname).HasMaxLength(20);
            e.Property(x => x.Avatar).HasMaxLength(16);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SkillMastery>(e =>
        {
            e.ToTable("skill_mastery");
            e.HasKey(x => new { x.ProfileId, x.SkillId });
            e.HasOne<PlayerProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Skill>().WithMany().HasForeignKey(x => x.SkillId);
        });

        b.Entity<SubjectProgress>(e =>
        {
            e.ToTable("subject_progress");
            e.HasKey(x => new { x.ProfileId, x.SubjectId });
            e.HasOne<PlayerProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Subject>().WithMany().HasForeignKey(x => x.SubjectId);
            e.HasOne<Level>().WithMany().HasForeignKey(x => x.CurrentLevelId);
        });

        b.Entity<StageProgress>(e =>
        {
            e.ToTable("stage_progress");
            e.HasKey(x => new { x.ProfileId, x.StageId });
            e.HasOne<PlayerProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Stage>().WithMany().HasForeignKey(x => x.StageId);
        });

        // ---------- Sessões ----------
        b.Entity<GameSession>(e =>
        {
            e.HasIndex(x => new { x.ProfileId, x.StartedAt });
            e.Property(x => x.Mode).HasMaxLength(32);
            e.Ignore(x => x.IsFinished);
            e.HasOne<PlayerProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Stage>().WithMany().HasForeignKey(x => x.StageId);
            e.HasMany(x => x.Problems).WithOne().HasForeignKey(p => p.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SessionProblem>(e =>
        {
            e.HasIndex(x => new { x.SessionId, x.Order }).IsUnique();
            e.Property(x => x.GeneratorKey).HasMaxLength(64);
            e.HasOne<Skill>().WithMany().HasForeignKey(x => x.SkillId);
            e.Property(x => x.Content)
                .HasColumnType("jsonb")
                .HasConversion(
                    c => JsonSerializer.Serialize(c, Json),
                    s => JsonSerializer.Deserialize<ProblemContent>(s, Json)!,
                    new ValueComparer<ProblemContent>(
                        (a, c) => JsonSerializer.Serialize(a, Json) == JsonSerializer.Serialize(c, Json),
                        c => JsonSerializer.Serialize(c, Json).GetHashCode(),
                        c => c));
            e.HasOne(x => x.Attempt).WithOne().HasForeignKey<Attempt>(a => a.SessionProblemId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Attempt>(e =>
        {
            e.HasIndex(x => x.SessionProblemId).IsUnique();
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.Answer).HasMaxLength(64);
        });
    }
}
