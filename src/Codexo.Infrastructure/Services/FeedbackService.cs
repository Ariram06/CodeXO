namespace Codexo.Infrastructure.Services;

using Codexo.Core.Dtos.Feedback;
using Codexo.Core.Entities;
using Codexo.Core.Interfaces;
using Codexo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(AppDbContext dbContext, ILogger<FeedbackService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<MessageFeedbackResponseDto> SubmitFeedbackAsync(
        string userId,
        Guid messageId,
        SubmitFeedbackRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(request);

        // Verify message exists and belongs to a conversation owned by the authenticated user
        var message = await _dbContext.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.Conversation != null && m.Conversation.UserId == userId, cancellationToken);

        if (message == null)
        {
            throw new KeyNotFoundException($"Message {messageId} was not found for the authenticated user.");
        }

        var trimmedComment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        // Upsert pattern: one feedback per user per message
        var existingFeedback = await _dbContext.MessageFeedbacks
            .FirstOrDefaultAsync(f => f.MessageId == messageId && f.UserId == userId, cancellationToken);

        if (existingFeedback != null)
        {
            existingFeedback.IsHelpful = request.IsHelpful;
            existingFeedback.Comment = trimmedComment;

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated feedback for message {MessageId} by user {UserId}. Helpful: {IsHelpful}", messageId, userId, request.IsHelpful);

            return MapToDto(existingFeedback);
        }

        var newFeedback = new MessageFeedback
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            UserId = userId,
            IsHelpful = request.IsHelpful,
            Comment = trimmedComment,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.MessageFeedbacks.Add(newFeedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created feedback for message {MessageId} by user {UserId}. Helpful: {IsHelpful}", messageId, userId, request.IsHelpful);

        return MapToDto(newFeedback);
    }

    public async Task<MessageFeedbackResponseDto?> GetUserFeedbackForMessageAsync(
        string userId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var feedback = await _dbContext.MessageFeedbacks
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.MessageId == messageId && f.UserId == userId, cancellationToken);

        return feedback == null ? null : MapToDto(feedback);
    }

    private static MessageFeedbackResponseDto MapToDto(MessageFeedback feedback) =>
        new(
            Id: feedback.Id,
            MessageId: feedback.MessageId,
            IsHelpful: feedback.IsHelpful,
            Comment: feedback.Comment,
            CreatedAt: feedback.CreatedAtUtc);
}
