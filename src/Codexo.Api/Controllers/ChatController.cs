namespace Codexo.Api.Controllers;

using System.Security.Claims;
using Codexo.Core.AI;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[Authorize]
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IValidator<SendMessageRequest> _validator;

    public ChatController(
        IChatService chatService,
        IValidator<SendMessageRequest> validator)
    {
        _chatService = chatService;
        _validator = validator;
    }

    /// <summary>
    /// Processes a natural language coding query, incorporating active tab editor/problem context.
    /// Non-streaming: returns full assistant answer upon completion.
    /// </summary>
    [HttpPost("messages")]
    [EnableRateLimiting("ChatPolicy")]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request,
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
            var response = await _chatService.SendMessageAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
        catch (AiAuthenticationException ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}
