namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.DTOs.Auth;
using Codexo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class DataMinimizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DataMinimizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SendMessageAsync_NeverStoresRawCodeOrPageText_InDatabase()
    {
        var client = _factory.CreateClient();
        var email = $"min_{Guid.NewGuid():N}@codexo.dev";
        var password = "SecurePassword123!";
        var regResp = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password, password));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        const string secretRawCode = "const confidentialAlgorithmCode = () => { doWork(); };";
        const string rawProblemText = "Confidential proprietary problem description with secret hints.";
        const string rawSelection = "confidentialAlgorithmCode";

        var context = new PageContext(
            Platform: "LeetCode",
            Url: "https://leetcode.com/problems/custom-problem",
            Title: "Custom Problem",
            ProblemText: rawProblemText,
            Language: "javascript",
            Code: secretRawCode,
            Selection: rawSelection,
            ErrorText: "SyntaxError: confidential",
            IsTruncated: false);

        var request = new SendMessageRequest(
            ConversationId: null,
            Message: "How do I fix this function?",
            Context: context);

        var response = await client.PostAsJsonAsync("/api/chat/messages", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify database contents directly via EF Core context
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userMessages = await dbContext.Messages
            .Include(m => m.Conversation)
            .Where(m => m.Conversation != null && m.Conversation.UserId == auth.UserId)
            .ToListAsync();

        Assert.Equal(2, userMessages.Count); // 1 User + 1 Assistant

        foreach (var msg in userMessages)
        {
            // PROOF: Raw code and raw problem text NEVER exist in any database column or content field
            Assert.DoesNotContain(secretRawCode, msg.Content);
            Assert.DoesNotContain(rawProblemText, msg.Content);
            Assert.DoesNotContain(rawSelection, msg.Content);

            if (msg.Metadata != null)
            {
                // Metadata contains ONLY structural summary
                Assert.Equal("LeetCode", msg.Metadata.Platform);
                Assert.Equal("leetcode.com", msg.Metadata.Host);
                Assert.Equal("Custom Problem", msg.Metadata.Title);
                Assert.Equal("javascript", msg.Metadata.Language);
                Assert.Equal(1, msg.Metadata.CodeLineCount);
            }
        }
    }

    [Fact]
    public async Task StreamMessageAsync_NeverStoresRawCodeOrPageText_InDatabase()
    {
        var client = _factory.CreateClient();
        var email = $"min_stream_{Guid.NewGuid():N}@codexo.dev";
        var password = "SecurePassword123!";
        var regResp = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password, password));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        const string secretRawCode = "def secret_streaming_algorithm(): return 42";
        const string rawProblemText = "Secret problem description for streaming test.";

        var context = new PageContext(
            Platform: "HackerRank",
            Url: "https://www.hackerrank.com/challenges/sample-problem",
            Title: "Sample Challenge",
            ProblemText: rawProblemText,
            Language: "python",
            Code: secretRawCode,
            Selection: null,
            ErrorText: null,
            IsTruncated: false);

        var request = new SendMessageRequest(
            ConversationId: null,
            Message: "Help with this challenge",
            Context: context);

        var response = await client.PostAsJsonAsync("/api/chat/stream", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Read stream to completion so final assistant message saves
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("[DONE]", body);

        // Verify database contents directly
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userMessages = await dbContext.Messages
            .Include(m => m.Conversation)
            .Where(m => m.Conversation != null && m.Conversation.UserId == auth.UserId)
            .ToListAsync();

        Assert.Equal(2, userMessages.Count); // 1 User + 1 Assistant

        foreach (var msg in userMessages)
        {
            Assert.DoesNotContain(secretRawCode, msg.Content);
            Assert.DoesNotContain(rawProblemText, msg.Content);

            if (msg.Metadata != null)
            {
                Assert.Equal("HackerRank", msg.Metadata.Platform);
                Assert.Equal("www.hackerrank.com", msg.Metadata.Host);
                Assert.Equal("Sample Challenge", msg.Metadata.Title);
                Assert.Equal("python", msg.Metadata.Language);
                Assert.Equal(1, msg.Metadata.CodeLineCount);
            }
        }
    }
}
