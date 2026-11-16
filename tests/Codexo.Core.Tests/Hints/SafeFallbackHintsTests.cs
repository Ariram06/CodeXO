namespace Codexo.Core.Tests.Hints;

using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Xunit;

public class SafeFallbackHintsTests
{
    [Theory]
    [InlineData(HintLevel.L1_Understand)]
    [InlineData(HintLevel.L2_Direction)]
    [InlineData(HintLevel.L3_Concepts)]
    [InlineData(HintLevel.L4_Steps)]
    [InlineData(HintLevel.L5_Deep)]
    public void SafeFallbackHints_AllLevels_PassSolutionGuard(HintLevel level)
    {
        var fallback = SafeFallbackHints.GetFallback(level, "Benchmark Problem");

        Assert.NotNull(fallback);
        Assert.Equal(level, fallback.Level);
        Assert.False(string.IsNullOrWhiteSpace(fallback.Hint));

        var guardResult = SolutionGuard.Inspect(level, fallback.Hint);
        Assert.True(guardResult.IsValid, $"Fallback for {level} failed SolutionGuard: {string.Join(", ", guardResult.Details)}");
    }

    [Fact]
    public void SafeFallbackHints_L1_HasNoConceptsOrSteps()
    {
        var fallback = SafeFallbackHints.GetFallback(HintLevel.L1_Understand);

        Assert.Empty(fallback.Concepts);
        Assert.Empty(fallback.Steps);
        Assert.NotEmpty(fallback.GuidingQuestions);
    }

    [Fact]
    public void SafeFallbackHints_L4_HasSteps()
    {
        var fallback = SafeFallbackHints.GetFallback(HintLevel.L4_Steps);

        Assert.NotEmpty(fallback.Steps);
    }
}
