namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
using Codexo.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(p => p.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.AutoUnlockL1)
            .IsRequired();

        builder.Property(p => p.ReasoningLevel)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("low");

        // Foreign Key relationship to Identity AppUser (1-to-1: Cascade preference cleanup on user deletion)
        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<UserPreference>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index: exactly one preference record per user
        builder.HasIndex(p => p.UserId)
            .IsUnique();
    }
}
