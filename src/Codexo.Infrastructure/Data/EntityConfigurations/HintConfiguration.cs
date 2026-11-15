namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HintConfiguration : IEntityTypeConfiguration<Hint>
{
    public void Configure(EntityTypeBuilder<Hint> builder)
    {
        builder.ToTable("Hints");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Level)
            .IsRequired();

        builder.Property(h => h.Content)
            .IsRequired();

        builder.Property(h => h.Explanation)
            .IsRequired();

        builder.Property(h => h.IsFallback)
            .IsRequired();

        builder.Property(h => h.GenerationAttempts)
            .IsRequired();

        // Foreign Key relationship to ProblemSession
        builder.HasOne(h => h.ProblemSession)
            .WithMany(s => s.Hints)
            .HasForeignKey(h => h.ProblemSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on (ProblemSessionId, Level) to retrieve hints for a session level
        builder.HasIndex(h => new { h.ProblemSessionId, h.Level });
    }
}
