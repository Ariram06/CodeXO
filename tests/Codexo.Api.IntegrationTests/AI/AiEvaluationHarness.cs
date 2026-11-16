namespace Codexo.Api.IntegrationTests.AI;

using Codexo.Core.AI;
using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Codexo.Core.Prompts;
using Codexo.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class AiEvaluationHarness
{
    public record BenchmarkProblem(string Title, string Description, string Category);

    public static readonly IReadOnlyList<BenchmarkProblem> BenchmarkProblems = new List<BenchmarkProblem>
    {
        new("Two Sum", "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.", "Arrays & Hash Table"),
        new("Valid Anagram", "Given two strings s and t, return true if t is an anagram of s, and false otherwise.", "Strings & Frequency Map"),
        new("Reverse Linked List", "Given the head of a singly linked list, reverse the list, and return the reversed list.", "Linked Lists"),
        new("Binary Search", "Given an array of integers nums which is sorted in ascending order, and an integer target, write a function to search target in nums.", "Search & Divide and Conquer"),
        new("Longest Substring Without Repeating Characters", "Given a string s, find the length of the longest substring without repeating characters.", "Sliding Window"),
        new("Coin Change", "You are given an integer array coins representing coins of different denominations and an integer amount. Return the fewest number of coins that you need to make up that amount.", "Dynamic Programming"),
        new("Maximum Subarray", "Given an integer array nums, find the subarray with the largest sum, and return its sum.", "Kadane's Algorithm / DP"),
        new("Valid Parentheses", "Given a string s containing just the characters '(', ')', '{', '}', '[' and ']', determine if the input string is valid.", "Stacks"),
        new("Merge Intervals", "Given an array of intervals where intervals[i] = [starti, endi], merge all overlapping intervals.", "Sorting & Intervals"),
        new("Lowest Common Ancestor of a Binary Search Tree", "Given a binary search tree (BST), find the lowest common ancestor (LCA) node of two given nodes in the BST.", "Trees & BST Properties")
    };

    private readonly FakeAiClient _fakeClient = new();

    [Fact]
    public async Task EvaluationHarness_All10BenchmarkProblems_PassStructuredSchemaAndSolutionGuard()
    {
        var evaluatedCount = 0;
        var passedCount = 0;

        foreach (var problem in BenchmarkProblems)
        {
            foreach (HintLevel level in new[] { HintLevel.L1_Understand, HintLevel.L2_Direction, HintLevel.L3_Concepts, HintLevel.L4_Steps, HintLevel.L5_Deep })
            {
                evaluatedCount++;

                var request = new AiPromptRequest(
                    Level: level,
                    ProblemTitle: problem.Title,
                    ProblemDescription: problem.Description,
                    UserAttempt: "I have considered the basic brute force approach.",
                    PreviousHints: Array.Empty<string>());

                var response = await _fakeClient.GenerateHintAsync(request);

                Assert.NotNull(response);
                Assert.Equal(level, response.Level);
                Assert.False(string.IsNullOrWhiteSpace(response.Hint));

                var guardResult = SolutionGuard.Inspect(level, response.Hint);
                Assert.True(guardResult.IsValid, $"Problem '{problem.Title}' Level {level} failed SolutionGuard: {string.Join(", ", guardResult.Details)}");

                passedCount++;
            }
        }

        Assert.Equal(50, evaluatedCount);
        Assert.Equal(50, passedCount);
    }

    [Fact]
    public async Task LiveGeminiSmokeTest_WithConfiguredApiKey_ReturnsValidHint()
    {
        // Opt-in live smoke test: Reads AI:ApiKey from User Secrets / environment
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var apiKey = config["AI:ApiKey"] ?? config["AI__ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Skip gracefully when running in CI or offline environments without an API key
            return;
        }

        var settings = new AISettings
        {
            Provider = "OpenAiCompatible",
            BaseUrl = config["AI:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/",
            Model = config["AI:Model"] ?? "gemini-4-hydrogen",
            ApiKey = apiKey,
            MaxTokens = 800,
            Temperature = 0.2,
            TimeoutSeconds = 60
        };

        using var httpClient = new HttpClient();
        var promptBuilder = new HintPromptBuilder();
        var client = new OpenAiCompatibleAiClient(
            httpClient,
            Options.Create(settings),
            promptBuilder,
            NullLogger<OpenAiCompatibleAiClient>.Instance);

        var request = new AiPromptRequest(
            Level: HintLevel.L1_Understand,
            ProblemTitle: "Two Sum",
            ProblemDescription: "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.",
            UserAttempt: "I considered checking all pairs.",
            PreviousHints: Array.Empty<string>());

        var response = await client.GenerateHintAsync(request);

        Assert.NotNull(response);
        Assert.Equal(HintLevel.L1_Understand, response.Level);
        Assert.False(string.IsNullOrWhiteSpace(response.Hint));

        var guardResult = SolutionGuard.Inspect(HintLevel.L1_Understand, response.Hint);
        Assert.True(guardResult.IsValid, $"Live Gemini response failed SolutionGuard: {string.Join(", ", guardResult.Details)}");
    }
}
