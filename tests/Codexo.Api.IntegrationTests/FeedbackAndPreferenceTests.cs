namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Codexo.Core.Dtos.Feedback;
using Codexo.Core.Dtos.Hints;
using Codexo.Core.Dtos.Preferences;
using Codexo.Core.DTOs.Auth;
using Codexo.Core.DTOs.Problems;
using Codexo.Core.Enums;
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

    private async Task<HintResponseDto> CreateSessionAndGetL1HintAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: $"feedback-prob-{uniqueId}",
            Title: $"Feedback Problem {uniqueId}",
            Slug: $"feedback-problem-{uniqueId}",
            Url: $"https://leetcode.com/problems/feedback-problem-{uniqueId}/",
            Difficulty: "Easy",
            Description: "Given an array of integers, return the indices of two elements that sum to a target.");

        var sessionResp = await client.PostAsJsonAsync("/api/sessions/context", request);
        Assert.Equal(HttpStatusCode.OK, sessionResp.StatusCode);
        var session = await sessionResp.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(session);

        var hintResp = await client.PostAsync($"/api/sessions/{session.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.OK, hintResp.StatusCode);
        var hint = await hintResp.Content.ReadFromJsonAsync<HintResponseDto>();
        Assert.NotNull(hint);

        return hint;
    }

    [Fact]
    public async Task SubmitFeedback_ValidHelpfulWithComment_PersistsAndReturnsDto()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var hint = await CreateSessionAndGetL1HintAsync(client);

        var feedbackRequest = new SubmitFeedbackRequestDto(
            IsHelpful: true,
            Comment: "The guiding questions helped me focus on array bounds without giving away the approach.");

        var response = await client.PostAsJsonAsync($"/api/hints/{hint.Id}/feedback", feedbackRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var feedback = await response.Content.ReadFromJsonAsync<HintFeedbackResponseDto>();
        Assert.NotNull(feedback);
        Assert.Equal(hint.Id, feedback.HintId);
        Assert.Equal(userId, feedback.UserId);
        Assert.True(feedback.IsHelpful);
        Assert.Equal("The guiding questions helped me focus on array bounds without giving away the approach.", feedback.Comment);

        // Verify retrieval endpoint
        var getResponse = await client.GetAsync($"/api/hints/{hint.Id}/feedback");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<HintFeedbackResponseDto>();
        Assert.NotNull(retrieved);
        Assert.Equal(feedback.Id, retrieved.Id);
    }

    [Fact]
    public async Task SubmitFeedback_Exceeding500CharsComment_FailsValidationWith400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var hint = await CreateSessionAndGetL1HintAsync(client);

        var longComment = new string('x', 501);
        var feedbackRequest = new SubmitFeedbackRequestDto(IsHelpful: false, Comment: longComment);

        var response = await client.PostAsJsonAsync($"/api/hints/{hint.Id}/feedback", feedbackRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitFeedback_TwiceForSameHint_UpdatesExistingFeedbackIdempotently()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var hint = await CreateSessionAndGetL1HintAsync(client);

        // First submission: thumbs down
        var first = await client.PostAsJsonAsync(
            $"/api/hints/{hint.Id}/feedback",
            new SubmitFeedbackRequestDto(IsHelpful: false, Comment: "Initial thoughts: confusing constraints."));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstFeedback = await first.Content.ReadFromJsonAsync<HintFeedbackResponseDto>();
        Assert.NotNull(firstFeedback);

        // Second submission: thumbs up with updated comment
        var second = await client.PostAsJsonAsync(
            $"/api/hints/{hint.Id}/feedback",
            new SubmitFeedbackRequestDto(IsHelpful: true, Comment: "Re-read and now it makes total sense!"));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondFeedback = await second.Content.ReadFromJsonAsync<HintFeedbackResponseDto>();
        Assert.NotNull(secondFeedback);

        // Same record ID, updated fields
        Assert.Equal(firstFeedback.Id, secondFeedback.Id);
        Assert.True(secondFeedback.IsHelpful);
        Assert.Equal("Re-read and now it makes total sense!", secondFeedback.Comment);
    }

    [Fact]
    public async Task SubmitFeedback_OnAnotherUsersHint_Returns404NotFound()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _) = await CreateAuthenticatedClientAsync();

        var hintUserA = await CreateSessionAndGetL1HintAsync(clientA);

        // User B attempts to submit feedback for User A's hint
        var response = await clientB.PostAsJsonAsync(
            $"/api/hints/{hintUserA.Id}/feedback",
            new SubmitFeedbackRequestDto(IsHelpful: true, Comment: "Malicious cross-user feedback."));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPreferences_ReturnsDefaultsIfNoneExist()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/preferences");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pref = await response.Content.ReadFromJsonAsync<UserPreferenceResponseDto>();
        Assert.NotNull(pref);
        Assert.Equal(userId, pref.UserId);
        Assert.Equal("C#", pref.PreferredLanguage);
        Assert.True(pref.AutoUnlockL1);
    }

    [Fact]
    public async Task UpdatePreferences_ValidLanguage_UpsertsSuccessfully()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var updateReq = new UpdateUserPreferenceRequestDto(
            PreferredLanguage: "Python",
            AutoUnlockL1: false);

        var putResponse = await client.PutAsJsonAsync("/api/preferences", updateReq);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<UserPreferenceResponseDto>();
        Assert.NotNull(updated);
        Assert.Equal(userId, updated.UserId);
        Assert.Equal("Python", updated.PreferredLanguage);
        Assert.False(updated.AutoUnlockL1);

        // Confirm subsequent GET retrieves updated preferences
        var getResponse = await client.GetAsync("/api/preferences");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<UserPreferenceResponseDto>();
        Assert.NotNull(retrieved);
        Assert.Equal("Python", retrieved.PreferredLanguage);
        Assert.False(retrieved.AutoUnlockL1);
    }

    [Fact]
    public async Task UpdatePreferences_EmptyLanguage_FailsValidationWith400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var updateReq = new UpdateUserPreferenceRequestDto(
            PreferredLanguage: "",
            AutoUnlockL1: true);

        var response = await client.PutAsJsonAsync("/api/preferences", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PerUserPreferences_IsolatedAcrossDifferentUsers()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _) = await CreateAuthenticatedClientAsync();

        await clientA.PutAsJsonAsync("/api/preferences", new UpdateUserPreferenceRequestDto("TypeScript", false));
        await clientB.PutAsJsonAsync("/api/preferences", new UpdateUserPreferenceRequestDto("Java", true));

        var prefA = await clientA.GetFromJsonAsync<UserPreferenceResponseDto>("/api/preferences");
        var prefB = await clientB.GetFromJsonAsync<UserPreferenceResponseDto>("/api/preferences");

        Assert.NotNull(prefA);
        Assert.NotNull(prefB);
        Assert.Equal("TypeScript", prefA.PreferredLanguage);
        Assert.False(prefA.AutoUnlockL1);
        Assert.Equal("Java", prefB.PreferredLanguage);
        Assert.True(prefB.AutoUnlockL1);
    }
}
