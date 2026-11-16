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
    public async Task DbInitializer_SeedsBenchmarkProblems_Successfully()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedAsync(context, isDevelopment: true);

        var problems = await context.Problems.OrderBy(p => p.Title).ToListAsync();

        Assert.True(problems.Count >= 3);
        Assert.Contains(problems, p => p.Slug == "two-sum" && p.PlatformProblemId == "1" && p.Platform == Platform.LeetCode);
        Assert.Contains(problems, p => p.Slug == "valid-anagram" && p.PlatformProblemId == "242");
        Assert.Contains(problems, p => p.Slug == "merge-two-sorted-lists" && p.PlatformProblemId == "21");
    }

    [Fact]
    public async Task DbInitializer_SeedsUserPreference_Successfully()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedAsync(context, isDevelopment: true);

        var pref = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == DbInitializer.DevUserId);

        Assert.NotNull(pref);
        Assert.Equal("C#", pref.PreferredLanguage);
        Assert.True(pref.AutoUnlockL1);
    }

    [Fact]
    public async Task Problem_UniqueCompositeIndex_PreventsDuplicatePlatformProblemId()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedAsync(context, isDevelopment: true);

        // Attempt to insert duplicate (Platform.LeetCode, "1")
        var duplicateProblem = new Problem
        {
            Id = Guid.NewGuid(),
            Platform = Platform.LeetCode,
            PlatformProblemId = "1",
            Title = "Duplicate Two Sum",
            Slug = "duplicate-two-sum",
            Url = "https://leetcode.com/problems/two-sum-dup",
            Difficulty = "Easy",
            Description = "Duplicate test"
        };

        context.Problems.Add(duplicateProblem);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Problem_Delete_WithExistingSessions_ThrowsDbUpdateException_DueToRestrict()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedAsync(context, isDevelopment: true);

        // Create a standalone problem and a linked session
        var uniqueId = Guid.NewGuid().ToString("N");
        var testProblem = new Problem
        {
            Id = Guid.NewGuid(),
            Platform = Platform.LeetCode,
            PlatformProblemId = $"restrict-{uniqueId}",
            Title = $"Restrict Test Problem {uniqueId}",
            Slug = $"restrict-test-{uniqueId}",
            Url = $"https://leetcode.com/problems/restrict-test-{uniqueId}",
            Difficulty = "Medium",
            Description = "Testing restrict delete behavior."
        };

        var linkedSession = new ProblemSession
        {
            Id = Guid.NewGuid(),
            UserId = DbInitializer.DevUserId,
            ProblemId = testProblem.Id,
            CurrentLevel = HintLevel.L1_Understand,
            Status = SessionStatus.Active
        };

        context.Problems.Add(testProblem);
        context.ProblemSessions.Add(linkedSession);
        await context.SaveChangesAsync();

        // Detach tracked entities so EF Core delegates foreign key check to the database
        context.ChangeTracker.Clear();

        var trackedProblem = await context.Problems.FindAsync(testProblem.Id);
        Assert.NotNull(trackedProblem);

        // Attempting to delete the Problem while ProblemSession references it MUST fail in database (Restrict / NO ACTION)
        context.Problems.Remove(trackedProblem);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });

        // Cleanup: remove session first, then problem succeeds
        context.ChangeTracker.Clear();
        var sessionToDelete = await context.ProblemSessions.FindAsync(linkedSession.Id);
        if (sessionToDelete != null)
        {
            context.ProblemSessions.Remove(sessionToDelete);
            await context.SaveChangesAsync();
        }

        var problemToDelete = await context.Problems.FindAsync(testProblem.Id);
        if (problemToDelete != null)
        {
            context.Problems.Remove(problemToDelete);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task ProblemSession_CascadeDeletes_HintsAndAttempts()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedAsync(context, isDevelopment: true);

        var problem = await context.Problems.FirstAsync();
        var session = new ProblemSession
        {
            Id = Guid.NewGuid(),
            UserId = DbInitializer.DevUserId,
            ProblemId = problem.Id,
            CurrentLevel = HintLevel.L1_Understand,
            Status = SessionStatus.Active
        };

        var hint = new Hint
        {
            Id = Guid.NewGuid(),
            ProblemSessionId = session.Id,
            Level = HintLevel.L1_Understand,
            Content = "Cascade test hint content",
            Explanation = "Cascade test explanation",
            IsFallback = false,
            GenerationAttempts = 1
        };

        var attempt = new UserAttempt
        {
            Id = Guid.NewGuid(),
            ProblemSessionId = session.Id,
            TargetLevel = HintLevel.L2_Direction,
            AttemptText = "Cascade test user attempt description notes",
            Status = AttemptStatus.Approved
        };

        context.ProblemSessions.Add(session);
        context.Hints.Add(hint);
        context.UserAttempts.Add(attempt);
        await context.SaveChangesAsync();

        // Delete session and verify hints and attempts cascade delete
        context.ProblemSessions.Remove(session);
        await context.SaveChangesAsync();

        var remainingHint = await context.Hints.FirstOrDefaultAsync(h => h.Id == hint.Id);
        var remainingAttempt = await context.UserAttempts.FirstOrDefaultAsync(a => a.Id == attempt.Id);

        Assert.Null(remainingHint);
        Assert.Null(remainingAttempt);
    }
}
