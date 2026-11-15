namespace Codexo.Core.Hints;

using System.Text.Json;
using Codexo.Core.Dtos;
using Codexo.Core.Enums;

/// <summary>
/// Validates AI provider responses structurally and semantically before delivering to users.
/// Verifies JSON deserialization, schema conformity, and invokes SolutionGuard.
/// </summary>
public static class AiResponseValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Validates raw JSON string returned by an AI provider against target level expectations.
    /// </summary>
    public static (GuardResult Result, AiHintResponseDto? ParsedDto) ValidateRawResponse(
        string? rawJson,
        HintLevel expectedLevel)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return (GuardResult.Fail(GuardViolationType.MalformedJson, "AI response payload was empty or whitespace."), null);
        }

        AiHintResponseDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<AiHintResponseDto>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return (GuardResult.Fail(GuardViolationType.MalformedJson, $"Malformed JSON from AI provider: {ex.Message}"), null);
        }

        if (dto is null)
        {
            return (GuardResult.Fail(GuardViolationType.MalformedJson, "Deserialized AI hint object was null."), null);
        }

        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return (GuardResult.Fail(GuardViolationType.EmptyContent, "AI hint content field was empty."), dto);
        }

        if (dto.HintLevel != expectedLevel)
        {
            return (GuardResult.Fail(
                GuardViolationType.MalformedJson,
                $"AI response hintLevel ({dto.HintLevel}) did not match expected level ({expectedLevel})."), dto);
        }

        // Delegate content safety and spoiler inspection to SolutionGuard
        var guardResult = SolutionGuard.Inspect(expectedLevel, dto.Content);
        return (guardResult, dto);
    }
}

