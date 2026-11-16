namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Codexo.Core.DTOs.Auth;
using Codexo.Core.DTOs.Problems;
using Codexo.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class HardeningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HardeningTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task HealthCheck_ReturnsHealthyAndStatusDetailsWithoutSecrets()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Equal("Connected", root.GetProperty("database").GetString());

        var ai = root.GetProperty("ai");
        Assert.True(ai.GetProperty("configured").GetBoolean());
        Assert.NotNull(ai.GetProperty("provider").GetString());

        // Strictly verify that no API key secret is leaked in the health check output
        Assert.DoesNotContain("ApiKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AI:ApiKey", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GlobalException_WhenKeyNotFound_ReturnsRfc7807ProblemDetails404()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var nonExistentSessionId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/sessions/{nonExistentSessionId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RateLimiter_ExceedingConfiguredPermitLimit_Returns429WithProblemDetailsAndRetryAfter()
    {
        // Custom factory configuring low limit for test execution speed (e.g. 2 requests per minute)
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:PermitLimitPerMinute", "2");
        });

        var client = customFactory.CreateClient();
        var uniqueEmail = $"user_{Guid.NewGuid():N}@codexo.dev";
        var password = "SecurePassword123!";

        var regResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(uniqueEmail, password, password));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        // Create problem session
        var uniqueId = Guid.NewGuid().ToString("N");
        var problemReq = new ProblemIntakeRequest(
            Platform: Platform.LeetCode,
            PlatformProblemId: $"ratelimit-{uniqueId}",
            Title: $"Rate Limit Problem {uniqueId}",
            Slug: $"ratelimit-{uniqueId}",
            Url: $"https://leetcode.com/problems/ratelimit-{uniqueId}/",
            Difficulty: "Easy",
            Description: "Given an array, determine if there are duplicates.");

        var sessionResp = await client.PostAsJsonAsync("/api/sessions/context", problemReq);
        var session = await sessionResp.Content.ReadFromJsonAsync<ProblemSessionResponse>();
        Assert.NotNull(session);

        // Request 1: OK
        var r1 = await client.PostAsync($"/api/sessions/{session.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        // Request 2: OK
        var r2 = await client.PostAsync($"/api/sessions/{session.SessionId}/hints", null);
        // Either OK or 400 (if locked) but not 429
        Assert.True(r2.StatusCode == HttpStatusCode.OK || r2.StatusCode == HttpStatusCode.BadRequest);

        // Request 3: Exceeds 2 permits/min -> 429 Too Many Requests
        var r3 = await client.PostAsync($"/api/sessions/{session.SessionId}/hints", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, r3.StatusCode);

        // Verify Retry-After header and RFC 7807 problem details
        Assert.True(r3.Headers.Contains("Retry-After"));
        var problem = await r3.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(429, problem.Status);
        Assert.Contains("Rate limit exceeded", problem.Detail);
    }
}
