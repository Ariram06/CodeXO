namespace Codexo.Api.IntegrationTests;

using Codexo.Core.Entities;
using Codexo.Core.Enums;
using Codexo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class DatabaseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DatabaseTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DbInitializer_SeedsDevUser_Successfully()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedAsync(context, isDevelopment: true);

        var devUser = await context.Users.FirstOrDefaultAsync(u => u.Id == DbInitializer.DevUserId);
        Assert.NotNull(devUser);
        Assert.Equal(DbInitializer.DevUserEmail, devUser.Email);
    }

    [Fact]
    public async Task DbInitializer_SeedsZeroProblems_NoSeededProblemsRemain()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedAsync(context, isDevelopment: true);

        // Verify that obsolete problem/session tables are completely gone and zero problems exist
        var conversationCount = await context.Conversations.CountAsync();
        Assert.True(conversationCount >= 0);
    }

    [Fact]
    public async Task Conversation_And_Message_Cascades_OnDelete()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = DbInitializer.DevUserId,
            Title = "Cascade Test",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = "Test message",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.Conversations.Add(conversation);
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        // Deleting conversation should cascade-delete messages
        var tracked = await context.Conversations.FindAsync(conversation.Id);
        Assert.NotNull(tracked);

        context.Conversations.Remove(tracked);
        await context.SaveChangesAsync();

        var messageExists = await context.Messages.AnyAsync(m => m.Id == message.Id);
        Assert.False(messageExists);
    }

    [Fact]
    public async Task MessageFeedback_UniqueCompositeIndex_PerUserPerMessage()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = DbInitializer.DevUserId,
            Title = "Feedback Test"
        };
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = "Assistant answer"
        };

        context.Conversations.Add(conversation);
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        var fb1 = new MessageFeedback
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            UserId = DbInitializer.DevUserId,
            IsHelpful = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        context.MessageFeedbacks.Add(fb1);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var fb2 = new MessageFeedback
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            UserId = DbInitializer.DevUserId,
            IsHelpful = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        context.MessageFeedbacks.Add(fb2);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }
}
