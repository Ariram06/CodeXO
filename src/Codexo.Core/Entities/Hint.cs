namespace Codexo.Core.Entities;

using Codexo.Core.Enums;

public class Hint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProblemSessionId { get; set; }
    public ProblemSession? ProblemSession { get; set; }
    public HintLevel Level { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public bool IsFallback { get; set; }
    public int GenerationAttempts { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<HintFeedback> Feedbacks { get; set; } = new List<HintFeedback>();
}
