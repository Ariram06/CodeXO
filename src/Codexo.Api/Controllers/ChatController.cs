namespace Codexo.Api.Controllers;

using System.Security.Claims;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

    /// <summary>
    /// Processes a natural language coding query and streams incremental response chunks
    /// using Server-Sent Events (SSE). Supports upstream cancellation via CancellationToken.
    /// </summary>
    [HttpPost("stream")]
    [EnableRateLimiting("ChatPolicy")]
    public async Task StreamMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) }, cancellationToken);
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { message = "User identity could not be verified." }, cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var chunk in _chatService.StreamMessageAsync(userId, request, cancellationToken))
            {
                var json = JsonSerializer.Serialize(chunk, SseJsonOptions);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException)
        {
            // Client cancelled or disconnected; cleanly exit
        }
        catch (AiQuotaExceededException ex)
        {
            var errorChunk = new ChatStreamChunk(
                request.ConversationId ?? Guid.Empty,
                Guid.Empty,
                null,
                true,
                Error: ex.Message);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(errorChunk, SseJsonOptions)}\n\n", CancellationToken.None);
            await Response.Body.FlushAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            var errorChunk = new ChatStreamChunk(
                request.ConversationId ?? Guid.Empty,
                Guid.Empty,
                null,
                true,
                Error: ex.Message);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(errorChunk, SseJsonOptions)}\n\n", CancellationToken.None);
            await Response.Body.FlushAsync(CancellationToken.None);
        }
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}
