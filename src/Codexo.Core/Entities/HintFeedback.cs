namespace Codexo.Core.Entities;

public class HintFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HintId { get; set; }
    public Hint? Hint { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsHelpful { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
