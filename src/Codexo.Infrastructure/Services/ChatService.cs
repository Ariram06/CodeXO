namespace Codexo.Infrastructure.Services;

using System.Runtime.CompilerServices;
using System.Text;
using Codexo.Core.AI;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.Entities;
using Codexo.Core.Enums;
using Codexo.Core.Interfaces;
using Codexo.Core.Prompts;
using Codexo.Infrastructure.AI;
using Codexo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ChatService : IChatService
{
    private const int MaxCodeLength = 20000;
    private const int MaxProblemLength = 10000;
    private const int MaxSelectionLength = 4000;
    private const int MaxMessageLength = 4000;

    private readonly AppDbContext _dbContext;
    private readonly IAiClient _aiClient;
    private readonly ChatPromptBuilder _promptBuilder;
    private readonly AISettings _aiSettings;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        AppDbContext dbContext,
        IAiClient aiClient,
        ChatPromptBuilder promptBuilder,
        IOptions<AISettings> aiSettings,
        ILogger<ChatService> logger)
    {
        _dbContext = dbContext;
        _aiClient = aiClient;
        _promptBuilder = promptBuilder;
        _aiSettings = aiSettings.Value;
        _logger = logger;
    }

    public async Task<ChatMessageResponse> SendMessageAsync(
        string userId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Message);

        var sanitizedMessage = request.Message.Trim();
        if (sanitizedMessage.Length > MaxMessageLength)
        {
            sanitizedMessage = sanitizedMessage[..MaxMessageLength];
        }

        // Apply size caps and create sanitized PageContext
        var sanitizedContext = SanitizeAndCapContext(request.Context);

        // Fetch or create conversation
        Conversation conversation;
        if (request.ConversationId.HasValue)
        {
            var existing = await _dbContext.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.UserId == userId, cancellationToken);

            if (existing == null)
            {
                throw new KeyNotFoundException($"Conversation '{request.ConversationId.Value}' was not found.");
            }

            conversation = existing;
        }
        else
        {
            var title = !string.IsNullOrWhiteSpace(sanitizedContext?.Title)
                ? sanitizedContext.Title.Trim()
                : (sanitizedMessage.Length > 50 ? sanitizedMessage[..47] + "..." : sanitizedMessage);

            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            _dbContext.Conversations.Add(conversation);
        }

        // Record User Message
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = sanitizedMessage,
            Metadata = ExtractMetadata(sanitizedContext),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Messages.Add(userMessage);
        conversation.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Build prompt with system instructions, last 12 turns, and active context data
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var history = _promptBuilder.FormatHistory(conversation.Messages);
        var userPrompt = _promptBuilder.BuildUserPrompt(sanitizedMessage, sanitizedContext);

        // Generate response through AI Client
        var assistantContent = await _aiClient.GenerateChatResponseAsync(
            systemPrompt,
            history,
            userPrompt,
            cancellationToken);

        var assistantMetadata = ExtractMetadata(sanitizedContext);

        // Record Assistant Message
        var assistantMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = assistantContent,
            Model = _aiSettings.Model,
            Metadata = assistantMetadata,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Messages.Add(assistantMessage);
        conversation.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Chat message processed for user {UserId} in conversation {ConversationId}. Role: Assistant",
            userId, conversation.Id);

        return new ChatMessageResponse(
            ConversationId: conversation.Id,
            MessageId: assistantMessage.Id,
            Role: "assistant",
            Content: assistantContent,
            Metadata: assistantMetadata,
            CreatedAtUtc: assistantMessage.CreatedAtUtc);
    }

    public async IAsyncEnumerable<ChatStreamChunk> StreamMessageAsync(
        string userId,
        SendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Message);

        var sanitizedMessage = request.Message.Trim();
        if (sanitizedMessage.Length > MaxMessageLength)
        {
            sanitizedMessage = sanitizedMessage[..MaxMessageLength];
        }

        var sanitizedContext = SanitizeAndCapContext(request.Context);

        Conversation conversation;
        if (request.ConversationId.HasValue)
        {
            var existing = await _dbContext.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.UserId == userId, cancellationToken);

            if (existing == null)
            {
                throw new KeyNotFoundException($"Conversation '{request.ConversationId.Value}' was not found.");
            }

            conversation = existing;
        }
        else
        {
            var title = !string.IsNullOrWhiteSpace(sanitizedContext?.Title)
                ? sanitizedContext.Title.Trim()
                : (sanitizedMessage.Length > 50 ? sanitizedMessage[..47] + "..." : sanitizedMessage);

            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            _dbContext.Conversations.Add(conversation);
        }

        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = sanitizedMessage,
            Metadata = ExtractMetadata(sanitizedContext),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Messages.Add(userMessage);
        conversation.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var history = _promptBuilder.FormatHistory(conversation.Messages);
        var userPrompt = _promptBuilder.BuildUserPrompt(sanitizedMessage, sanitizedContext);

        var assistantMessageId = Guid.NewGuid();
        var accumulated = new StringBuilder();
        var isCompleted = false;

        try
        {
            await using var enumerator = _aiClient.StreamChatResponseAsync(systemPrompt, history, userPrompt, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                var delta = enumerator.Current;
                accumulated.Append(delta);
                yield return new ChatStreamChunk(
                    conversation.Id,
                    assistantMessageId,
                    delta,
                    false,
                    _aiSettings.Model);
            }

            isCompleted = true;
            yield return new ChatStreamChunk(
                conversation.Id,
                assistantMessageId,
                null,
                true,
                _aiSettings.Model);
        }
        finally
        {
            var finalContent = accumulated.ToString();
            if (!isCompleted && finalContent.Length > 0)
            {
                finalContent += " [Generation stopped]";
            }

            if (finalContent.Length > 0)
            {
                try
                {
                    var assistantMessage = new Message
                    {
                        Id = assistantMessageId,
                        ConversationId = conversation.Id,
                        Role = MessageRole.Assistant,
                        Content = finalContent,
                        Model = _aiSettings.Model,
                        Metadata = ExtractMetadata(sanitizedContext),
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    };

                    _dbContext.Messages.Add(assistantMessage);
                    conversation.UpdatedAtUtc = DateTimeOffset.UtcNow;

                    // Safely commit partial or completed response using uncancelled token
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    _logger.LogInformation("Successfully saved assistant message {MessageId} (completed: {IsCompleted})", assistantMessageId, isCompleted);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save assistant message {MessageId} in finally block", assistantMessageId);
                }
            }
        }
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => new ConversationSummaryDto(
                c.Id,
                c.Title,
                c.CreatedAtUtc,
                c.UpdatedAtUtc,
                c.Messages.Count))
            .ToListAsync(cancellationToken);

        return conversations;
    }

    public async Task<ConversationDetailDto> GetConversationByIdAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

        if (conversation == null)
        {
            throw new KeyNotFoundException($"Conversation '{conversationId}' was not found.");
        }

        var messages = conversation.Messages
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new ChatMessageResponse(
                m.ConversationId,
                m.Id,
                m.Role.ToString().ToLowerInvariant(),
                m.Content,
                m.Metadata,
                m.CreatedAtUtc))
            .ToList();

        return new ConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc,
            messages);
    }

    public async Task DeleteConversationAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

        if (conversation == null)
        {
            throw new KeyNotFoundException($"Conversation '{conversationId}' was not found.");
        }

        _dbContext.Conversations.Remove(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await _dbContext.Conversations
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        if (conversations.Count > 0)
        {
            _dbContext.Conversations.RemoveRange(conversations);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static PageContext? SanitizeAndCapContext(PageContext? context)
    {
        if (context == null)
        {
            return null;
        }

        var isTruncated = context.IsTruncated;

        var code = context.Code;
        if (code != null && code.Length > MaxCodeLength)
        {
            code = code[..MaxCodeLength];
            isTruncated = true;
        }

        var problemText = context.ProblemText;
        if (problemText != null && problemText.Length > MaxProblemLength)
        {
            problemText = problemText[..MaxProblemLength];
            isTruncated = true;
        }

        var selection = context.Selection;
        if (selection != null && selection.Length > MaxSelectionLength)
        {
            selection = selection[..MaxSelectionLength];
            isTruncated = true;
        }

        return new PageContext(
            Platform: context.Platform,
            Url: context.Url,
            Title: context.Title,
            ProblemText: problemText,
            Language: context.Language,
            Code: code,
            Selection: selection,
            ErrorText: context.ErrorText,
            IsTruncated: isTruncated);
    }

    private static MessageContextMetadata? ExtractMetadata(PageContext? context)
    {
        if (context == null)
        {
            return null;
        }

        string? host = null;
        if (!string.IsNullOrWhiteSpace(context.Url) && Uri.TryCreate(context.Url, UriKind.Absolute, out var uri))
        {
            host = uri.Host;
        }

        var lineCount = context.Code != null
            ? context.Code.Split('\n').Length
            : (int?)null;

        return new MessageContextMetadata
        {
            Platform = context.Platform,
            Host = host,
            Title = context.Title,
            Language = context.Language,
            CodeLineCount = lineCount,
            IsTruncated = context.IsTruncated
        };
    }
}
