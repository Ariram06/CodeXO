namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProblemSessionConfiguration : IEntityTypeConfiguration<ProblemSession>
{
    public void Configure(EntityTypeBuilder<ProblemSession> builder)
    {
        builder.ToTable("ProblemSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.CurrentLevel)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired();

        // Foreign Key relationship to Problem (Restrict deletion if user sessions exist)
        builder.HasOne(s => s.Problem)
            .WithMany(p => p.Sessions)
            .HasForeignKey(s => s.ProblemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on (UserId, ProblemId) to quickly retrieve a user's session for a specific problem
        builder.HasIndex(s => new { s.UserId, s.ProblemId });

        // Index on (UserId, Status) to query active sessions
        builder.HasIndex(s => new { s.UserId, s.Status });
    }
}
