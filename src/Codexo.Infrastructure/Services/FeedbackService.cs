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

    public async Task<HintFeedbackResponseDto> SubmitFeedbackAsync(
        string userId,
        Guid hintId,
        SubmitFeedbackRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(request);

        // Verify hint exists and belongs to a session owned by the authenticated user
        var hint = await _dbContext.Hints
            .Include(h => h.ProblemSession)
            .FirstOrDefaultAsync(h => h.Id == hintId && h.ProblemSession != null && h.ProblemSession.UserId == userId, cancellationToken);

        if (hint == null)
        {
            throw new KeyNotFoundException($"Hint {hintId} was not found for the authenticated user.");
        }

        var trimmedComment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        // One feedback per user per hint (upsert pattern)
        var existingFeedback = await _dbContext.HintFeedbacks
            .FirstOrDefaultAsync(f => f.HintId == hintId && f.UserId == userId, cancellationToken);

        if (existingFeedback != null)
        {
            existingFeedback.IsHelpful = request.IsHelpful;
            existingFeedback.Comment = trimmedComment;

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated feedback for hint {HintId} by user {UserId}. Helpful: {IsHelpful}", hintId, userId, request.IsHelpful);

            return MapToDto(existingFeedback);
        }

        var newFeedback = new HintFeedback
        {
            Id = Guid.NewGuid(),
            HintId = hintId,
            UserId = userId,
            IsHelpful = request.IsHelpful,
            Comment = trimmedComment,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.HintFeedbacks.Add(newFeedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created feedback for hint {HintId} by user {UserId}. Helpful: {IsHelpful}", hintId, userId, request.IsHelpful);

        return MapToDto(newFeedback);
    }

    public async Task<HintFeedbackResponseDto?> GetUserFeedbackForHintAsync(
        string userId,
        Guid hintId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var feedback = await _dbContext.HintFeedbacks
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.HintId == hintId && f.UserId == userId, cancellationToken);

        return feedback == null ? null : MapToDto(feedback);
    }

    private static HintFeedbackResponseDto MapToDto(HintFeedback feedback) =>
        new(
            Id: feedback.Id,
            HintId: feedback.HintId,
            UserId: feedback.UserId,
            IsHelpful: feedback.IsHelpful,
            Comment: feedback.Comment,
            CreatedAt: feedback.CreatedAt);
}
