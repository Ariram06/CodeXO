namespace Codexo.Core.Tests.Hints;

using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Xunit;

public class AiResponseValidatorTests
{
    [Fact]
    public void ValidateRawResponse_ValidPayload_ReturnsPassAndParsedDto()
    {
        var rawJson = """
        {
            "hintLevel": 1,
            "content": "Notice that the elements can be negative and target can be 0. What does this tell you about sorting?",
            "explanation": "Clarifying edge conditions in input values without naming algorithms."
        }
        """;

        var (result, dto) = AiResponseValidator.ValidateRawResponse(rawJson, HintLevel.L1_Understand);

        Assert.True(result.IsValid);
        Assert.NotNull(dto);
        Assert.Equal(HintLevel.L1_Understand, dto.HintLevel);
        Assert.Contains("Notice that the elements", dto.Content);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("This is not JSON at all")]
    [InlineData("{ broken json")]
    public void ValidateRawResponse_MalformedOrNonJson_ReturnsMalformedViolation(string? input)
    {
        var (result, dto) = AiResponseValidator.ValidateRawResponse(input, HintLevel.L1_Understand);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.MalformedJson, result.Violations);
        Assert.Null(dto);
    }

    [Fact]
    public void ValidateRawResponse_EmptyContentField_ReturnsEmptyContentViolation()
    {
        var rawJson = """
        {
            "hintLevel": 1,
            "content": "   ",
            "explanation": "test"
        }
        """;

        var (result, dto) = AiResponseValidator.ValidateRawResponse(rawJson, HintLevel.L1_Understand);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.EmptyContent, result.Violations);
    }

    [Fact]
    public void ValidateRawResponse_MismatchedHintLevel_ReturnsMalformedViolation()
    {
        var rawJson = """
        {
            "hintLevel": 2,
            "content": "Think about what changes from left to right.",
            "explanation": "Guiding clue"
        }
        """;

        // Expected L1, but payload contains L2
        var (result, _) = AiResponseValidator.ValidateRawResponse(rawJson, HintLevel.L1_Understand);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.MalformedJson, result.Violations);
    }

    [Fact]
    public void ValidateRawResponse_ContainsCodeBlock_ReturnsContainsCodeBlockViolation()
    {
        var rawJson = """
        {
            "hintLevel": 3,
            "content": "Here is a quick snippet: ```python\nprint('hello')\n```",
            "explanation": "Code leak"
        }
        """;

        var (result, _) = AiResponseValidator.ValidateRawResponse(rawJson, HintLevel.L3_Concepts);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.ContainsCodeBlock, result.Violations);
    }

    [Fact]
    public void ValidateRawResponse_L1Spoiler_ReturnsSpoilerViolation()
    {
        var rawJson = """
        {
            "hintLevel": 1,
            "content": "You should use binary search to locate the complement.",
            "explanation": "Spoils approach prematurely"
        }
        """;

        var (result, _) = AiResponseValidator.ValidateRawResponse(rawJson, HintLevel.L1_Understand);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.SpoilerAtEarlyLevel, result.Violations);
    }
}

