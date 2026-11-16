namespace Codexo.Api.Controllers;

using System.Security.Claims;
using Codexo.Core.DTOs.Problems;
using Codexo.Core.Enums;
using Codexo.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IValidator<ProblemIntakeRequest> _intakeValidator;

    public SessionsController(
        ISessionService sessionService,
        IValidator<ProblemIntakeRequest> intakeValidator)
    {
        _sessionService = sessionService;
        _intakeValidator = intakeValidator;
    }

    /// <summary>
    /// Ingests problem context from the extension and retrieves or initializes the user's active session.
    /// </summary>
    [HttpPost("context")]
    public async Task<IActionResult> IntakeProblemContext(
        [FromBody] ProblemIntakeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _intakeValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        var session = await _sessionService.GetOrCreateSessionAsync(userId, request, cancellationToken);
        return Ok(session);
    }

    /// <summary>
    /// Retrieves a study session by ID, strictly enforcing that the session belongs to the requesting user.
    /// Returns 404 if the session does not exist or belongs to another user.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSessionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        var session = await _sessionService.GetSessionByIdAsync(userId, id, cancellationToken);
        if (session == null)
        {
            return NotFound(new { message = "Session not found." });
        }

        return Ok(session);
    }

    /// <summary>
    /// Retrieves the current active session for a specific problem and platform.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSession(
        [FromQuery] Platform platform,
        [FromQuery] string platformProblemId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(platformProblemId))
        {
            return BadRequest(new { errors = new[] { "PlatformProblemId is required." } });
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        var session = await _sessionService.GetActiveSessionForProblemAsync(userId, platform, platformProblemId, cancellationToken);
        if (session == null)
        {
            return NotFound(new { message = "No active session found for this problem." });
        }

        return Ok(session);
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}
