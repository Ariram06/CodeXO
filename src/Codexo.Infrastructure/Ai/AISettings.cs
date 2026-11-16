namespace Codexo.Infrastructure.AI;

/// <summary>
/// Configuration options for AI hint generation providers.
/// </summary>
public class AISettings
{
    public const string SectionName = "AI";

    /// <summary>
    /// AI provider type: "Fake" (default, offline for CI/tests) or "OpenAiCompatible" (Google Gemini API).
    /// </summary>
    public string Provider { get; set; } = "Fake";

    /// <summary>
    /// Base URL for the OpenAI-compatible endpoint.
    /// Default targets Google Gemini OpenAI-compatible gateway.
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/openai/";

    /// <summary>
    /// Target model identifier. Default is fast, low-cost Flash tier.
    /// </summary>
    public string Model { get; set; } = "gemini-2.5-flash";

    /// <summary>
    /// Secret API key. Stored strictly in .NET User Secrets or environment variables (AI__ApiKey).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Hard token ceiling per hint generation to protect quotas.
    /// </summary>
    public int MaxTokens { get; set; } = 400;

    /// <summary>
    /// Generation temperature (low temperature ensures deterministic, factual output).
    /// </summary>
    public double Temperature { get; set; } = 0.2;
}
