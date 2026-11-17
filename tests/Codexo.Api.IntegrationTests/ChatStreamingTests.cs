namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.DTOs.Auth;
using Codexo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ChatStreamingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatStreamingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChatStream_ValidRequest_ReturnsMultipleSseChunksAndDoneEvent()
    {
        var client = _factory.CreateClient();

        // 1. Authenticate user
        var email = $"stream_user_{Guid.NewGuid():N}@codexo.dev";
        var password = "StrongPassword123!";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password, password));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var authData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authData?.Token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);

        // 2. Prepare streaming request with PageContext
        var pageContext = new PageContext(
            Platform: "LeetCode",
            Url: "https://leetcode.com/problems/two-sum/",
            Title: "Two Sum",
            ProblemText: "Given an array of integers nums and an integer target...",
            Language: "python",
            Code: "def twoSum(nums, target): pass",
            Selection: null,
            ErrorText: null,
            IsTruncated: false);

        var request = new SendMessageRequest(
            ConversationId: null,
            Message: "Explain the hash map approach for Two Sum.",
            Context: pageContext);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        // 3. Read SSE stream chunks
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var receivedDeltas = new List<string>();
        var gotDoneEvent = false;
        Guid conversationId = Guid.Empty;
        Guid messageId = Guid.Empty;

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var payload = line[6..].Trim();
                if (payload == "[DONE]")
                {
                    gotDoneEvent = true;
                    break;
                }

                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                var deltaProp = root.TryGetProperty("delta", out var dProp) ? dProp : (root.TryGetProperty("Delta", out var dp) ? dp : default);
                if (deltaProp.ValueKind == JsonValueKind.String)
                {
                    var delta = deltaProp.GetString();
                    if (!string.IsNullOrEmpty(delta))
                    {
                        receivedDeltas.Add(delta);
                    }
                }

                if ((root.TryGetProperty("conversationId", out var convProp) || root.TryGetProperty("ConversationId", out convProp)) && convProp.TryGetGuid(out var cId))
                {
                    conversationId = cId;
                }

                if ((root.TryGetProperty("messageId", out var msgProp) || root.TryGetProperty("MessageId", out msgProp)) && msgProp.TryGetGuid(out var mId))
                {
                    messageId = mId;
                }
            }
        }

        Assert.True(gotDoneEvent, "SSE stream must conclude with a data: [DONE] event.");
        Assert.True(receivedDeltas.Count > 1, $"Expected multiple incremental chunks, but got {receivedDeltas.Count}.");
        Assert.NotEqual(Guid.Empty, conversationId);
        Assert.NotEqual(Guid.Empty, messageId);

        // Verify full accumulated message was persisted in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedMessage = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        Assert.NotNull(persistedMessage);
        Assert.Contains(receivedDeltas[0], persistedMessage.Content);
    }

    [Fact]
    public async Task ChatStream_ClientCancellation_AbortsStreamAndPersistsPartialContent()
    {
        var client = _factory.CreateClient();

        var email = $"cancel_user_{Guid.NewGuid():N}@codexo.dev";
        var password = "StrongPassword123!";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password, password));
        var authData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authData?.Token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);

        var request = new SendMessageRequest(
            ConversationId: null,
            Message: "Generate a very long explanation that will be cancelled.",
            Context: null);

        using var cts = new CancellationTokenSource();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
        {
            Content = JsonContent.Create(request)
        };

        var chunksRead = 0;
        Guid messageId = Guid.Empty;

        try
        {
            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(cts.Token)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
                {
                    chunksRead++;
                    using var doc = JsonDocument.Parse(line[6..].Trim());
                    if (doc.RootElement.TryGetProperty("messageId", out var mProp) ||
                        doc.RootElement.TryGetProperty("MessageId", out mProp))
                    {
                        messageId = mProp.GetGuid();
                    }

                    // Cancel client-side after reading 2 chunks to trigger upstream cancellation
                    if (chunksRead >= 2)
                    {
                        cts.Cancel();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected client-side abort
        }

        Assert.True(chunksRead >= 2, "Should have received at least 2 chunks before cancellation.");

        // Poll database to allow background cancellation persistence to complete
        Codexo.Core.Entities.Message? persisted = null;
        for (var attempt = 0; attempt < 25; attempt++)
        {
            await Task.Delay(100);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            persisted = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (persisted != null)
            {
                break;
            }
        }

        Assert.NotNull(persisted);
        Assert.True(persisted.Content.Contains("[Generation stopped]"), $"ACTUAL CONTENT WAS: '{persisted.Content}'");
    }

    [Fact]
    public async Task ChatStream_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new SendMessageRequest(null, "Hello without token", null);
        var response = await client.PostAsJsonAsync("/api/chat/stream", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatStream_EmptyMessage_Returns400()
    {
        var client = _factory.CreateClient();

        var email = $"val_{Guid.NewGuid():N}@codexo.dev";
        var password = "StrongPassword123!";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, password));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var request = new SendMessageRequest(null, "   ", null);
        var response = await client.PostAsJsonAsync("/api/chat/stream", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
