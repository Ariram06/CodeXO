namespace Codexo.Api.Controllers;

using System.Security.Claims;
using Codexo.Core.Dtos.Feedback;
using Codexo.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly IValidator<SubmitFeedbackRequestDto> _validator;

    public FeedbackController(
        IFeedbackService feedbackService,
        IValidator<SubmitFeedbackRequestDto> validator)
    {
        _feedbackService = feedbackService;
        _validator = validator;
    }

    /// <summary>
    /// Submits helpfulness feedback (thumbs up/down and optional comment) for a specific assistant message.
    /// Idempotent / upsert: updates existing feedback if already submitted by this user for the message.
    /// </summary>
    [HttpPost("api/messages/{messageId:guid}/feedback")]
    public async Task<IActionResult> SubmitFeedback(
        Guid messageId,
        [FromBody] SubmitFeedbackRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        try
        {
            var feedback = await _feedbackService.SubmitFeedbackAsync(userId, messageId, request, cancellationToken);
            return Ok(feedback);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Message not found." });
        }
    }

    /// <summary>
    /// Retrieves the calling user's feedback for a specific assistant message, if previously submitted.
    /// </summary>
    [HttpGet("api/messages/{messageId:guid}/feedback")]
    public async Task<IActionResult> GetFeedback(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        var feedback = await _feedbackService.GetUserFeedbackForMessageAsync(userId, messageId, cancellationToken);
        if (feedback == null)
        {
            return NotFound(new { message = "No feedback submitted by this user for this message." });
        }

        return Ok(feedback);
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}
