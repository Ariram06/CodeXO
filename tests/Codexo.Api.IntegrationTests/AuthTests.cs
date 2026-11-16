namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Codexo.Core.DTOs.Auth;
using Codexo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns200WithJwtToken()
    {
        var uniqueEmail = $"testuser_{Guid.NewGuid():N}@codexo.dev";
        var request = new RegisterRequest(
            Email: uniqueEmail,
            Password: "SecurePassword123!",
            ConfirmPassword: "SecurePassword123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.False(string.IsNullOrWhiteSpace(result.UserId));
        Assert.Equal(uniqueEmail, result.Email);
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400ValidationError()
    {
        var request = new RegisterRequest(
            Email: $"testuser_{Guid.NewGuid():N}@codexo.dev",
            Password: "weak",
            ConfirmPassword: "weak");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MismatchedPasswords_Returns400ValidationError()
    {
        var request = new RegisterRequest(
            Email: $"testuser_{Guid.NewGuid():N}@codexo.dev",
            Password: "SecurePassword123!",
            ConfirmPassword: "DifferentPassword123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithJwtToken()
    {
        // First register a unique user
        var uniqueEmail = $"testlogin_{Guid.NewGuid():N}@codexo.dev";
        var password = "SecurePassword123!";
        var registerRequest = new RegisterRequest(uniqueEmail, password, password);
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);

        // Now attempt login
        var loginRequest = new LoginRequest(uniqueEmail, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(uniqueEmail, result.Email);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401WithGenericMessage()
    {
        // Dev user is seeded on startup
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbInitializer.SeedAsync(context, isDevelopment: true);

        var loginRequest = new LoginRequest(DbInitializer.DevUserEmail, "WrongPassword123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password.", content);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401WithIdenticalGenericMessage()
    {
        var loginRequest = new LoginRequest("nonexistent_user_404@codexo.dev", "AnyPassword123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password.", content);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401Unauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tampered.jwt.invalidtoken");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200WithUserDetails()
    {
        // Register a new user and obtain token
        var uniqueEmail = $"testme_{Guid.NewGuid():N}@codexo.dev";
        var password = "SecurePassword123!";
        var registerRequest = new RegisterRequest(uniqueEmail, password, password);
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);

        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userInfo = await response.Content.ReadFromJsonAsync<UserInfoResponse>();
        Assert.NotNull(userInfo);
        Assert.Equal(auth.UserId, userInfo.UserId);
        Assert.Equal(uniqueEmail, userInfo.Email);
    }
}
