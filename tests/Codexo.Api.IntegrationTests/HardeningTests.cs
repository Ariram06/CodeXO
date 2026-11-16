namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.DTOs.Auth;
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
        var nonExistentMessageId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/messages/{nonExistentMessageId}/feedback");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RateLimiter_ExceedingConfiguredPermitLimit_Returns429WithProblemDetailsAndRetryAfter()
    {
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

        var chatReq = new SendMessageRequest(
            ConversationId: null,
            Message: "Quick query",
            Context: null);

        // Request 1: OK
        var r1 = await client.PostAsJsonAsync("/api/chat/messages", chatReq);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        // Request 2: OK
        var r2 = await client.PostAsJsonAsync("/api/chat/messages", chatReq);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        // Request 3: Exceeds 2 permits/min -> 429 Too Many Requests
        var r3 = await client.PostAsJsonAsync("/api/chat/messages", chatReq);
        Assert.Equal(HttpStatusCode.TooManyRequests, r3.StatusCode);

        // Verify Retry-After header and RFC 7807 problem details
        Assert.True(r3.Headers.Contains("Retry-After"));
        var problem = await r3.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(429, problem.Status);
        Assert.Contains("Rate limit exceeded", problem.Detail);
    }
}
