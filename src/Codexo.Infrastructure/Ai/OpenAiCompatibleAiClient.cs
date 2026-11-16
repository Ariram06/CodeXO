namespace Codexo.Infrastructure.AI;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Codexo.Core.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Production AI client communicating with OpenAI-compatible endpoints (such as Google Gemini via its OpenAI compatibility API).
/// Enforces timeout handling, token usage logging, and structured error mapping.
/// </summary>
public class OpenAiCompatibleAiClient : IAiClient
{
    private readonly HttpClient _httpClient;
    private readonly AISettings _settings;
    private readonly ILogger<OpenAiCompatibleAiClient> _logger;

    public OpenAiCompatibleAiClient(
        HttpClient httpClient,
        IOptions<AISettings> options,
        ILogger<OpenAiCompatibleAiClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateChatResponseAsync(
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new AiAuthenticationException(
                "AI API key is missing. Configure it securely via .NET User Secrets: dotnet user-secrets set \"AI:ApiKey\" \"<key>\" " +
                "or set the AI__ApiKey environment variable.");
        }

        var messageList = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var (role, content) in history)
        {
            messageList.Add(new { role, content });
        }

        messageList.Add(new { role = "user", content = userPrompt });

        var payload = new Dictionary<string, object>
        {
            ["model"] = _settings.Model,
            ["messages"] = messageList,
            ["temperature"] = _settings.Temperature,
            ["max_tokens"] = _settings.MaxTokens
        };

        if (!string.IsNullOrWhiteSpace(_settings.ReasoningEffort))
        {
            payload["reasoning_effort"] = _settings.ReasoningEffort;
        }

        var endpoint = _settings.BaseUrl.TrimEnd('/') + "/chat/completions";
        HttpResponseMessage? response = null;
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                response = await _httpClient.SendAsync(httpRequest, linkedCts.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == maxAttempts)
                {
                    throw new AiServiceUnavailableException(
                        $"AI request timed out after {_settings.TimeoutSeconds} seconds.", ex);
                }
            }
            catch (HttpRequestException ex)
            {
                if (attempt == maxAttempts)
                {
                    throw new AiServiceUnavailableException(
                        "Failed to establish network connection to AI endpoint.", ex);
                }
            }

            if (response != null)
            {
                if ((int)response.StatusCode == 503 && attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
                    continue;
                }

                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1 * attempt), cancellationToken);
        }

        if (response == null)
        {
            throw new AiServiceUnavailableException("AI provider did not return a response.");
        }

        // Handle provider-specific HTTP status codes
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new AiAuthenticationException(
                    "AI provider rejected authentication. Please verify that your Gemini API key is valid and active.");
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                TimeSpan? retryAfter = null;
                if (response.Headers.RetryAfter != null)
                {
                    if (response.Headers.RetryAfter.Delta.HasValue)
                    {
                        retryAfter = response.Headers.RetryAfter.Delta.Value;
                    }
                    else if (response.Headers.RetryAfter.Date.HasValue)
                    {
                        var diff = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                        retryAfter = diff > TimeSpan.Zero ? diff : TimeSpan.FromSeconds(1);
                    }
                }

                throw new AiQuotaExceededException(
                    "AI rate limit or quota exceeded. Please try again shortly.", retryAfter);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new AiServiceUnavailableException(
                    $"AI model '{_settings.Model}' is not found or unavailable on the configured endpoint.");
            }

            if ((int)response.StatusCode >= 500)
            {
                throw new AiServiceUnavailableException(
                    $"AI provider server error (HTTP {(int)response.StatusCode}: {response.ReasonPhrase}).");
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AiClientException(
                $"AI provider rejected request with HTTP {(int)response.StatusCode}: {response.ReasonPhrase}. Details: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        // Parse OpenAI-compatible envelope
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Audit log token counts only (never log prompts, headers, or keys per CODEXO BUILD RULES)
        if (root.TryGetProperty("usage", out var usage))
        {
            var promptTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
            var completionTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;
            var totalTokens = usage.TryGetProperty("total_tokens", out var tt) ? tt.GetInt32() : 0;

            _logger.LogInformation(
                "AI chat generation completed. Model: {Model}, PromptTokens: {PromptTokens}, CompletionTokens: {CompletionTokens}, TotalTokens: {TotalTokens}",
                _settings.Model, promptTokens, completionTokens, totalTokens);
        }

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new AiClientException("AI provider response contained no choices.");
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var contentElement))
        {
            throw new AiClientException("AI provider response choice contained no message content.");
        }

        var rawContent = contentElement.GetString();
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            throw new AiClientException("AI provider returned empty message content.");
        }

        return rawContent;
    }
}
