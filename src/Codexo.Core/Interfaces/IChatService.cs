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

    Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ConversationDetailDto> GetConversationByIdAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task DeleteConversationAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task ClearConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
