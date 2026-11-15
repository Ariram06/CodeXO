namespace Codexo.Core.Entities;

using Codexo.Core.Enums;

public class ProblemSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid ProblemId { get; set; }
    public Problem? Problem { get; set; }
    public HintLevel CurrentLevel { get; set; } = HintLevel.None;
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Hint> Hints { get; set; } = new List<Hint>();
    public ICollection<UserAttempt> Attempts { get; set; } = new List<UserAttempt>();
}
