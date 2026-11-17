namespace Codexo.Core.Dtos.Chat;

public record ConversationSummaryDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int MessageCount);
