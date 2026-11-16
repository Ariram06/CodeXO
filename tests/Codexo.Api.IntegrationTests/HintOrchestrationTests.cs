namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Codexo.Core.AI;
using Codexo.Core.Dtos.Hints;
using Codexo.Core.DTOs.Auth;
using Codexo.Core.DTOs.Problems;
using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class HintOrchestrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HintOrchestrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string UserId, string Email)> CreateAuthenticatedClientAsync()
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
        return (client, auth.UserId, uniqueEmail);
    }

    private async Task<ProblemSessionResponse> CreateSessionAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: $"hint-test-{uniqueId}",
            Title: $"Hint Problem {uniqueId}",
            Slug: $"hint-problem-{uniqueId}",
            Url: $"https://leetcode.com/problems/hint-problem-{uniqueId}/",
            Difficulty: "Medium",
            Description: "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.");

        var response = await client.PostAsJsonAsync("/api/sessions/context", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(session);
        return session;
    }

    [Fact]
    public async Task FullHintLifecycle_L1ThroughL5_UnlocksProgressivelyWithValidAttempts()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync();
        var session = await CreateSessionAsync(client);
        var sessionId = session.SessionId;

        // 1. Initial status: None -> L1 requires NO attempt
        var statusResponse = await client.GetFromJsonAsync<SessionHintStatusDto>($"/api/sessions/{sessionId}/hints/status");
        Assert.NotNull(statusResponse);
        Assert.Equal(HintLevel.None, statusResponse.CurrentLevel);
        Assert.True(statusResponse.CanRequestNextHint);
        Assert.Equal(HintLevel.L1_Understand, statusResponse.NextLevel);
        Assert.False(statusResponse.AttemptRequiredForNextLevel);

        // 2. Request L1 hint (unlocked without attempt)
        var l1Response = await client.PostAsync($"/api/sessions/{sessionId}/hints", null);
        var errBody = await l1Response.Content.ReadAsStringAsync();
        Assert.True(l1Response.IsSuccessStatusCode, $"Status: {l1Response.StatusCode}, Body: {errBody}");
        var l1Hint = await l1Response.Content.ReadFromJsonAsync<HintResponseDto>();
        Assert.NotNull(l1Hint);
        Assert.Equal(HintLevel.L1_Understand, l1Hint.Level);
        Assert.False(string.IsNullOrWhiteSpace(l1Hint.Hint));

        // 3. Attempting to get L2 without an attempt must fail
        var lockedL2Response = await client.PostAsync($"/api/sessions/{sessionId}/hints", null);
        Assert.Equal(HttpStatusCode.BadRequest, lockedL2Response.StatusCode);

        // 4. Submit invalid keysmash attempt -> Rejected
        var invalidAttempt = await client.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/attempts",
            new SubmitAttemptRequestDto("asdfasdfasdfasdf"));
        Assert.Equal(HttpStatusCode.OK, invalidAttempt.StatusCode);
        var invalidResult = await invalidAttempt.Content.ReadFromJsonAsync<AttemptEvaluationResultDto>();
        Assert.NotNull(invalidResult);
        Assert.False(invalidResult.Unlocked);
        Assert.Equal(AttemptStatus.Rejected, invalidResult.Status);

        // 5. Submit valid attempt for L2 -> Approved
        var validAttemptL2 = await client.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/attempts",
            new SubmitAttemptRequestDto("I realized that for each element, we need to check if target minus element has already been seen in the array."));
        Assert.Equal(HttpStatusCode.OK, validAttemptL2.StatusCode);
        var validResultL2 = await validAttemptL2.Content.ReadFromJsonAsync<AttemptEvaluationResultDto>();
        Assert.NotNull(validResultL2);
        Assert.True(validResultL2.Unlocked);
        Assert.Equal(AttemptStatus.Approved, validResultL2.Status);

        // 6. Request L2 hint -> Succeeds
        var l2Response = await client.PostAsync($"/api/sessions/{sessionId}/hints", null);
        Assert.Equal(HttpStatusCode.OK, l2Response.StatusCode);
        var l2Hint = await l2Response.Content.ReadFromJsonAsync<HintResponseDto>();
        Assert.NotNull(l2Hint);
        Assert.Equal(HintLevel.L2_Direction, l2Hint.Level);

        // 7. Progress through L3, L4, L5 with valid attempts
        var levels = new[]
        {
            (HintLevel.L3_Concepts, "I considered using a dictionary or hash map to map each visited number to its array index."),
            (HintLevel.L4_Steps, "First initialize the map, then iterate through the numbers, compute the complement, query the map, and return indices if found."),
            (HintLevel.L5_Deep, "I considered edge cases such as duplicate numbers, large array sizes, and ensuring the same element is not used twice.")
        };

        foreach (var (level, attemptText) in levels)
        {
            var attemptResp = await client.PostAsJsonAsync(
                $"/api/sessions/{sessionId}/attempts",
                new SubmitAttemptRequestDto(attemptText));
            Assert.Equal(HttpStatusCode.OK, attemptResp.StatusCode);
            var attemptResult = await attemptResp.Content.ReadFromJsonAsync<AttemptEvaluationResultDto>();
            Assert.NotNull(attemptResult);
            Assert.True(attemptResult.Unlocked);

            var hintResp = await client.PostAsync($"/api/sessions/{sessionId}/hints", null);
            Assert.Equal(HttpStatusCode.OK, hintResp.StatusCode);
            var hint = await hintResp.Content.ReadFromJsonAsync<HintResponseDto>();
            Assert.NotNull(hint);
            Assert.Equal(level, hint.Level);
        }

        // 8. Verify all 5 hints are stored and retrievable
        var allHints = await client.GetFromJsonAsync<List<HintResponseDto>>($"/api/sessions/{sessionId}/hints");
        Assert.NotNull(allHints);
        Assert.Equal(5, allHints.Count);
        Assert.Equal(HintLevel.L1_Understand, allHints[0].Level);
        Assert.Equal(HintLevel.L5_Deep, allHints[4].Level);

        // 9. Status at L5 shows complete / cannot request next
        var finalStatus = await client.GetFromJsonAsync<SessionHintStatusDto>($"/api/sessions/{sessionId}/hints/status");
        Assert.NotNull(finalStatus);
        Assert.Equal(HintLevel.L5_Deep, finalStatus.CurrentLevel);
        Assert.False(finalStatus.CanRequestNextHint);
        Assert.Null(finalStatus.NextLevel);
    }

    [Fact]
    public async Task AttemptGating_EmptyAttempt_ReturnsBadRequest()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync();
        var session = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/sessions/{session.SessionId}/attempts",
            new SubmitAttemptRequestDto(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AttemptGating_FullSolutionSolicitation_RejectsWithEducationalMessage()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync();
        var session = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/sessions/{session.SessionId}/attempts",
            new SubmitAttemptRequestDto("Please give me the full complete solution code for this problem right now."));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Body: {body}");
        var result = await response.Content.ReadFromJsonAsync<AttemptEvaluationResultDto>();
        Assert.NotNull(result);
        Assert.False(result.Unlocked);
        Assert.Equal("FullSolutionSolicitation", result.RejectionReason);
        Assert.Contains("Codexo does not provide full solutions", result.FeedbackMessage);
    }

    [Fact]
    public async Task Concurrency_ParallelHintRequests_DoNotAdvanceLevelTwice()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync();
        var session = await CreateSessionAsync(client);
        var sessionId = session.SessionId;

        // Fire 5 simultaneous requests for L1
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.PostAsync($"/api/sessions/{sessionId}/hints", null))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // All responses should be successful
        foreach (var resp in responses)
        {
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        // Verify that exactly ONE hint exists (L1) and current level is L1
        var allHints = await client.GetFromJsonAsync<List<HintResponseDto>>($"/api/sessions/{sessionId}/hints");
        Assert.NotNull(allHints);
        Assert.Single(allHints);
        Assert.Equal(HintLevel.L1_Understand, allHints[0].Level);

        var status = await client.GetFromJsonAsync<SessionHintStatusDto>($"/api/sessions/{sessionId}/hints/status");
        Assert.NotNull(status);
        Assert.Equal(HintLevel.L1_Understand, status.CurrentLevel);
    }

    [Fact]
    public async Task PerUserIsolation_UserCannotRequestHintsForAnotherUsersSession()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _, _) = await CreateAuthenticatedClientAsync();

        var sessionA = await CreateSessionAsync(clientA);
        var sessionAId = sessionA.SessionId;

        // User B attempts to request hints for User A's session -> 404
        var hintResponse = await clientB.PostAsync($"/api/sessions/{sessionAId}/hints", null);
        Assert.Equal(HttpStatusCode.NotFound, hintResponse.StatusCode);

        // User B attempts to submit attempt for User A's session -> 404
        var attemptResponse = await clientB.PostAsJsonAsync(
            $"/api/sessions/{sessionAId}/attempts",
            new SubmitAttemptRequestDto("Valid attempt for solving the two sum array complement problem."));
        Assert.Equal(HttpStatusCode.NotFound, attemptResponse.StatusCode);

        // User B attempts to view User A's hints -> 404
        var getHintsResponse = await clientB.GetAsync($"/api/sessions/{sessionAId}/hints");
        Assert.Equal(HttpStatusCode.NotFound, getHintsResponse.StatusCode);
    }

    [Fact]
    public async Task Caching_L1Hint_CachedAcrossDifferentSessionsForSameProblem()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _, _) = await CreateAuthenticatedClientAsync();

        var uniqueProblemId = Guid.NewGuid().ToString("N");
        var problemRequest = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: $"shared-cache-{uniqueProblemId}",
            Title: $"Shared Problem {uniqueProblemId}",
            Slug: $"shared-problem-{uniqueProblemId}",
            Url: $"https://leetcode.com/problems/shared-problem-{uniqueProblemId}/",
            Difficulty: "Easy",
            Description: "Given an array of integers nums, return true if any value appears at least twice in the array.");

        var sessionRespA = await clientA.PostAsJsonAsync("/api/sessions/context", problemRequest);
        var sessionA = await sessionRespA.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(sessionA);

        var sessionRespB = await clientB.PostAsJsonAsync("/api/sessions/context", problemRequest);
        var sessionB = await sessionRespB.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(sessionB);

        // User A requests L1 hint (populates cache)
        var hintRespA = await clientA.PostAsync($"/api/sessions/{sessionA.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.OK, hintRespA.StatusCode);
        var hintA = await hintRespA.Content.ReadFromJsonAsync<HintResponseDto>();
        Assert.NotNull(hintA);

        // User B requests L1 hint for the same problem (hits cache)
        var hintRespB = await clientB.PostAsync($"/api/sessions/{sessionB.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.OK, hintRespB.StatusCode);
        var hintB = await hintRespB.Content.ReadFromJsonAsync<HintResponseDto>();
        Assert.NotNull(hintB);

        // Content is identical from cache, but sessions and hint record IDs are separate
        Assert.Equal(hintA.Hint, hintB.Hint);
        Assert.NotEqual(hintA.Id, hintB.Id);
        Assert.NotEqual(hintA.SessionId, hintB.SessionId);
    }

    [Fact]
    public async Task SolutionGuardFallback_WhenAiProducesSpoilers_ReturnsSafeFallbackHint()
    {
        var spoilerAiClient = new SpoilerAiClient();
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IAiClient>(_ => spoilerAiClient);
            });
        });

        var client = customFactory.CreateClient();
        var uniqueEmail = $"user_{Guid.NewGuid():N}@codexo.dev";
        var password = "SecurePassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(uniqueEmail, password, password));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var session = await CreateSessionAsync(client);

        var hintResponse = await client.PostAsync($"/api/sessions/{session.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.OK, hintResponse.StatusCode);

        var hint = await hintResponse.Content.ReadFromJsonAsync<HintResponseDto>();
        Assert.NotNull(hint);
        Assert.True(hint.IsFallback);
        Assert.False(string.IsNullOrWhiteSpace(hint.Hint));

        // Fallback hint must pass SolutionGuard
        var guardResult = SolutionGuard.Inspect(HintLevel.L1_Understand, hint.Hint);
        Assert.True(guardResult.IsValid);
    }

    private class SpoilerAiClient : IAiClient
    {
        public Task<AiHintResponse> GenerateHintAsync(AiPromptRequest request, CancellationToken cancellationToken = default)
        {
            // Intentionally violates SolutionGuard by outputting code fences and compilable code
            return Task.FromResult(new AiHintResponse(
                Level: request.Level,
                Hint: "```python\ndef twoSum(nums, target):\n    return [0, 1]\n```",
                GuidingQuestions: Array.Empty<string>(),
                Concepts: Array.Empty<string>(),
                Steps: Array.Empty<string>(),
                Pitfalls: Array.Empty<string>()));
        }
    }
}
