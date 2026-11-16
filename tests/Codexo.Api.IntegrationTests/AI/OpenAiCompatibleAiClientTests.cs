namespace Codexo.Api.IntegrationTests.AI;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Codexo.Core.AI;
using Codexo.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

public class OpenAiCompatibleAiClientTests
{
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public int CallCount { get; private set; }

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return await _handler(request, cancellationToken);
        }
    }

    private class TestLogger<T> : ILogger<T>
    {
        public List<string> LoggedMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add(formatter(state, exception));
        }
    }

    private static (OpenAiCompatibleAiClient Client, TestHttpMessageHandler Handler, TestLogger<OpenAiCompatibleAiClient> Logger)
        CreateClient(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc,
            Action<AISettings>? configureSettings = null)
    {
        var settings = new AISettings
        {
            ApiKey = "test-secret-key-12345",
            Model = "gemini-3.5-flash-lite",
            MaxTokens = 2000,
            ReasoningEffort = "low",
            TimeoutSeconds = 5,
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/"
        };
        configureSettings?.Invoke(settings);

        var handler = new TestHttpMessageHandler(handlerFunc);
        var httpClient = new HttpClient(handler);
        var logger = new TestLogger<OpenAiCompatibleAiClient>();
        var options = Options.Create(settings);

        var client = new OpenAiCompatibleAiClient(httpClient, options, logger);
        return (client, handler, logger);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_SuccessfulResponse_ParsesChoicesAndLogsTokens()
    {
        const string expectedAssistantText = "Here is the working solution and complexity analysis.";
        var (client, handler, logger) = CreateClient((req, ct) =>
        {
            var responseJson = JsonSerializer.Serialize(new
            {
                id = "chatcmpl-test",
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new { role = "assistant", content = expectedAssistantText },
                        finish_reason = "stop"
                    }
                },
                usage = new
                {
                    prompt_tokens = 45,
                    completion_tokens = 120,
                    total_tokens = 165
                }
            });

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var history = new List<(string Role, string Content)>
        {
            ("user", "Previous question"),
            ("assistant", "Previous answer")
        };

        var result = await client.GenerateChatResponseAsync(
            "System prompt instructions",
            history,
            "User synthetic query");

        Assert.Equal(expectedAssistantText, result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test-secret-key-12345", handler.LastRequest.Headers.Authorization?.Parameter);

        // Verify request payload envelope
        Assert.NotNull(handler.LastRequestBody);
        using var requestDoc = JsonDocument.Parse(handler.LastRequestBody);
        var reqRoot = requestDoc.RootElement;
        Assert.Equal("gemini-3.5-flash-lite", reqRoot.GetProperty("model").GetString());
        Assert.Equal(2000, reqRoot.GetProperty("max_tokens").GetInt32());
        Assert.Equal("low", reqRoot.GetProperty("reasoning_effort").GetString());

        var messages = reqRoot.GetProperty("messages");
        Assert.Equal(4, messages.GetArrayLength()); // system + 2 history + user

        // Verify token usage was logged
        var logMessage = Assert.Single(logger.LoggedMessages);
        Assert.Contains("PromptTokens: 45", logMessage);
        Assert.Contains("CompletionTokens: 120", logMessage);
        Assert.Contains("TotalTokens: 165", logMessage);

        // Verify prompt content and API key are NEVER in logs
        Assert.DoesNotContain("test-secret-key-12345", logMessage);
        Assert.DoesNotContain("User synthetic query", logMessage);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_ApiKeyMissing_ThrowsAiAuthenticationException_WithoutSendingRequest()
    {
        var (client, handler, _) = CreateClient(
            (req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
            s => s.ApiKey = "");

        var ex = await Assert.ThrowsAsync<AiAuthenticationException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("AI API key is missing", ex.Message);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_Unauthorized401_ThrowsAiAuthenticationException()
    {
        var (client, _, _) = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var ex = await Assert.ThrowsAsync<AiAuthenticationException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("rejected authentication", ex.Message);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_Forbidden403_ThrowsAiAuthenticationException()
    {
        var (client, _, _) = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));

        var ex = await Assert.ThrowsAsync<AiAuthenticationException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("rejected authentication", ex.Message);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_RateLimited429_WithRetryAfterSeconds_ThrowsAiQuotaExceededException()
    {
        var (client, _, _) = CreateClient((req, ct) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            resp.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(45));
            return Task.FromResult(resp);
        });

        var ex = await Assert.ThrowsAsync<AiQuotaExceededException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("rate limit or quota exceeded", ex.Message);
        Assert.NotNull(ex.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(45), ex.RetryAfter.Value);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_RateLimited429_WithRetryAfterDate_ThrowsAiQuotaExceededException()
    {
        var (client, _, _) = CreateClient((req, ct) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            resp.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddSeconds(30));
            return Task.FromResult(resp);
        });

        var ex = await Assert.ThrowsAsync<AiQuotaExceededException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.NotNull(ex.RetryAfter);
        Assert.True(ex.RetryAfter.Value > TimeSpan.Zero);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_RateLimited429_WithoutRetryAfter_ThrowsAiQuotaExceededException()
    {
        var (client, _, _) = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));

        var ex = await Assert.ThrowsAsync<AiQuotaExceededException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Null(ex.RetryAfter);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_ModelNotFound404_ThrowsAiServiceUnavailableException()
    {
        var (client, _, _) = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var ex = await Assert.ThrowsAsync<AiServiceUnavailableException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("not found or unavailable", ex.Message);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_ServiceUnavailable503_RetriesAndEventuallyThrowsAiServiceUnavailableException()
    {
        var (client, handler, _) = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var ex = await Assert.ThrowsAsync<AiServiceUnavailableException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("server error", ex.Message);
        Assert.Equal(3, handler.CallCount); // Verified 3 attempts
    }

    [Fact]
    public async Task GenerateChatResponseAsync_NetworkFailure_RetriesAndThrowsAiServiceUnavailableException()
    {
        var (client, handler, _) = CreateClient((req, ct) =>
            throw new HttpRequestException("Socket connection refused"));

        var ex = await Assert.ThrowsAsync<AiServiceUnavailableException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("Failed to establish network connection", ex.Message);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_RequestTimeout_ThrowsAiServiceUnavailableException()
    {
        var (client, _, _) = CreateClient(
            async (req, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            s => s.TimeoutSeconds = 1);

        var ex = await Assert.ThrowsAsync<AiServiceUnavailableException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("timed out", ex.Message);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_EmptyChoices_ThrowsAiClientException()
    {
        var (client, _, _) = CreateClient((req, ct) =>
        {
            var json = JsonSerializer.Serialize(new { choices = Array.Empty<object>() });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var ex = await Assert.ThrowsAsync<AiClientException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("contained no choices", ex.Message);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_EmptyContent_ThrowsAiClientException()
    {
        var (client, _, _) = CreateClient((req, ct) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new { message = new { role = "assistant", content = "   " } }
                }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var ex = await Assert.ThrowsAsync<AiClientException>(() =>
            client.GenerateChatResponseAsync("sys", [], "user query"));

        Assert.Contains("empty message content", ex.Message);
    }

    [Fact]
    public async Task GenerateChatResponseAsync_ZeroSecretsAndPromptsInLogs_Audit()
    {
        const string secretKey = "super-secret-gemini-key-xyz";
        const string sensitivePrompt = "Classified intellectual property source code";

        var (client, _, logger) = CreateClient(
            (req, ct) =>
            {
                var responseJson = JsonSerializer.Serialize(new
                {
                    choices = new[] { new { message = new { role = "assistant", content = "public class Solution {}" } } },
                    usage = new { prompt_tokens = 20, completion_tokens = 30, total_tokens = 50 }
                });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
                });
            },
            s => s.ApiKey = secretKey);

        await client.GenerateChatResponseAsync("sys", [], sensitivePrompt);

        foreach (var msg in logger.LoggedMessages)
        {
            Assert.DoesNotContain(secretKey, msg);
            Assert.DoesNotContain(sensitivePrompt, msg);
            Assert.DoesNotContain("Authorization", msg);
            Assert.DoesNotContain("Bearer", msg);
        }
    }
}
