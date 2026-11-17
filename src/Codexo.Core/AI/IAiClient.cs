namespace Codexo.Core.AI;

/// <summary>
/// Provider-independent contract for generating AI coding responses and chat assistance.
/// Supports both synchronous non-streaming and asynchronous streaming responses.
/// </summary>
public interface IAiClient
{
    Task<string> GenerateChatResponseAsync(
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        string userPrompt,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamChatResponseAsync(
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
