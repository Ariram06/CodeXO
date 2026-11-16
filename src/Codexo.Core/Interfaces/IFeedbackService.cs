namespace Codexo.Core.Interfaces;

using Codexo.Core.Dtos.Feedback;

public interface IFeedbackService
{
    Task<MessageFeedbackResponseDto> SubmitFeedbackAsync(
        string userId,
        Guid messageId,
        SubmitFeedbackRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MessageFeedbackResponseDto?> GetUserFeedbackForMessageAsync(
        string userId,
        Guid messageId,
        CancellationToken cancellationToken = default);
}
