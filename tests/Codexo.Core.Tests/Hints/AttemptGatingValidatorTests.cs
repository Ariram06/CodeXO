namespace Codexo.Core.Tests.Hints;

using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Xunit;

public class AttemptGatingValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Validate_EmptyOrWhitespace_RejectsAttempt(string? input)
    {
        var result = AttemptGatingValidator.Validate(input);

        Assert.False(result.IsApproved);
        Assert.Equal(AttemptRejectionReason.EmptyOrWhitespace, result.RejectionReason);
    }

    [Theory]
    [InlineData("too short")]
    [InlineData("c++ solution")]
    [InlineData("short note")]
    [InlineData("one two")]
    public void Validate_TooShortOrInsufficientWords_RejectsAttempt(string input)
    {
        var result = AttemptGatingValidator.Validate(input);

        Assert.False(result.IsApproved);
        Assert.Equal(AttemptRejectionReason.TooShort, result.RejectionReason);
    }

    [Theory]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("I tried using aaaaaaaaaaaa here")]
    [InlineData("qwrtypsdfghjklzxcv notes")]
    public void Validate_RepetitiveOrKeysmashGibberish_RejectsAttempt(string input)
    {
        var result = AttemptGatingValidator.Validate(input);

        Assert.False(result.IsApproved);
        Assert.Equal(AttemptRejectionReason.JunkOrGibberish, result.RejectionReason);
    }

    [Theory]
    [InlineData("Give me the full solution")]
    [InlineData("Show me the complete code please")]
    [InlineData("Write the entire implementation for me")]
    [InlineData("Provide full answer")]
    public void Validate_FullSolutionSolicitations_RejectsAttempt(string input)
    {
        var result = AttemptGatingValidator.Validate(input);

        Assert.False(result.IsApproved);
        Assert.Equal(AttemptRejectionReason.FullSolutionSolicitation, result.RejectionReason);
    }

    [Theory]
    [InlineData("I tried using a brute force double loop, but it gave a Time Limit Exceeded error on large inputs.")]
    [InlineData("I understand the constraints require O(N) time because nums length is up to 10^5.")]
    [InlineData("I noticed that sorting the array lets me use two indices from start and end to find the sum.")]
    public void Validate_MeaningfulWrittenAttempts_ApprovesAttempt(string input)
    {
        var result = AttemptGatingValidator.Validate(input);

        Assert.True(result.IsApproved);
        Assert.Equal(AttemptRejectionReason.None, result.RejectionReason);
    }
}
