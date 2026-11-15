namespace Codexo.Core.Entities;

using Codexo.Core.Enums;

public class UserAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProblemSessionId { get; set; }
    public ProblemSession? ProblemSession { get; set; }
    public HintLevel TargetLevel { get; set; }
    public string AttemptText { get; set; } = string.Empty;
    public AttemptStatus Status { get; set; }
    public AttemptRejectionReason? RejectionReason { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}
