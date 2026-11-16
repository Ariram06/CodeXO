namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.Dtos.Feedback;
using Codexo.Core.DTOs.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class EndToEndContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndToEndContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Chat_FullExtensionLifecycle_SendChatQuery_And_SubmitFeedback_Succeeds()
    {
        var client = _factory.CreateClient();

        // 1. Authenticate user
        var email = $"chat_e2e_{Guid.NewGuid():N}@codexo.dev";
        var password = "StrongPassword123!";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password, password));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var authData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authData?.Token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);

        // 2. Send initial message with active page and editor context
        var pageContext = new PageContext(
            Platform: "LeetCode",
            Url: "https://leetcode.com/problems/two-sum/",
            Title: "Two Sum",
            ProblemText: "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.",
            Language: "python",
            Code: "class Solution:\n    def twoSum(self, nums: List[int], target: int) -> List[int]:\n        pass",
            Selection: "def twoSum",
            ErrorText: null,
            IsTruncated: false);

        var firstRequest = new SendMessageRequest(
            ConversationId: null,
            Message: "How can I solve this in O(N) time with a hash map?",
            Context: pageContext);

        var firstResponse = await client.PostAsJsonAsync("/api/chat/messages", firstRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstChat = await firstResponse.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(firstChat);
        Assert.NotEqual(Guid.Empty, firstChat.ConversationId);
        Assert.NotEqual(Guid.Empty, firstChat.MessageId);
        Assert.Equal("assistant", firstChat.Role);
        Assert.Contains("```python", firstChat.Content);
        Assert.NotNull(firstChat.Metadata);
        Assert.Equal("LeetCode", firstChat.Metadata.Platform);
        Assert.Equal("leetcode.com", firstChat.Metadata.Host);
        Assert.Equal(3, firstChat.Metadata.CodeLineCount);

        // 3. Send follow-up in the same conversation
        var followUpRequest = new SendMessageRequest(
            ConversationId: firstChat.ConversationId,
            Message: "What if there are duplicates in nums?",
            Context: null);

        var followUpResponse = await client.PostAsJsonAsync("/api/chat/messages", followUpRequest);
        Assert.Equal(HttpStatusCode.OK, followUpResponse.StatusCode);

        var followUpChat = await followUpResponse.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(followUpChat);
        Assert.Equal(firstChat.ConversationId, followUpChat.ConversationId);
        Assert.NotEqual(firstChat.MessageId, followUpChat.MessageId);

        // 4. Submit helpfulness feedback on the assistant response
        var feedbackRequest = new SubmitFeedbackRequestDto(
            IsHelpful: true,
            Comment: "Direct and accurate explanation!");

        var feedbackResponse = await client.PostAsJsonAsync(
            $"/api/messages/{followUpChat.MessageId}/feedback",
            feedbackRequest);

        Assert.Equal(HttpStatusCode.OK, feedbackResponse.StatusCode);
        var feedback = await feedbackResponse.Content.ReadFromJsonAsync<MessageFeedbackResponseDto>();
        Assert.NotNull(feedback);
        Assert.True(feedback.IsHelpful);
    }

    [Fact]
    public async Task LegacyEndpoints_Return404_ProvingLadderAndSessionsRemoved()
    {
        var client = _factory.CreateClient();
        var email = $"legacy_{Guid.NewGuid():N}@codexo.dev";
        var password = "StrongPassword123!";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password, password));
        var authData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        var randomGuid = Guid.NewGuid();

        // 1. Session context intake
        var sessionIntake = await client.PostAsJsonAsync("/api/sessions/context", new { });
        Assert.Equal(HttpStatusCode.NotFound, sessionIntake.StatusCode);

        // 2. Session by ID
        var sessionGet = await client.GetAsync($"/api/sessions/{randomGuid}");
        Assert.Equal(HttpStatusCode.NotFound, sessionGet.StatusCode);

        // 3. Hint request
        var hintPost = await client.PostAsync($"/api/sessions/{randomGuid}/hints", null);
        Assert.Equal(HttpStatusCode.NotFound, hintPost.StatusCode);

        // 4. Attempt submission
        var attemptPost = await client.PostAsJsonAsync($"/api/sessions/{randomGuid}/attempts", new { });
        Assert.Equal(HttpStatusCode.NotFound, attemptPost.StatusCode);

        // 5. Hint-specific feedback endpoint
        var hintFeedback = await client.PostAsJsonAsync($"/api/hints/{randomGuid}/feedback", new { });
        Assert.Equal(HttpStatusCode.NotFound, hintFeedback.StatusCode);
    }
}
