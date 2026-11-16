namespace Codexo.Core.Dtos.Feedback;

public record MessageFeedbackResponseDto(
    Guid Id,
    Guid MessageId,
    bool IsHelpful,
    string? Comment,
    DateTimeOffset CreatedAt);
