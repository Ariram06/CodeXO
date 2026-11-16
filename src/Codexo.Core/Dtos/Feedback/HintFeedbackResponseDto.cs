namespace Codexo.Core.Dtos.Feedback;

public record HintFeedbackResponseDto(
    Guid Id,
    Guid HintId,
    string UserId,
    bool IsHelpful,
    string? Comment,
    DateTimeOffset CreatedAt);
