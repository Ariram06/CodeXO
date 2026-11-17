namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.Dtos.Feedback;
using Codexo.Core.Dtos.Preferences;
using Codexo.Core.DTOs.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class FeedbackAndPreferenceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FeedbackAndPreferenceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string UserId)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var uniqueEmail = $"user_{Guid.NewGuid():N}@codexo.dev";
        var password = "SecurePassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(uniqueEmail, password, password));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth.UserId);
    }

    private async Task<ChatMessageResponse> SendChatMessageAsync(HttpClient client)
    {
        var request = new SendMessageRequest(
            ConversationId: null,
            Message: "How do I optimize two sum using a hash map?",
            Context: new PageContext(
                Platform: "LeetCode",
                Url: "https://leetcode.com/problems/two-sum/",
                Title: "Two Sum",
                ProblemText: "Given nums and target...",
                Language: "python",
                Code: "def twoSum(nums, target): pass",
                Selection: null,
                ErrorText: null,
                IsTruncated: false));

        var response = await client.PostAsJsonAsync("/api/chat/messages", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var chatResponse = await response.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(chatResponse);
        return chatResponse;
    }

    [Fact]
    public async Task SubmitFeedback_ForMessage_Succeeds()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var chatMsg = await SendChatMessageAsync(client);

        var feedbackReq = new SubmitFeedbackRequestDto(
            IsHelpful: true,
            Comment: "Great explanation!");

        var response = await client.PostAsJsonAsync($"/api/messages/{chatMsg.MessageId}/feedback", feedbackReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var feedback = await response.Content.ReadFromJsonAsync<MessageFeedbackResponseDto>();
        Assert.NotNull(feedback);
        Assert.Equal(chatMsg.MessageId, feedback.MessageId);
        Assert.True(feedback.IsHelpful);
        Assert.Equal("Great explanation!", feedback.Comment);
    }

    [Fact]
    public async Task SubmitFeedback_Upserts_WhenCalledAgain()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var chatMsg = await SendChatMessageAsync(client);

        var initialReq = new SubmitFeedbackRequestDto(IsHelpful: true, Comment: "Initial");
        await client.PostAsJsonAsync($"/api/messages/{chatMsg.MessageId}/feedback", initialReq);

        var updateReq = new SubmitFeedbackRequestDto(IsHelpful: false, Comment: "Changed my mind");
        var updateResp = await client.PostAsJsonAsync($"/api/messages/{chatMsg.MessageId}/feedback", updateReq);

        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var feedback = await updateResp.Content.ReadFromJsonAsync<MessageFeedbackResponseDto>();
        Assert.NotNull(feedback);
        Assert.False(feedback.IsHelpful);
        Assert.Equal("Changed my mind", feedback.Comment);
    }

    [Fact]
    public async Task SubmitFeedback_ForOtherUsersMessage_Returns404()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _) = await CreateAuthenticatedClientAsync();

        var chatMsgA = await SendChatMessageAsync(clientA);

        var feedbackReq = new SubmitFeedbackRequestDto(IsHelpful: true, Comment: "Tampering");
        var response = await clientB.PostAsJsonAsync($"/api/messages/{chatMsgA.MessageId}/feedback", feedbackReq);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Preferences_Get_And_Update_Succeeds()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var getResp = await client.GetAsync("/api/preferences");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var initialPref = await getResp.Content.ReadFromJsonAsync<UserPreferenceResponseDto>();
        Assert.NotNull(initialPref);
        Assert.Equal("low", initialPref.ReasoningLevel);
        Assert.NotNull(initialPref.Model);

        var updateReq = new UpdateUserPreferenceRequestDto(
            PreferredLanguage: "Python",
            AutoUnlockL1: false,
            ReasoningLevel: "high");

        var putResp = await client.PutAsJsonAsync("/api/preferences", updateReq);
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

        var updatedPref = await putResp.Content.ReadFromJsonAsync<UserPreferenceResponseDto>();
        Assert.NotNull(updatedPref);
        Assert.Equal("Python", updatedPref.PreferredLanguage);
        Assert.False(updatedPref.AutoUnlockL1);
        Assert.Equal("high", updatedPref.ReasoningLevel);
        Assert.NotNull(updatedPref.Model);
    }
}
