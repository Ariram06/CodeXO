namespace Codexo.Infrastructure.Data;

using Codexo.Core.Entities;
using Codexo.Core.Enums;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Development-only database initialization and seeding.
/// In production, migrations are applied via deployment pipelines and seeding is disabled.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Placeholder User ID used strictly for local development and integration tests.
    /// In M3 (Authentication), this will be replaced by ASP.NET Core Identity users.
    /// </summary>
    public const string DevPlaceholderUserId = "seed-user-1";

    public static async Task SeedAsync(AppDbContext context, bool isDevelopment = true, CancellationToken cancellationToken = default)
    {
        // Safety guard: DbInitializer must ONLY run in Development
        if (!isDevelopment)
        {
            return;
        }

        // Apply pending migrations automatically on local dev startup
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        // Idempotent: Only seed if database is empty
        if (await context.Problems.AnyAsync(cancellationToken))
        {
            return;
        }

        var problems = new List<Problem>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Platform = Platform.LeetCode,
                PlatformProblemId = "1",
                Title = "Two Sum",
                Slug = "two-sum",
                Url = "https://leetcode.com/problems/two-sum/",
                Difficulty = "Easy",
                Description = "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target. You may assume that each input would have exactly one solution, and you may not use the same element twice. You can return the answer in any order.",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Platform = Platform.LeetCode,
                PlatformProblemId = "242",
                Title = "Valid Anagram",
                Slug = "valid-anagram",
                Url = "https://leetcode.com/problems/valid-anagram/",
                Difficulty = "Easy",
                Description = "Given two strings s and t, return true if t is an anagram of s, and false otherwise. An Anagram is a word or phrase formed by rearranging the letters of a different word or phrase, typically using all the original letters exactly once.",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Platform = Platform.LeetCode,
                PlatformProblemId = "21",
                Title = "Merge Two Sorted Lists",
                Slug = "merge-two-sorted-lists",
                Url = "https://leetcode.com/problems/merge-two-sorted-lists/",
                Difficulty = "Easy",
                Description = "You are given the heads of two sorted linked lists list1 and list2. Merge the two lists into one sorted list. The list should be made by splicing together the nodes of the first two lists. Return the head of the merged linked list.",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        await context.Problems.AddRangeAsync(problems, cancellationToken);

        // DEV-ONLY placeholder user preference
        var seedPreference = new UserPreference
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            UserId = DevPlaceholderUserId,
            PreferredLanguage = "C#",
            AutoUnlockL1 = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await context.UserPreferences.AddAsync(seedPreference, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
