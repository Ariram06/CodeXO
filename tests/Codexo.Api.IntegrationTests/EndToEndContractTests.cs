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

public class EndToEndContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndToEndContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EndToEnd_FullExtensionLifecycle_SucceedsWithProgressionFeedbackAndRateLimiting()
    {
        var client = _factory.CreateClient();

        // 1. Authenticate user as would occur in Angular AuthPanel
        var email = $"e2e_{Guid.NewGuid():N}@codexo.dev";
        var password = "StrongPassword123!";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password, password));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var authData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authData?.Token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);

        // 2. Problem context intake from simulated LeetCode content script extraction
        var intakePayload = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: "1",
            Title: "Two Sum",
            Slug: "two-sum",
            Url: "https://leetcode.com/problems/two-sum/",
            Description: "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.",
            Difficulty: "Easy");

        var sessionResponse = await client.PostAsJsonAsync("/api/sessions/context", intakePayload);
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        var session = await sessionResponse.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal("Two Sum", session.Problem.Title);
        Assert.Equal(HintLevel.L1_Understand, session.CurrentLevel);

        // 3. Request initial hint (L1 Understand)
        var l1Response = await client.PostAsync($"/api/sessions/{session.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.OK, l1Response.StatusCode);
        var l1Hint = await l1Response.Content.ReadFromJsonAsync<HintResponseDto>();
        Assert.NotNull(l1Hint);
        Assert.Equal(HintLevel.L1_Understand, l1Hint.Level);
        Assert.False(string.IsNullOrWhiteSpace(l1Hint.Hint));

        // 4. Verify Attempt Gating: Requesting next hint without attempt must return 400 Bad Request
        var prematureL2Response = await client.PostAsync($"/api/sessions/{session.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.BadRequest, prematureL2Response.StatusCode);

        // 5. Submit valid learner attempt
        var attemptRequest = new SubmitAttemptRequestDto(
            AttemptText: "I realized brute-forcing all pairs is O(n^2), so I want to use a hash map for O(1) complements.");
        var attemptResponse = await client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/attempts", attemptRequest);
        Assert.Equal(HttpStatusCode.OK, attemptResponse.StatusCode);
        var attemptResult = await attemptResponse.Content.ReadFromJsonAsync<AttemptEvaluationResultDto>();
        Assert.NotNull(attemptResult);
        Assert.Equal(AttemptStatus.Approved, attemptResult.Status);
        Assert.True(attemptResult.Unlocked);

        // 6. Request unlocked hint (L2 Direction)
        var l2Response = await client.PostAsync($"/api/sessions/{session.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.OK, l2Response.StatusCode);
        var l2Hint = await l2Response.Content.ReadFromJsonAsync<HintResponseDto>();
        Assert.NotNull(l2Hint);
        Assert.Equal(HintLevel.L2_Direction, l2Hint.Level);

        // 7. Submit Feedback for L1 Hint (Thumbs up with comment)
        var feedbackRequest = new SubmitFeedbackRequestDto(
            IsHelpful: true,
            Comment: "Great restatement of the constraints!");
        var feedbackResponse = await client.PostAsJsonAsync($"/api/hints/{l1Hint.Id}/feedback", feedbackRequest);
        Assert.Equal(HttpStatusCode.OK, feedbackResponse.StatusCode);
        var feedbackResult = await feedbackResponse.Content.ReadFromJsonAsync<HintFeedbackResponseDto>();
        Assert.NotNull(feedbackResult);
        Assert.True(feedbackResult.IsHelpful);
        Assert.Equal("Great restatement of the constraints!", feedbackResult.Comment);

        // 8. Update Study Preferences (Language = Python)
        var prefRequest = new UpdateUserPreferenceRequestDto(
            PreferredLanguage: "Python",
            AutoUnlockL1: true);
        var prefResponse = await client.PutAsJsonAsync("/api/preferences", prefRequest);
        Assert.Equal(HttpStatusCode.OK, prefResponse.StatusCode);
        var prefResult = await prefResponse.Content.ReadFromJsonAsync<UserPreferenceResponseDto>();
        Assert.NotNull(prefResult);
        Assert.Equal("Python", prefResult.PreferredLanguage);
        Assert.True(prefResult.AutoUnlockL1);

        // 9. Query Hint Status
        var statusResponse = await client.GetAsync($"/api/sessions/{session.SessionId}/hints/status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var status = await statusResponse.Content.ReadFromJsonAsync<SessionHintStatusDto>();
        Assert.NotNull(status);
        Assert.Equal(HintLevel.L2_Direction, status.CurrentLevel);
    }

    [Fact]
    public async Task EndToEnd_UnauthenticatedAccess_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var intakePayload = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: "1",
            Title: "Two Sum",
            Slug: "two-sum",
            Url: "https://leetcode.com/problems/two-sum/",
            Description: "Given nums...",
            Difficulty: "Easy");

        var response = await client.PostAsJsonAsync("/api/sessions/context", intakePayload);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
