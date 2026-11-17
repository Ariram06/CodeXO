namespace Codexo.Core.Dtos.Chat;

public record ChatStreamChunk(
    Guid ConversationId,
    Guid MessageId,
    string? Delta,
    bool IsDone,
    string? Model = null,
    string? Error = null);
