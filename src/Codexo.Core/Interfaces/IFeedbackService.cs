namespace Codexo.Core.Interfaces;

using Codexo.Core.Dtos.Feedback;

public interface IFeedbackService
{
    Task<HintFeedbackResponseDto> SubmitFeedbackAsync(
        string userId,
        Guid hintId,
        SubmitFeedbackRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HintFeedbackResponseDto?> GetUserFeedbackForHintAsync(
        string userId,
        Guid hintId,
        CancellationToken cancellationToken = default);
}
