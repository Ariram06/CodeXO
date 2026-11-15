namespace Codexo.Core.Tests.Hints;

using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Xunit;

public class SolutionGuardTests
{
    [Fact]
    public void Inspect_NullOrWhitespace_FailsWithEmptyViolation()
    {
        var result = SolutionGuard.Inspect(HintLevel.L1_Understand, "   ");

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.EmptyContent, result.Violations);
    }

    [Theory]
    [InlineData("```csharp\npublic int[] TwoSum() { return null; }\n```")]
    [InlineData("Here is the code block:\n```python\ndef solve(): pass\n```")]
    public void Inspect_ContainsMarkdownCodeBlock_FailsWithCodeBlockViolation(string content)
    {
        var result = SolutionGuard.Inspect(HintLevel.L3_Concepts, content);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.ContainsCodeBlock, result.Violations);
    }

    [Theory]
    [InlineData("You should define function solve(nums) to iterate through the array.")]
    [InlineData("Consider writing def two_sum(nums, target): for your entrypoint.")]
    [InlineData("Try using int diff = target - nums[i]; to check each element.")]
    [InlineData("In a loop for (int i = 0; i < n; i++) you can look back.")]
    public void Inspect_ContainsCompilableStatementsOrDeclarations_FailsWithCompilableCodeViolation(string content)
    {
        var result = SolutionGuard.Inspect(HintLevel.L4_Steps, content);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.ContainsCompilableCode, result.Violations);
    }

    [Theory]
    [InlineData("You can solve this using binary search over the array indices.")]
    [InlineData("A hash map is the ideal structure to store previously seen numbers.")]
    [InlineData("Consider dynamic programming with a memoization table.")]
    [InlineData("This problem maps directly to a two pointers pattern.")]
    [InlineData("Notice how a priority queue helps select the minimum element.")]
    public void Inspect_L1UnderstandWithSpoilerTerms_FailsWithSpoilerViolation(string content)
    {
        var result = SolutionGuard.Inspect(HintLevel.L1_Understand, content);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.SpoilerAtEarlyLevel, result.Violations);
    }

    [Theory]
    [InlineData("Think about applying a sliding window as you expand right.")]
    [InlineData("Store each visited number in a hash map as you iterate.")]
    [InlineData("Try two pointers moving towards the middle.")]
    public void Inspect_L2DirectionWithSpoilerTerms_FailsWithSpoilerViolation(string content)
    {
        var result = SolutionGuard.Inspect(HintLevel.L2_Direction, content);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.SpoilerAtEarlyLevel, result.Violations);
    }

    [Fact]
    public void Inspect_L3ConceptsNamingDataStructure_PassesValidation()
    {
        // At L3, naming the data structure or algorithm is allowed per Codexo hint ladder rules
        var content = "A hash table allows you to record complements with expected O(1) time complexity per lookup.";
        var result = SolutionGuard.Inspect(HintLevel.L3_Concepts, content);

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Inspect_FullSolutionClaim_FailsWithFullSolutionViolation()
    {
        var content = "Here is the complete solution for Two Sum: step 1 create dictionary, step 2 loop array, step 3 return result.";
        var result = SolutionGuard.Inspect(HintLevel.L4_Steps, content);

        Assert.False(result.IsValid);
        Assert.Contains(GuardViolationType.ContainsFullSolution, result.Violations);
    }

    [Theory]
    [InlineData(HintLevel.L1_Understand, "Notice that the array can have negative numbers and target can also be negative. What does that mean for indices?")]
    [InlineData(HintLevel.L2_Direction, "What information from earlier elements do you wish you remembered when you arrive at element i?")]
    [InlineData(HintLevel.L4_Steps, "Step 1: Initialize an empty mapping.\nStep 2: For each item, compute complement.\nStep 3: If present, return indices.")]
    [InlineData(HintLevel.L5_Deep, "Watch out for when nums[i] == target - nums[i]. Ensure you don't match an element with itself.")]
    public void Inspect_CleanHintsAtAllLevels_PassValidation(HintLevel level, string content)
    {
        var result = SolutionGuard.Inspect(level, content);

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }
}
