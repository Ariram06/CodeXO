namespace Codexo.Core.Hints;

using Codexo.Core.Enums;

public record AttemptValidationResult
{
    public bool IsApproved { get; init; }
    public AttemptRejectionReason RejectionReason { get; init; } = AttemptRejectionReason.None;
    public string FeedbackMessage { get; init; } = string.Empty;

    public static AttemptValidationResult Success() =>
        new() { IsApproved = true, RejectionReason = AttemptRejectionReason.None, FeedbackMessage = "Attempt approved." };

    public static AttemptValidationResult Reject(AttemptRejectionReason reason, string feedback) =>
        new() { IsApproved = false, RejectionReason = reason, FeedbackMessage = feedback };
}

