namespace Codexo.Core.AI;

/// <summary>
/// Provider-independent contract for generating pedagogical DSA hints.
/// Implementations must return structured JSON conforming to AiHintResponse.
/// </summary>
public interface IAiClient
{
    Task<AiHintResponse> GenerateHintAsync(AiPromptRequest request, CancellationToken cancellationToken = default);
}
