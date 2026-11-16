namespace Codexo.Infrastructure.Data;

using Codexo.Core.Entities;
using Codexo.Core.Enums;
using Codexo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Development-only database initialization and seeding.
/// In production, migrations are applied via deployment pipelines and seeding is disabled.
/// </summary>
public static class DbInitializer
{
    public const string DevUserId = "00000000-0000-0000-0000-000000000001";
    public const string DevUserEmail = "dev@codexo.dev";

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

        // 1. Seed or ensure Development Identity User exists
        if (!await context.Users.AnyAsync(u => u.Id == DevUserId, cancellationToken))
        {
            var hasher = new PasswordHasher<AppUser>();
            var devUser = new AppUser
            {
                Id = DevUserId,
                UserName = DevUserEmail,
                NormalizedUserName = DevUserEmail.ToUpperInvariant(),
                Email = DevUserEmail,
                NormalizedEmail = DevUserEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("D"),
                ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                CreatedAtUtc = DateTime.UtcNow
            };
            devUser.PasswordHash = hasher.HashPassword(devUser, "DevPassword123!");
            await context.Users.AddAsync(devUser, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Clean up legacy placeholder from M2 if present
        var legacyPref = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == "seed-user-1", cancellationToken);
        if (legacyPref != null)
        {
            context.UserPreferences.Remove(legacyPref);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 2. Seed Problems if table is empty
        if (!await context.Problems.AnyAsync(cancellationToken))
        {
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
                    Description = "Given two strings s and t, return true if t is an anagram of s, and false otherwise.",
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
            await context.SaveChangesAsync(cancellationToken);
        }

        // 3. Seed dev user preferences
        if (!await context.UserPreferences.AnyAsync(p => p.UserId == DevUserId, cancellationToken))
        {
            var seedPreference = new UserPreference
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                UserId = DevUserId,
                PreferredLanguage = "C#",
                AutoUnlockL1 = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await context.UserPreferences.AddAsync(seedPreference, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
