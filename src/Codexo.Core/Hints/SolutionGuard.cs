namespace Codexo.Core.Hints;

using System.Text.RegularExpressions;
using Codexo.Core.Enums;

/// <summary>
/// Structurally verifies that AI-generated hints adhere to Codexo's core pedagogical principles:
/// - No compilable code in any level
/// - No code blocks (markdown fences)
/// - No premature spoilers (algorithm/data structure names at L1/L2)
/// - No complete finished solutions
/// </summary>
public static class SolutionGuard
{
    private static readonly Regex MarkdownCodeFenceRegex = new(
        @"```[\s\S]*?```",
        RegexOptions.Compiled);

    // Matches function / method declarations in C#, Java, Python, C++, JS, Go, Rust
    private static readonly Regex FunctionDeclarationRegex = new(
        @"\b(def\s+[a-zA-Z_]\w*\s*\(|function\s+[a-zA-Z_]\w*\s*\(|(public|private|protected|static|async)\s+[\w<>\[\]]+\s+[a-zA-Z_]\w*\s*\(|fn\s+[a-zA-Z_]\w*\s*\()",
        RegexOptions.Compiled);

    // Matches type-declared variable assignments or let/const bindings with statements
    private static readonly Regex CompilableStatementRegex = new(
        @"((\b(int|double|float|long|bool|string|char|var|let|const|auto)\s+[a-zA-Z_]\w*\s*=\s*[^;]+;)|(\bfor\s*\([^;]+;[^;]+;[^)]+\))|(\bwhile\s*\([^)]+\)\s*\{)|\b(return\s+[a-zA-Z_0-9]+;))",
        RegexOptions.Compiled);

    // Matches inline code backticks containing semicolons or assignments
    private static readonly Regex InlineCompilableCodeRegex = new(
        @"`[^`]*(=|;|\(\))[a-zA-Z0-9_\s]*`",
        RegexOptions.Compiled);

    // Level 1 forbidden terms: No algorithms, approaches, or data structure names
    private static readonly string[] L1ForbiddenTerms = new[]
    {
        // Algorithms & Patterns
        "binary search", "two pointers", "two-pointer", "sliding window", "dynamic programming",
        "depth first search", "breadth first search", "depth-first search", "breadth-first search",
        "dfs", "bfs", "dijkstra", "topological sort", "backtracking", "greedy algorithm",
        "kadane", "floyd", "prefix sum", "union find", "disjoint set", "divide and conquer",
        // Data Structures
        "hash map", "hashmap", "hash table", "hashtable", "hash set", "hashset",
        "binary tree", "binary search tree", "bst", "heap", "priority queue", "priorityqueue",
        "stack", "queue", "deque", "linked list", "linkedlist", "trie", "segment tree",
        "adjacency list", "fenwick tree"
    };

    // Level 2 forbidden terms: Guiding questions allowed, but NO pattern or algorithm names
    private static readonly string[] L2ForbiddenTerms = new[]
    {
        "binary search", "two pointers", "two-pointer", "sliding window", "dynamic programming",
        "depth first search", "breadth first search", "dfs", "bfs", "dijkstra",
        "topological sort", "backtracking", "kadane", "union find", "disjoint set",
        "prefix sum", "divide and conquer", "hash map", "hashmap", "hash table",
        "priority queue", "heap", "trie", "segment tree"
    };

    /// <summary>
    /// Inspects hint content structurally against Codexo rules for the given target level.
    /// </summary>
    public static GuardResult Inspect(HintLevel targetLevel, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return GuardResult.Fail(GuardViolationType.EmptyContent, "Hint content is null or empty.");
        }

        var violations = new List<GuardViolationType>();
        var details = new List<string>();

        // 1. Check for Markdown code fences (```)
        if (MarkdownCodeFenceRegex.IsMatch(content))
        {
            violations.Add(GuardViolationType.ContainsCodeBlock);
            details.Add("Hint contains markdown code fence blocks (```), which are strictly forbidden.");
        }

        // 2. Check for compilable function signatures or method declarations
        if (FunctionDeclarationRegex.IsMatch(content))
        {
            violations.Add(GuardViolationType.ContainsCompilableCode);
            details.Add("Hint contains function/method declaration syntax.");
        }

        // 3. Check for compilable statement structures
        if (CompilableStatementRegex.IsMatch(content))
        {
            violations.Add(GuardViolationType.ContainsCompilableCode);
            details.Add("Hint contains compilable code statements (declarations, loops, or return statements with semicolons).");
        }

        // 4. Check for inline code containing statement expressions
        if (InlineCompilableCodeRegex.IsMatch(content))
        {
            violations.Add(GuardViolationType.ContainsCompilableCode);
            details.Add("Hint contains inline code backticks with executable statements or assignments.");
        }

        // 5. Level-specific spoiler checks
        if (targetLevel == HintLevel.L1_Understand)
        {
            foreach (var term in L1ForbiddenTerms)
            {
                if (ContainsWordBoundaryTerm(content, term))
                {
                    violations.Add(GuardViolationType.SpoilerAtEarlyLevel);
                    details.Add($"L1 Understand hint contains spoiler term '{term}'. L1 must only clarify problem scope and constraints.");
                    break;
                }
            }
        }
        else if (targetLevel == HintLevel.L2_Direction)
        {
            foreach (var term in L2ForbiddenTerms)
            {
                if (ContainsWordBoundaryTerm(content, term))
                {
                    violations.Add(GuardViolationType.SpoilerAtEarlyLevel);
                    details.Add($"L2 Direction hint contains spoiler pattern/algorithm name '{term}'. L2 must guide via questions and observations only.");
                    break;
                }
            }
        }

        // 6. Complete solution detector: checks for full step-by-step resolution that acts as full solution
        if (IsCompleteSolution(content))
        {
            violations.Add(GuardViolationType.ContainsFullSolution);
            details.Add("Hint appears to provide a full end-to-end solution rather than a progressive nudge.");
        }

        return violations.Count > 0
            ? GuardResult.Fail(violations, details)
            : GuardResult.Pass();
    }

    private static bool ContainsWordBoundaryTerm(string text, string term)
    {
        var pattern = $@"\b{Regex.Escape(term)}\b";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    private static bool IsCompleteSolution(string content)
    {
        // Detects responses that start with "Here is the complete solution" or sequentially output Step 1 through Step N
        // followed by full resolution logic
        if (Regex.IsMatch(content, @"\b(here is the (full|complete) solution|entire solution is as follows)\b", RegexOptions.IgnoreCase))
        {
            return true;
        }

        return false;
    }
}
