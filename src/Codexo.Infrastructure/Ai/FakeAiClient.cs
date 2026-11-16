namespace Codexo.Infrastructure.AI;

using Codexo.Core.AI;
using Codexo.Core.Enums;

/// <summary>
/// Deterministic offline AI client for unit testing, CI pipelines, and local development.
/// Requires zero API keys and zero network connectivity.
/// </summary>
public class FakeAiClient : IAiClient
{
    public Task<AiHintResponse> GenerateHintAsync(AiPromptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var title = string.IsNullOrWhiteSpace(request.ProblemTitle) ? "the problem" : request.ProblemTitle.Trim();

        var response = request.Level switch
        {
            HintLevel.L1_Understand => new AiHintResponse(
                Level: HintLevel.L1_Understand,
                Hint: "Focus on clarifying the problem requirements: understand the expected input types, return values, constraints, and boundary edge cases. Avoid thinking about specific algorithms or implementations yet.",
                GuidingQuestions: new[]
                {
                    "What are the bounds on input size, and can the elements be negative or zero?",
                    "What should you return if no solution exists or if the input is empty?"
                },
                Concepts: Array.Empty<string>(),
                Steps: Array.Empty<string>(),
                Pitfalls: Array.Empty<string>()),

            HintLevel.L2_Direction => new AiHintResponse(
                Level: HintLevel.L2_Direction,
                Hint: "Consider what information from earlier elements is needed to resolve the current state efficiently. Can you record intermediate observations to avoid repeated backward scans?",
                GuidingQuestions: new[]
                {
                    "If you pre-record what you have seen, what key allows constant-time lookup?",
                    "How can you avoid re-evaluating the same sub-problem multiple times?"
                },
                Concepts: Array.Empty<string>(),
                Steps: Array.Empty<string>(),
                Pitfalls: Array.Empty<string>()),

            HintLevel.L3_Concepts => new AiHintResponse(
                Level: HintLevel.L3_Concepts,
                Hint: "The optimal technique leverages an associative lookup table (Hash Table / Dictionary) to achieve linear time by trading O(N) auxiliary memory.",
                GuidingQuestions: new[]
                {
                    "How does single-pass hash mapping prevent double-counting the current element?",
                    "What is the average and worst-case time complexity of hash table lookups?"
                },
                Concepts: new[]
                {
                    "Hash Table / Dictionary for O(1) average lookup",
                    "Space-time trade-off",
                    "Complement matching"
                },
                Steps: Array.Empty<string>(),
                Pitfalls: Array.Empty<string>()),

            HintLevel.L4_Steps => new AiHintResponse(
                Level: HintLevel.L4_Steps,
                Hint: "Follow these logical implementation steps to structure your solution:",
                GuidingQuestions: Array.Empty<string>(),
                Concepts: new[] { "Hash Table", "Single-pass traversal" },
                Steps: new[]
                {
                    "1. Initialize an empty hash table to store visited elements as keys and their indices as values.",
                    "2. Iterate through the input sequence tracking the current index and element.",
                    "3. Compute the required complement for the current element.",
                    "4. Query the hash table: if the complement exists, return the stored index and the current index.",
                    "5. Otherwise, store the current element and index in the hash table and advance to the next element."
                },
                Pitfalls: Array.Empty<string>()),

            HintLevel.L5_Deep => new AiHintResponse(
                Level: HintLevel.L5_Deep,
                Hint: "Key pitfalls, complexity characteristics, and edge cases to consider:",
                GuidingQuestions: Array.Empty<string>(),
                Concepts: new[] { "Time & Space Complexity", "Edge Case Hardening" },
                Steps: Array.Empty<string>(),
                Pitfalls: new[]
                {
                    "Self-matching: Ensure the current element is not matched with itself when target equals twice the element value.",
                    "Time Complexity: O(N) average time for single-pass iteration with O(1) hash map operations.",
                    "Space Complexity: O(N) worst-case space to hold up to N elements in auxiliary memory.",
                    "Duplicate values: Ensure that inserting duplicate values updates or preserves indices without causing incorrect lookups."
                }),

            _ => throw new ArgumentOutOfRangeException(nameof(request.Level), request.Level, "Unsupported hint level.")
        };

        return Task.FromResult(response);
    }
}
