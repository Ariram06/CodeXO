namespace Codexo.Core.Dtos.Chat;

public record ConversationDetailDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ChatMessageResponse> Messages);
