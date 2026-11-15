namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserAttemptConfiguration : IEntityTypeConfiguration<UserAttempt>
{
    public void Configure(EntityTypeBuilder<UserAttempt> builder)
    {
        builder.ToTable("UserAttempts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TargetLevel)
            .IsRequired();

        builder.Property(a => a.AttemptText)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired();

        builder.Property(a => a.RejectionReason)
            .IsRequired(false);

        // Foreign Key relationship to ProblemSession
        builder.HasOne(a => a.ProblemSession)
            .WithMany(s => s.Attempts)
            .HasForeignKey(a => a.ProblemSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on (ProblemSessionId, TargetLevel)
        builder.HasIndex(a => new { a.ProblemSessionId, a.TargetLevel });
    }
}
