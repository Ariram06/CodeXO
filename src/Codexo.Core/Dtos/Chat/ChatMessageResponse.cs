namespace Codexo.Core.Dtos.Chat;

using Codexo.Core.Entities;

public record ChatMessageResponse(
    Guid ConversationId,
    Guid MessageId,
    string Role,
    string Content,
    MessageContextMetadata? Metadata,
    DateTimeOffset CreatedAtUtc);
