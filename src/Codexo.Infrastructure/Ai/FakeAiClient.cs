namespace Codexo.Infrastructure.AI;

using System.Runtime.CompilerServices;
using System.Text;
using Codexo.Core.AI;

/// <summary>
/// Deterministic offline AI client for unit testing, CI pipelines, and local development.
/// Requires zero API keys and zero network connectivity.
/// </summary>
public class FakeAiClient : IAiClient
{
    public Task<string> GenerateChatResponseAsync(
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        var sb = new StringBuilder();
        sb.AppendLine("### Analysis & Solution");
        sb.AppendLine();
        sb.AppendLine("Based on your code and question, here is a complete working solution and complexity analysis:");
        sb.AppendLine();
        sb.AppendLine("```python");
        sb.AppendLine("def solve(nums, target):");
        sb.AppendLine("    lookup = {}");
        sb.AppendLine("    for i, num in enumerate(nums):");
        sb.AppendLine("        complement = target - num");
        sb.AppendLine("        if complement in lookup:");
        sb.AppendLine("            return [lookup[complement], i]");
        sb.AppendLine("        lookup[num] = i");
        sb.AppendLine("    return []");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("#### Complexity Analysis");
        sb.AppendLine("- **Time Complexity**: $O(N)$ single pass hash table lookup.");
        sb.AppendLine("- **Space Complexity**: $O(N)$ auxiliary hash table space.");

        return Task.FromResult(sb.ToString());
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        string systemPrompt,
        IEnumerable<(string Role, string Content)> history,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        var fullResponse = await GenerateChatResponseAsync(systemPrompt, history, userPrompt, cancellationToken);
        var words = fullResponse.Split(' ');

        for (var i = 0; i < words.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = i == words.Length - 1 ? words[i] : words[i] + " ";
            yield return chunk;

            // Small asynchronous yield to simulate streaming token delivery
            var delayMs = userPrompt.Contains("cancel", StringComparison.OrdinalIgnoreCase) ? 150 : 10;
            await Task.Delay(delayMs, cancellationToken);
        }
    }
}
