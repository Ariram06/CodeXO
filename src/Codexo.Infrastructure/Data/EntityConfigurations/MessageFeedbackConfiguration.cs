namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MessageFeedbackConfiguration : IEntityTypeConfiguration<MessageFeedback>
{
    public void Configure(EntityTypeBuilder<MessageFeedback> builder)
    {
        builder.ToTable("MessageFeedbacks");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(f => f.IsHelpful)
            .IsRequired();

        builder.Property(f => f.Comment)
            .HasMaxLength(500);

        builder.Property(f => f.CreatedAtUtc)
            .IsRequired();

        // Enforce one feedback per user per message
        builder.HasIndex(f => new { f.MessageId, f.UserId })
            .IsUnique();
    }
}
