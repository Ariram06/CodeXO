namespace Codexo.Infrastructure.Data;

using Codexo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Development-only database initialization.
/// In production, migrations are applied via deployment pipelines.
/// Seeded problems (Two Sum etc.) are removed per Chat Pivot rules.
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

        // Seed or ensure Development Identity User exists
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
    }
}
