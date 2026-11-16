namespace Codexo.Infrastructure.Data.EntityConfigurations;

using Codexo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.Model)
            .HasMaxLength(128);

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAtUtc });

        // Owned metadata entity - persists structural attributes ONLY, NEVER raw code
        builder.OwnsOne(m => m.Metadata, meta =>
        {
            meta.Property(p => p.Platform).HasMaxLength(64).HasColumnName("ContextPlatform");
            meta.Property(p => p.Host).HasMaxLength(256).HasColumnName("ContextHost");
            meta.Property(p => p.Title).HasMaxLength(256).HasColumnName("ContextTitle");
            meta.Property(p => p.Language).HasMaxLength(64).HasColumnName("ContextLanguage");
            meta.Property(p => p.CodeLineCount).HasColumnName("ContextCodeLineCount");
            meta.Property(p => p.IsTruncated).HasColumnName("ContextIsTruncated");
        });

        builder.HasOne(m => m.Feedback)
            .WithOne(f => f.Message)
            .HasForeignKey<MessageFeedback>(f => f.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
