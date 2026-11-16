namespace Codexo.Core.Dtos.Hints;

using Codexo.Core.Enums;

public record AttemptEvaluationResultDto(
    Guid AttemptId,
    bool Unlocked,
    HintLevel TargetLevel,
    AttemptStatus Status,
    string? RejectionReason,
    string FeedbackMessage);
