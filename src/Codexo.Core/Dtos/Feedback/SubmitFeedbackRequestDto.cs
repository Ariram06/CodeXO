namespace Codexo.Core.Dtos.Feedback;

public record SubmitFeedbackRequestDto(
    bool IsHelpful,
    string? Comment);
