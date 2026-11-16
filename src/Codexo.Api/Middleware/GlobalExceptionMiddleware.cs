namespace Codexo.Api.Middleware;

using System.Net;
using System.Text.Json;
using Codexo.Core.AI;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate _next, ILogger<GlobalExceptionMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var title = "An unexpected error occurred.";
        var detail = "An internal server error occurred. Please try again later.";
        var type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        switch (exception)
        {
            case KeyNotFoundException knf:
                statusCode = HttpStatusCode.NotFound;
                title = "Resource Not Found";
                detail = knf.Message;
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                break;

            case ValidationException ve:
                statusCode = HttpStatusCode.BadRequest;
                title = "Validation Failed";
                detail = string.Join("; ", ve.Errors.Select(e => e.ErrorMessage));
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                break;

            case InvalidOperationException ioe:
                statusCode = HttpStatusCode.BadRequest;
                title = "Invalid Operation";
                detail = ioe.Message;
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                break;

            case ArgumentException ae:
                statusCode = HttpStatusCode.BadRequest;
                title = "Invalid Argument";
                detail = ae.Message;
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                break;

            case AiQuotaExceededException aqe:
                statusCode = HttpStatusCode.TooManyRequests;
                title = "AI Quota Exceeded";
                detail = aqe.Message;
                type = "https://tools.ietf.org/html/rfc6585#section-4";
                if (aqe.RetryAfter.HasValue)
                {
                    context.Response.Headers.RetryAfter = ((int)aqe.RetryAfter.Value.TotalSeconds).ToString();
                }
                break;

            case AiAuthenticationException:
                statusCode = HttpStatusCode.ServiceUnavailable;
                title = "AI Service Unavailable";
                detail = "The AI service is temporarily unavailable due to a configuration or provider issue.";
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.4";
                _logger.LogError(exception, "AI Authentication error. Never log secret API key values.");
                break;

            case AiServiceUnavailableException asue:
                statusCode = HttpStatusCode.ServiceUnavailable;
                title = "AI Service Unavailable";
                detail = asue.Message;
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.4";
                break;

            default:
                _logger.LogError(exception, "Unhandled exception processing HTTP request: {Path}", context.Request.Path);
                break;
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
