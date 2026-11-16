namespace Codexo.Api.Controllers;

using System.Security.Claims;
using Codexo.Core.AI;
using Codexo.Core.Dtos.Hints;
using Codexo.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[Authorize]
[ApiController]
public class HintsController : ControllerBase
{
    private readonly IHintService _hintService;
    private readonly IValidator<SubmitAttemptRequestDto> _attemptValidator;

    public HintsController(
        IHintService hintService,
        IValidator<SubmitAttemptRequestDto> attemptValidator)
    {
        _hintService = hintService;
        _attemptValidator = attemptValidator;
    }

    /// <summary>
    /// Requests the next sequential hint for the active DSA problem session.
    /// Server is authoritative for hint level; gating requires an accepted attempt for L2+.
    /// Rate limited per user to prevent Gemini quota exhaustion.
    /// </summary>
    [HttpPost("api/sessions/{sessionId:guid}/hints")]
    [EnableRateLimiting("HintPolicy")]
    public async Task<IActionResult> RequestNextHint(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        try
        {
            var hint = await _hintService.RequestNextHintAsync(userId, sessionId, cancellationToken);
            return Ok(hint);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Session not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AiQuotaExceededException ex)
        {
            if (ex.RetryAfter.HasValue)
            {
                Response.Headers.RetryAfter = ((int)ex.RetryAfter.Value.TotalSeconds).ToString();
            }
            return StatusCode(429, new { message = ex.Message, retryAfterSeconds = ex.RetryAfter?.TotalSeconds });
        }
        catch (AiServiceUnavailableException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Submits a written problem-solving attempt to unlock the next progressive hint level.
    /// Evaluates attempt quality using AttemptGatingValidator.
    /// </summary>
    [HttpPost("api/sessions/{sessionId:guid}/attempts")]
    public async Task<IActionResult> SubmitAttempt(
        Guid sessionId,
        [FromBody] SubmitAttemptRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _attemptValidator.ValidateAsync(request, cancellationToken);
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
            var evaluation = await _hintService.SubmitAttemptAsync(userId, sessionId, request, cancellationToken);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Session not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all unlocked hints for a given problem session.
    /// Strictly verifies that the session belongs to the authenticated user.
    /// </summary>
    [HttpGet("api/sessions/{sessionId:guid}/hints")]
    public async Task<IActionResult> GetSessionHints(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        try
        {
            var hints = await _hintService.GetHintsForSessionAsync(userId, sessionId, cancellationToken);
            return Ok(hints);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Session not found." });
        }
    }

    /// <summary>
    /// Gets the current hint progression and attempt status for a problem session.
    /// </summary>
    [HttpGet("api/sessions/{sessionId:guid}/hints/status")]
    public async Task<IActionResult> GetSessionHintStatus(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        try
        {
            var status = await _hintService.GetSessionHintStatusAsync(userId, sessionId, cancellationToken);
            return Ok(status);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Session not found." });
        }
    }

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
    }
}
