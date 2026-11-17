namespace Codexo.Core.Interfaces;

using Codexo.Core.Dtos.Chat;

public interface IChatService
{
    Task<ChatMessageResponse> SendMessageAsync(
        string userId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatStreamChunk> StreamMessageAsync(
        string userId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default);
}
