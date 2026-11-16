namespace Codexo.Infrastructure.AI;

using System.Text.Json;
using Codexo.Core.AI;
using Codexo.Core.Enums;

/// <summary>
/// Tolerant parser for extracting structured JSON hint responses from raw LLM output.
/// Handles markdown code fences, preamble/postamble text, and ensures non-null arrays.
/// </summary>
public static class TolerantJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AiHintResponse Parse(string rawOutput, HintLevel expectedLevel)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            throw new AiClientException("AI response was empty or whitespace.");
        }

        var cleaned = rawOutput.Trim();

        // 1. Strip markdown fences if present
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..].Trim();
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned[3..].Trim();
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned[..^3].Trim();
        }

        // 2. Extract first JSON object between { and }
        var firstBrace = cleaned.IndexOf('{');
        var lastBrace = cleaned.LastIndexOf('}');

        if (firstBrace == -1 || lastBrace == -1 || lastBrace <= firstBrace)
        {
            throw new AiClientException("No JSON object detected in AI response.");
        }

        var jsonSubstring = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);

        try
        {
            var rawModel = JsonSerializer.Deserialize<RawHintJsonModel>(jsonSubstring, JsonOptions);
            if (rawModel == null || string.IsNullOrWhiteSpace(rawModel.Hint))
            {
                throw new AiClientException("AI response JSON did not contain a valid 'hint' field.");
            }

            var levelInt = rawModel.Level ?? (int)expectedLevel;
            var level = Enum.IsDefined(typeof(HintLevel), levelInt)
                ? (HintLevel)levelInt
                : expectedLevel;

            return new AiHintResponse(
                Level: level,
                Hint: rawModel.Hint.Trim(),
                GuidingQuestions: rawModel.GuidingQuestions ?? Array.Empty<string>(),
                Concepts: rawModel.Concepts ?? Array.Empty<string>(),
                Steps: rawModel.Steps ?? Array.Empty<string>(),
                Pitfalls: rawModel.Pitfalls ?? Array.Empty<string>());
        }
        catch (JsonException ex)
        {
            throw new AiClientException($"Failed to deserialize AI JSON response: {ex.Message}", ex);
        }
    }

    private class RawHintJsonModel
    {
        public int? Level { get; set; }
        public string? Hint { get; set; }
        public string[]? GuidingQuestions { get; set; }
        public string[]? Concepts { get; set; }
        public string[]? Steps { get; set; }
        public string[]? Pitfalls { get; set; }
    }
}
