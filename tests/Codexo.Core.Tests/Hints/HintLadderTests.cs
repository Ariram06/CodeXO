namespace Codexo.Core.Tests.Hints;

using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Xunit;

public class HintLadderTests
{
    [Theory]
    [InlineData(HintLevel.None, HintLevel.L1_Understand)]
    [InlineData(HintLevel.L1_Understand, HintLevel.L2_Direction)]
    [InlineData(HintLevel.L2_Direction, HintLevel.L3_Concepts)]
    [InlineData(HintLevel.L3_Concepts, HintLevel.L4_Steps)]
    [InlineData(HintLevel.L4_Steps, HintLevel.L5_Deep)]
    public void GetNextLevel_ValidProgression_ReturnsNextSequentialLevel(HintLevel current, HintLevel expected)
    {
        var result = HintLadder.GetNextLevel(current);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetNextLevel_AtMaxBoundary_ReturnsMaxLevelWithoutOverflow()
    {
        var result = HintLadder.GetNextLevel(HintLevel.L5_Deep);
        Assert.Equal(HintLevel.L5_Deep, result);
    }

    [Theory]
    [InlineData(HintLevel.None, false)]
    [InlineData(HintLevel.L1_Understand, false)]
    [InlineData(HintLevel.L2_Direction, true)]
    [InlineData(HintLevel.L3_Concepts, true)]
    [InlineData(HintLevel.L4_Steps, true)]
    [InlineData(HintLevel.L5_Deep, true)]
    public void RequiresAttempt_IdentifiesLevelsRequiringWrittenAttempt(HintLevel level, bool expected)
    {
        var requires = HintLadder.RequiresAttempt(level);
        Assert.Equal(expected, requires);
    }

    [Theory]
    [InlineData(HintLevel.None, HintLevel.L1_Understand, true)]
    [InlineData(HintLevel.L1_Understand, HintLevel.L2_Direction, true)]
    [InlineData(HintLevel.L1_Understand, HintLevel.L1_Understand, true)] // Idempotent re-request
    [InlineData(HintLevel.L1_Understand, HintLevel.L3_Concepts, false)]   // Skipping level
    [InlineData(HintLevel.L2_Direction, HintLevel.L5_Deep, false)]      // Jump
    [InlineData(HintLevel.L5_Deep, (HintLevel)6, false)]                // Beyond boundary
    public void IsValidProgression_EnforcesSequentialProgression(HintLevel current, HintLevel requested, bool expected)
    {
        var isValid = HintLadder.IsValidProgression(current, requested);
        Assert.Equal(expected, isValid);
    }

    [Fact]
    public void DetermineAuthorizedLevel_WithoutApprovedAttempt_DoesNotAdvancePastL1()
    {
        // When at L1 and user has not passed attempt gating for L2, remains at L1
        var level = HintLadder.DetermineAuthorizedLevel(HintLevel.L1_Understand, hasApprovedAttemptForNext: false);
        Assert.Equal(HintLevel.L1_Understand, level);
    }

    [Fact]
    public void DetermineAuthorizedLevel_WithApprovedAttempt_AdvancesToNextLevel()
    {
        // When at L1 and user has an approved attempt, unlocks L2
        var level = HintLadder.DetermineAuthorizedLevel(HintLevel.L1_Understand, hasApprovedAttemptForNext: true);
        Assert.Equal(HintLevel.L2_Direction, level);
    }

    [Fact]
    public void DetermineAuthorizedLevel_FromNone_AlwaysAuthorizesL1WithoutAttempt()
    {
        var level = HintLadder.DetermineAuthorizedLevel(HintLevel.None, hasApprovedAttemptForNext: false);
        Assert.Equal(HintLevel.L1_Understand, level);
    }

    [Fact]
    public void DetermineAuthorizedLevel_AtL5Boundary_NeverExceedsL5()
    {
        var level = HintLadder.DetermineAuthorizedLevel(HintLevel.L5_Deep, hasApprovedAttemptForNext: true);
        Assert.Equal(HintLevel.L5_Deep, level);
    }
}
