namespace Codexo.Core.Entities;

using Codexo.Core.Enums;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    public string? Model { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }

    public MessageContextMetadata? Metadata { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public MessageFeedback? Feedback { get; set; }
}
