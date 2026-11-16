namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Codexo.Core.DTOs.Auth;
using Codexo.Core.DTOs.Problems;
using Codexo.Core.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class SessionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SessionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(HttpClient Client, string UserId, string Email)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var uniqueEmail = $"user_{Guid.NewGuid():N}@codexo.dev";
        var password = "SecurePassword123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(uniqueEmail, password, password));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth.UserId, uniqueEmail);
    }

    [Fact]
    public async Task Intake_ValidProblemContext_CreatesProblemAndActiveSession()
    {
        var (client, userId, _) = await CreateAuthenticatedClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N");

        var request = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: $"intake-{uniqueId}",
            Title: $"Unique Problem {uniqueId}",
            Slug: $"unique-problem-{uniqueId}",
            Url: $"https://leetcode.com/problems/unique-problem-{uniqueId}/",
            Difficulty: "Medium",
            Description: "Given a collection of unique elements, compute the optimal subset matching the target criteria.");

        var response = await client.PostAsJsonAsync("/api/sessions/context", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(HintLevel.L1_Understand, session.CurrentLevel);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Equal($"Unique Problem {uniqueId}", session.Problem.Title);
        Assert.Equal($"intake-{uniqueId}", session.Problem.PlatformProblemId);
    }

    [Fact]
    public async Task Intake_ExistingBenchmarkProblem_ReusesProblemAndReturnsActiveSession()
    {
        var (client, userId, _) = await CreateAuthenticatedClientAsync();

        // Two Sum is seeded with ID 11111111-1111-1111-1111-111111111111 and PlatformProblemId "1"
        var request = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: "1",
            Title: "Two Sum",
            Slug: "two-sum",
            Url: "https://leetcode.com/problems/two-sum/",
            Difficulty: "Easy",
            Description: "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.");

        var response1 = await client.PostAsJsonAsync("/api/sessions/context", request);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var session1 = await response1.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(session1);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), session1.Problem.Id);

        // Submitting same problem context again must return the same session idempotently
        var response2 = await client.PostAsJsonAsync("/api/sessions/context", request);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var session2 = await response2.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(session2);
        Assert.Equal(session1.SessionId, session2.SessionId);
    }

    [Fact]
    public async Task Intake_InvalidContext_FailsValidationWith400()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync();

        // Description exceeding 20,000 characters
        var oversizedDescription = new string('x', 20001);
        var invalidRequest = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: "1",
            Title: "Test",
            Slug: "test",
            Url: "not-a-valid-url",
            Difficulty: "Easy",
            Description: oversizedDescription);

        var response = await client.PostAsJsonAsync("/api/sessions/context", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ContextChange_UserSwitchesProblems_ReturnsSeparateSessionsWithoutDataCorruption()
    {
        var (client, userId, _) = await CreateAuthenticatedClientAsync();
        var idA = Guid.NewGuid().ToString("N");
        var idB = Guid.NewGuid().ToString("N");

        var problemA = new ProblemIntakeRequest(
            Platform.LeetCode,
            $"probA-{idA}",
            $"Problem A {idA}",
            $"prob-a-{idA}",
            $"https://leetcode.com/problems/prob-a-{idA}",
            "Description for problem A testing context change.",
            "Easy");

        var problemB = new ProblemIntakeRequest(
            Platform.LeetCode,
            $"probB-{idB}",
            $"Problem B {idB}",
            $"prob-b-{idB}",
            $"https://leetcode.com/problems/prob-b-{idB}",
            "Description for problem B testing context change.",
            "Medium");

        // 1. User starts session on Problem A
        var resA1 = await client.PostAsJsonAsync("/api/sessions/context", problemA);
        Assert.Equal(HttpStatusCode.OK, resA1.StatusCode);
        var sessionA1 = await resA1.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(sessionA1);

        // 2. User switches to Problem B
        var resB = await client.PostAsJsonAsync("/api/sessions/context", problemB);
        Assert.Equal(HttpStatusCode.OK, resB.StatusCode);
        var sessionB = await resB.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(sessionB);
        Assert.NotEqual(sessionA1.SessionId, sessionB.SessionId);
        Assert.NotEqual(sessionA1.Problem.Id, sessionB.Problem.Id);

        // 3. User switches back to Problem A
        var resA2 = await client.PostAsJsonAsync("/api/sessions/context", problemA);
        Assert.Equal(HttpStatusCode.OK, resA2.StatusCode);
        var sessionA2 = await resA2.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(sessionA2);
        Assert.Equal(sessionA1.SessionId, sessionA2.SessionId);
    }

    [Fact]
    public async Task PerUserIsolation_UserCannotReadOrAccessAnotherUsersSession()
    {
        // 1. User 1 logs in and creates a session
        var (clientUser1, user1Id, _) = await CreateAuthenticatedClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N");

        var problem = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: $"iso-{uniqueId}",
            Title: $"Isolation Problem {uniqueId}",
            Slug: $"iso-problem-{uniqueId}",
            Url: $"https://leetcode.com/problems/iso-problem-{uniqueId}/",
            Difficulty: "Hard",
            Description: "Given an isolation constraint, verify that users cannot access sessions belonging to other accounts.");

        var intakeResponseUser1 = await clientUser1.PostAsJsonAsync("/api/sessions/context", problem);
        Assert.Equal(HttpStatusCode.OK, intakeResponseUser1.StatusCode);

        var sessionUser1 = await intakeResponseUser1.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(sessionUser1);
        Assert.Equal(user1Id, sessionUser1.UserId);

        // 2. User 2 logs in with their own distinct account
        var (clientUser2, user2Id, _) = await CreateAuthenticatedClientAsync();
        Assert.NotEqual(user1Id, user2Id);

        // 3. User 2 directly attempts to query User 1's session by ID -> MUST return 404 Not Found (no existence leak)
        var crossUserResponse = await clientUser2.GetAsync($"/api/sessions/{sessionUser1.SessionId}");
        Assert.Equal(HttpStatusCode.NotFound, crossUserResponse.StatusCode);

        // 4. User 2 checks active session for the same problem before starting -> MUST return 404 Not Found
        var activeBeforeIntake = await clientUser2.GetAsync($"/api/sessions/active?platform={Platform.LeetCode}&platformProblemId=iso-{uniqueId}");
        Assert.Equal(HttpStatusCode.NotFound, activeBeforeIntake.StatusCode);

        // 5. User 2 intakes the SAME problem -> receives their own unique session (Session 2 != Session 1)
        var intakeResponseUser2 = await clientUser2.PostAsJsonAsync("/api/sessions/context", problem);
        Assert.Equal(HttpStatusCode.OK, intakeResponseUser2.StatusCode);

        var sessionUser2 = await intakeResponseUser2.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(sessionUser2);
        Assert.NotEqual(sessionUser1.SessionId, sessionUser2.SessionId);
        Assert.Equal(user2Id, sessionUser2.UserId);
        Assert.Equal(sessionUser1.Problem.Id, sessionUser2.Problem.Id); // Same catalog problem, distinct user sessions

        // 6. User 1 can still access their session intact
        var user1Check = await clientUser1.GetAsync($"/api/sessions/{sessionUser1.SessionId}");
        Assert.Equal(HttpStatusCode.OK, user1Check.StatusCode);
    }
}
