namespace Codexo.Core.Auth;

/// <summary>
/// Service responsible for issuing cryptographically signed JWT tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    string GenerateToken(string userId, string email);
}
