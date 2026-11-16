namespace Codexo.Api.IntegrationTests.AI;

using Codexo.Infrastructure.AI;
using Xunit;

public class FakeAiClientTests
{
    private readonly FakeAiClient _client = new();

    [Fact]
    public async Task GenerateChatResponseAsync_ReturnsContextualMarkdownWithCodeAndComplexity()
    {
        var systemPrompt = "You are Codexo.";
        var history = new List<(string Role, string Content)>
        {
            ("user", "Hello"),
            ("assistant", "Hi, how can I help you?")
        };
        var userPrompt = "How do I solve two sum?";

        var response = await _client.GenerateChatResponseAsync(systemPrompt, history, userPrompt);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response));
        Assert.Contains("```python", response);
        Assert.Contains("Time Complexity", response);
        Assert.Contains("Space Complexity", response);
    }
}
