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
    /// Submits helpfulness feedback (thumbs up/down and optional comment) for a specific hint.
    /// Idempotent / upsert: If feedback already exists for this user and hint, updates it.
    /// </summary>
    [HttpPost("api/hints/{hintId:guid}/feedback")]
    public async Task<IActionResult> SubmitFeedback(
        Guid hintId,
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
            var feedback = await _feedbackService.SubmitFeedbackAsync(userId, hintId, request, cancellationToken);
            return Ok(feedback);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Hint not found." });
        }
    }

    /// <summary>
    /// Retrieves the calling user's feedback for a specific hint, if previously submitted.
    /// </summary>
    [HttpGet("api/hints/{hintId:guid}/feedback")]
    public async Task<IActionResult> GetFeedback(
        Guid hintId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        var feedback = await _feedbackService.GetUserFeedbackForHintAsync(userId, hintId, cancellationToken);
        if (feedback == null)
        {
            return NotFound(new { message = "No feedback submitted by this user for this hint." });
        }

        return Ok(feedback);
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}
