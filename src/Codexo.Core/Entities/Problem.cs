namespace Codexo.Core.Entities;

using Codexo.Core.Enums;

public class Problem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Platform Platform { get; set; } = Platform.LeetCode;
    public string PlatformProblemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProblemSession> Sessions { get; set; } = new List<ProblemSession>();
}

