namespace Codexo.Core.Interfaces;

using Codexo.Core.Dtos.Hints;

public interface IHintService
{
    Task<HintResponseDto> RequestNextHintAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<AttemptEvaluationResultDto> SubmitAttemptAsync(
        string userId,
        Guid sessionId,
        SubmitAttemptRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HintResponseDto>> GetHintsForSessionAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<SessionHintStatusDto> GetSessionHintStatusAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
