namespace Codexo.Core.Dtos.Chat;

public record SendMessageRequest(
    Guid? ConversationId,
    string Message,
    PageContext? Context);
