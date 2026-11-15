namespace Codexo.Core.Hints;

using Codexo.Core.Enums;

public record GuardResult
{
    public bool IsValid => Violations.Count == 0;
    public IReadOnlyList<GuardViolationType> Violations { get; init; } = Array.Empty<GuardViolationType>();
    public IReadOnlyList<string> Details { get; init; } = Array.Empty<string>();

    public static GuardResult Pass() => new();

    public static GuardResult Fail(GuardViolationType violation, string detail) =>
        new()
        {
            Violations = new[] { violation },
            Details = new[] { detail }
        };

    public static GuardResult Fail(IEnumerable<GuardViolationType> violations, IEnumerable<string> details) =>
        new()
        {
            Violations = violations.Distinct().ToList(),
            Details = details.ToList()
        };
}
