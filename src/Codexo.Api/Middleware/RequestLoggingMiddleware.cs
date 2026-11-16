namespace Codexo.Api.Middleware;

using System.Diagnostics;
using System.Security.Claims;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip noisy health checks from verbose logging if desired, or log concisely
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value
                ?? "Anonymous";

            // Audit log on rate limiting (429)
            if (statusCode == StatusCodes.Status429TooManyRequests)
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _logger.LogWarning(
                    "[RateLimit Audit] User {UserId} (IP: {ClientIp}) exceeded rate limit on {Method} {Path}. Duration: {ElapsedMs}ms",
                    userId, clientIp, method, path, elapsedMs);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms (User: {UserId})",
                    method, path, statusCode, elapsedMs, userId);
            }
        }
    }
}
