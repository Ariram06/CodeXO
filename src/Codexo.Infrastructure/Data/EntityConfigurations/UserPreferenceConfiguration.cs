namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
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
            .HasMaxLength(100);

        builder.Property(p => p.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.AutoUnlockL1)
            .IsRequired();

        // Unique index: exactly one preference record per user
        builder.HasIndex(p => p.UserId)
            .IsUnique();
    }
}
