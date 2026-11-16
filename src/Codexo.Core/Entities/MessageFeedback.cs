namespace Codexo.Core.Entities;

public class MessageFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public string UserId { get; set; } = string.Empty;
    public bool IsHelpful { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
