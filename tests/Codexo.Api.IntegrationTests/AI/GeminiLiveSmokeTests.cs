namespace Codexo.Api.IntegrationTests.AI;

using Codexo.Core.AI;
using Codexo.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

public class GeminiLiveSmokeTests
{
    private readonly ITestOutputHelper _output;

    public GeminiLiveSmokeTests(ITestOutputHelper output)
    {
        _output = output;
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

    [Fact]
    public async Task LiveGeminiApi_WhenKeyPresent_AnswersSyntheticQuery_WithTokenUsageLogged()
    {
        // Resolve configuration securely (Environment variables or User Secrets)
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var apiKey = config["AI:ApiKey"]
            ?? config["AISettings:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("AI__ApiKey");

        var isKeyConfigured = !string.IsNullOrWhiteSpace(apiKey);

        // Security check: strictly log presence ONLY, never the secret key value
        _output.WriteLine($"[Presence Check] Gemini API key configured: {(isKeyConfigured ? "yes" : "no")}");

        if (!isKeyConfigured)
        {
            _output.WriteLine("[Presence Check] Skipped live test: No API key configured in environment or user-secrets.");
            return;
        }

        var settings = new AISettings
        {
            ApiKey = apiKey,
            BaseUrl = config["AI:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/",
            Model = config["AI:Model"] ?? "gemini-3.5-flash-lite",
            MaxTokens = 50,
            ReasoningEffort = "low",
            TimeoutSeconds = 25
        };

        using var httpClient = new HttpClient();
        var logger = new TestLogger<OpenAiCompatibleAiClient>();
        var options = Options.Create(settings);
        var client = new OpenAiCompatibleAiClient(httpClient, options, logger);

        const string syntheticPrompt = "Respond with 'Codexo Live OK' and nothing else.";

        try
        {
            var response = await client.GenerateChatResponseAsync(
                "You are a test agent. Respond precisely as requested.",
                [],
                syntheticPrompt);

            _output.WriteLine($"[Live Smoke Test] Received response length: {response.Length} characters.");

            Assert.NotNull(response);
            Assert.False(string.IsNullOrWhiteSpace(response));
            Assert.Contains("Codexo Live OK", response, StringComparison.OrdinalIgnoreCase);

            // Audit test logger: verify token logging occurred without leaking secrets or prompts
            var logMessage = Assert.Single(logger.LoggedMessages);
            Assert.Contains("PromptTokens:", logMessage);
            Assert.Contains("CompletionTokens:", logMessage);
            Assert.Contains("TotalTokens:", logMessage);

            Assert.DoesNotContain(apiKey!, logMessage);
            Assert.DoesNotContain(syntheticPrompt, logMessage);
        }
        catch (AiServiceUnavailableException ex)
        {
            _output.WriteLine($"[Live Smoke Test] External Gemini service unavailable or network connection failed: {ex.Message}. Skipping without breaking build.");
        }
    }
}
