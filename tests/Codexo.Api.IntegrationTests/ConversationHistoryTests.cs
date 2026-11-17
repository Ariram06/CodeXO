namespace Codexo.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.DTOs.Auth;
using Codexo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ConversationHistoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConversationHistoryTests(WebApplicationFactory<Program> factory)
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
    public async Task GetConversations_ReturnsOrderedList_ForAuthenticatedUser()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();

        // Send two messages creating two distinct conversations
        var req1 = new SendMessageRequest(
            ConversationId: null,
            Message: "First conversation query",
            Context: new PageContext("LeetCode", "https://leetcode.com/1", "Two Sum", null, "python", null, null, null, false));
        var resp1 = await client.PostAsJsonAsync("/api/chat/messages", req1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        var msg1 = await resp1.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(msg1);

        // Wait a tiny moment to ensure timestamp distinction
        await Task.Delay(50);

        var req2 = new SendMessageRequest(
            ConversationId: null,
            Message: "Second conversation query",
            Context: new PageContext("LeetCode", "https://leetcode.com/2", "Add Two Numbers", null, "python", null, null, null, false));
        var resp2 = await client.PostAsJsonAsync("/api/chat/messages", req2);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        var msg2 = await resp2.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(msg2);

        // Fetch conversations
        var listResp = await client.GetAsync("/api/chat/conversations");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);

        var list = await listResp.Content.ReadFromJsonAsync<List<ConversationSummaryDto>>();
        Assert.NotNull(list);
        Assert.True(list.Count >= 2);

        // Verify ordering: newest first
        var conv2 = list.First(c => c.Id == msg2.ConversationId);
        var conv1 = list.First(c => c.Id == msg1.ConversationId);
        Assert.True(conv2.UpdatedAtUtc >= conv1.UpdatedAtUtc);
        Assert.Equal(2, conv1.MessageCount); // User + Assistant
        Assert.Equal(2, conv2.MessageCount);
    }

    [Fact]
    public async Task GetConversationById_ReturnsFullDetail_AndMessages()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var req = new SendMessageRequest(
            ConversationId: null,
            Message: "Explain binary search",
            Context: new PageContext("LeetCode", "https://leetcode.com/704", "Binary Search", null, "python", null, null, null, false));
        var resp = await client.PostAsJsonAsync("/api/chat/messages", req);
        var msg = await resp.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(msg);

        // Fetch conversation detail
        var detailResp = await client.GetAsync($"/api/chat/conversations/{msg.ConversationId}");
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);

        var detail = await detailResp.Content.ReadFromJsonAsync<ConversationDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(msg.ConversationId, detail.Id);
        Assert.Equal(2, detail.Messages.Count);
        Assert.Equal("user", detail.Messages[0].Role);
        Assert.Equal("Explain binary search", detail.Messages[0].Content);
        Assert.Equal("assistant", detail.Messages[1].Role);
    }

    [Fact]
    public async Task GetConversationById_BelongingToOtherUser_Returns404()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync();
        var (clientB, _) = await CreateAuthenticatedClientAsync();

        var req = new SendMessageRequest(
            ConversationId: null,
            Message: "User A private query",
            Context: null);
        var respA = await clientA.PostAsJsonAsync("/api/chat/messages", req);
        var msgA = await respA.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(msgA);

        // User B attempts to access User A's conversation
        var respB = await clientB.GetAsync($"/api/chat/conversations/{msgA.ConversationId}");
        Assert.Equal(HttpStatusCode.NotFound, respB.StatusCode);
    }

    [Fact]
    public async Task DeleteConversation_CascadesDeletesMessages()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var req = new SendMessageRequest(
            ConversationId: null,
            Message: "To be deleted",
            Context: null);
        var resp = await client.PostAsJsonAsync("/api/chat/messages", req);
        var msg = await resp.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(msg);

        // Delete conversation
        var delResp = await client.DeleteAsync($"/api/chat/conversations/{msg.ConversationId}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // Verify conversation is gone
        var getResp = await client.GetAsync($"/api/chat/conversations/{msg.ConversationId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);

        // Verify messages in DB were cascade-deleted
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbMessages = await dbContext.Messages.Where(m => m.ConversationId == msg.ConversationId).ToListAsync();
        Assert.Empty(dbMessages);
    }

    [Fact]
    public async Task ClearConversations_RemovesAllUserConversations_WithoutTouchingOtherUsers()
    {
        var (clientA, userIdA) = await CreateAuthenticatedClientAsync();
        var (clientB, userIdB) = await CreateAuthenticatedClientAsync();

        // User A creates conversation
        var reqA = new SendMessageRequest(ConversationId: null, Message: "User A chat", Context: null);
        var respA = await clientA.PostAsJsonAsync("/api/chat/messages", reqA);
        var msgA = await respA.Content.ReadFromJsonAsync<ChatMessageResponse>();

        // User B creates conversation
        var reqB = new SendMessageRequest(ConversationId: null, Message: "User B chat", Context: null);
        var respB = await clientB.PostAsJsonAsync("/api/chat/messages", reqB);
        var msgB = await respB.Content.ReadFromJsonAsync<ChatMessageResponse>();

        // User A clears all conversations
        var clearResp = await clientA.DeleteAsync("/api/chat/conversations");
        Assert.Equal(HttpStatusCode.NoContent, clearResp.StatusCode);

        // User A's conversations list is now empty
        var listA = await clientA.GetFromJsonAsync<List<ConversationSummaryDto>>("/api/chat/conversations");
        Assert.NotNull(listA);
        Assert.Empty(listA);

        // User B's conversation still exists!
        var listB = await clientB.GetFromJsonAsync<List<ConversationSummaryDto>>("/api/chat/conversations");
        Assert.NotNull(listB);
        Assert.Single(listB);
        Assert.Equal(msgB!.ConversationId, listB[0].Id);
    }
}
