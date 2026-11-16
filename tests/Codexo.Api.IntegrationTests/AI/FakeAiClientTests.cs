namespace Codexo.Api.IntegrationTests.AI;

using Codexo.Core.AI;
using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Codexo.Infrastructure.AI;
using Xunit;

public class FakeAiClientTests
{
    private readonly FakeAiClient _client = new();

    [Theory]
    [InlineData(HintLevel.L1_Understand)]
    [InlineData(HintLevel.L2_Direction)]
    [InlineData(HintLevel.L3_Concepts)]
    [InlineData(HintLevel.L4_Steps)]
    [InlineData(HintLevel.L5_Deep)]
    public async Task GenerateHintAsync_ReturnsValidStructuredHint_PassingSolutionGuard(HintLevel level)
    {
        var request = new AiPromptRequest(
            Level: level,
            ProblemTitle: "Two Sum",
            ProblemDescription: "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.",
            UserAttempt: "I tried iterating with two loops.",
            PreviousHints: new[] { "Understand: Clarify constraints." });

        var response = await _client.GenerateHintAsync(request);

        Assert.NotNull(response);
        Assert.Equal(level, response.Level);
        Assert.False(string.IsNullOrWhiteSpace(response.Hint));
        Assert.NotNull(response.GuidingQuestions);
        Assert.NotNull(response.Concepts);
        Assert.NotNull(response.Steps);
        Assert.NotNull(response.Pitfalls);

        // Verify that FakeAiClient output adheres strictly to SolutionGuard
        var guardResult = SolutionGuard.Inspect(level, response.Hint);
        Assert.True(guardResult.IsValid, $"Hint failed guard at level {level}: {string.Join(", ", guardResult.Details)}");
    }
}
