namespace Codexo.Core.Tests.Prompts;

using Codexo.Core.AI;
using Codexo.Core.Enums;
using Codexo.Core.Prompts;
using Xunit;

public class HintPromptBuilderTests
{
    private readonly HintPromptBuilder _builder = new();

    [Theory]
    [InlineData(HintLevel.L1_Understand, "L1 — UNDERSTAND")]
    [InlineData(HintLevel.L2_Direction, "L2 — DIRECTION")]
    [InlineData(HintLevel.L3_Concepts, "L3 — CONCEPTS")]
    [InlineData(HintLevel.L4_Steps, "L4 — STEPS")]
    [InlineData(HintLevel.L5_Deep, "L5 — DEEP")]
    public void BuildSystemPrompt_ContainsBaseRulesAndCorrectLevelTemplate(HintLevel level, string expectedLevelMarker)
    {
        var prompt = _builder.BuildSystemPrompt(level);

        Assert.Contains("You are Codexo", prompt);
        Assert.Contains("OUTPUT FORMAT:", prompt);
        Assert.Contains(expectedLevelMarker, prompt);
    }

    [Fact]
    public void BuildSystemPrompt_InvalidLevel_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _builder.BuildSystemPrompt(HintLevel.None));
    }

    [Fact]
    public void BuildUserPrompt_FormatsProblemAttemptAndPreviousHintsCorrectly()
    {
        var request = new AiPromptRequest(
            Level: HintLevel.L2_Direction,
            ProblemTitle: "Two Sum",
            ProblemDescription: "Find two numbers that add up to target.",
            UserAttempt: "I tried using a nested loop in O(N^2) time.",
            PreviousHints: new[] { "Understand: Restate constraints." });

        var prompt = _builder.BuildUserPrompt(request);

        Assert.Contains("=== DATA: PROBLEM ===", prompt);
        Assert.Contains("Title: Two Sum", prompt);
        Assert.Contains("Find two numbers that add up to target.", prompt);
        Assert.Contains("=== DATA: USER_ATTEMPT ===", prompt);
        Assert.Contains("nested loop in O(N^2) time", prompt);
        Assert.Contains("=== DATA: PREVIOUS_HINTS ===", prompt);
        Assert.Contains("[Hint 1] Understand: Restate constraints.", prompt);
    }

    [Fact]
    public void BuildUserPrompt_WithNoAttemptOrPreviousHints_ProvidesFallbacks()
    {
        var request = new AiPromptRequest(
            Level: HintLevel.L1_Understand,
            ProblemTitle: "Valid Anagram",
            ProblemDescription: "Check if t is an anagram of s.",
            UserAttempt: "",
            PreviousHints: Array.Empty<string>());

        var prompt = _builder.BuildUserPrompt(request);

        Assert.Contains("None (First hint request or L1 understand level).", prompt);
        Assert.Contains("None (No prior hints provided for this problem session).", prompt);
    }
}
