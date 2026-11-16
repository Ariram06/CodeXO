namespace Codexo.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

/// <summary>
/// Custom application user entity extending ASP.NET Core Identity.
/// </summary>
public class AppUser : IdentityUser
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
