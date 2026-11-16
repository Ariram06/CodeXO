namespace Codexo.Core.AI;

/// <summary>
/// Provider-independent contract for generating AI coding responses and chat assistance.
/// </summary>
public interface IAiClient
{
    Task<string> GenerateChatResponseAsync(
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
