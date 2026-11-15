namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HintFeedbackConfiguration : IEntityTypeConfiguration<HintFeedback>
{
    public void Configure(EntityTypeBuilder<HintFeedback> builder)
    {
        builder.ToTable("HintFeedbacks");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.IsHelpful)
            .IsRequired();

        builder.Property(f => f.Comment)
            .HasMaxLength(1000)
            .IsRequired(false);

        // Foreign Key relationship to Hint
        builder.HasOne(f => f.Hint)
            .WithMany(h => h.Feedbacks)
            .HasForeignKey(f => f.HintId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on (HintId, UserId)
        builder.HasIndex(f => new { f.HintId, f.UserId });
    }
}
