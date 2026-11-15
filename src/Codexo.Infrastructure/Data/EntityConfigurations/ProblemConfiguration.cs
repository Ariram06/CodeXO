namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        builder.ToTable("Problems");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PlatformProblemId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(p => p.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .IsRequired();

        builder.Property(p => p.Difficulty)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Platform)
            .IsRequired();

        // Unique composite index: A platform problem ID is unique per platform
        builder.HasIndex(p => new { p.Platform, p.PlatformProblemId })
            .IsUnique();

        // Index on slug for rapid problem lookup by URL slug
        builder.HasIndex(p => p.Slug);
    }
}
