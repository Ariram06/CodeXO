namespace Codexo.Core.AI;

/// <summary>
/// Base exception for AI client generation failures.
/// </summary>
public class AiClientException : Exception
{
    public AiClientException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when the AI provider rate limit is exceeded (HTTP 429).
/// </summary>
public class AiQuotaExceededException : AiClientException
{
    public TimeSpan? RetryAfter { get; }

    public AiQuotaExceededException(string message, TimeSpan? retryAfter = null, Exception? innerException = null)
        : base(message, innerException)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// Thrown when AI credentials or access permissions are rejected (HTTP 401/403).
/// </summary>
public class AiAuthenticationException : AiClientException
{
    public AiAuthenticationException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when the AI service is experiencing outages, 5xx server errors, or network timeouts.
/// </summary>
public class AiServiceUnavailableException : AiClientException
{
    public AiServiceUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
