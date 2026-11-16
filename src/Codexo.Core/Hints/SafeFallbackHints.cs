namespace Codexo.Core.Hints;

using Codexo.Core.AI;
using Codexo.Core.Enums;

/// <summary>
/// Provides guaranteed-safe static fallback hints when AI generation fails or violates SolutionGuard rules.
/// Structurally compliant with all level exclusion and inclusion constraints.
/// </summary>
public static class SafeFallbackHints
{
    public static AiHintResponse GetFallback(HintLevel level, string? problemTitle = null)
    {
        return level switch
        {
            HintLevel.L1_Understand => new AiHintResponse(
                Level: HintLevel.L1_Understand,
                Hint: "Begin by understanding the problem specifications: carefully examine the expected input boundaries, return value formats, and problem constraints. Are negative numbers or empty collections allowed?",
                GuidingQuestions: new[]
                {
                    "What are the minimum and maximum possible sizes for the inputs?",
                    "What should your solution return if the input contains no valid match or is empty?"
                },
                Concepts: Array.Empty<string>(),
                Steps: Array.Empty<string>(),
                Pitfalls: Array.Empty<string>()),

            HintLevel.L2_Direction => new AiHintResponse(
                Level: HintLevel.L2_Direction,
                Hint: "Consider what intermediate information you need as you inspect elements sequentially. Can you remember elements you have already seen to avoid repetitive scans from scratch?",
                GuidingQuestions: new[]
                {
                    "What information from previous elements helps you evaluate the current element?",
                    "How can you avoid re-evaluating the same sub-problem multiple times?"
                },
                Concepts: Array.Empty<string>(),
                Steps: Array.Empty<string>(),
                Pitfalls: Array.Empty<string>()),

            HintLevel.L3_Concepts => new AiHintResponse(
                Level: HintLevel.L3_Concepts,
                Hint: "The optimal technique leverages an associative lookup table to achieve linear time complexity by trading auxiliary memory for instant retrieval.",
                GuidingQuestions: new[]
                {
                    "How does constant-time lookup eliminate the need for nested scanning loops?",
                    "What is the space-time trade-off between nested iteration and auxiliary lookup tables?"
                },
                Concepts: new[]
                {
                    "Lookup table for constant-time access",
                    "Space-time trade-off: O(N) memory for O(N) runtime",
                    "Single-pass processing"
                },
                Steps: Array.Empty<string>(),
                Pitfalls: Array.Empty<string>()),

            HintLevel.L4_Steps => new AiHintResponse(
                Level: HintLevel.L4_Steps,
                Hint: "Follow these plain-English implementation steps to structure your solution:",
                GuidingQuestions: Array.Empty<string>(),
                Concepts: new[] { "Sequential Traversal", "Auxiliary State Tracking" },
                Steps: new[]
                {
                    "1. Initialize an auxiliary collection to store previously seen items and their positions.",
                    "2. Iterate through each element in the input sequence from start to finish.",
                    "3. Calculate the complement or required matching condition for the current element.",
                    "4. If the required match exists in your auxiliary collection, return the successful result.",
                    "5. Otherwise, record the current element in your collection and continue to the next item."
                },
                Pitfalls: Array.Empty<string>()),

            HintLevel.L5_Deep => new AiHintResponse(
                Level: HintLevel.L5_Deep,
                Hint: "Review these critical edge cases, complexity characteristics, and common pitfalls:",
                GuidingQuestions: Array.Empty<string>(),
                Concepts: new[] { "Time & Space Complexity", "Edge Case Hardening" },
                Steps: Array.Empty<string>(),
                Pitfalls: new[]
                {
                    "Self-Matching: Ensure an element cannot match with itself when the required complement equals the current value.",
                    "Duplicate Values: Decide how duplicate keys should be stored or updated in your state collection.",
                    "Boundary Inputs: Test with 0 elements, 1 element, all negative values, and maximum possible integer values.",
                    "Complexity: Target O(N) time with O(N) auxiliary space instead of O(N^2) brute-force nested loops."
                }),

            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported hint level for safe fallback.")
        };
    }
}
