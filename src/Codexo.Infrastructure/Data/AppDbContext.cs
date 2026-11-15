namespace Codexo.Infrastructure.Data;

using Codexo.Core.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<ProblemSession> ProblemSessions => Set<ProblemSession>();
    public DbSet<Hint> Hints => Set<Hint>();
    public DbSet<UserAttempt> UserAttempts => Set<UserAttempt>();
    public DbSet<HintFeedback> HintFeedbacks => Set<HintFeedback>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Discovers and applies all IEntityTypeConfiguration classes in Codexo.Infrastructure
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
