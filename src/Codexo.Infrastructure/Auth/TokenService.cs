namespace Codexo.Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Codexo.Core.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Issues cryptographically signed JSON Web Tokens (JWT) for authenticated users.
/// Signing key is retrieved securely via configuration (User Secrets / Key Vault).
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string userId, string email)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured or is shorter than 32 characters (256 bits). " +
                "Set it via .NET User Secrets: dotnet user-secrets set \"Jwt:Key\" \"<your-secret-key>\"");
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "CodexoApi";
        var audience = _configuration["Jwt:Audience"] ?? "CodexoExtension";
        var expiryMinutesStr = _configuration["Jwt:ExpiryMinutes"];
        var expiryMinutes = int.TryParse(expiryMinutesStr, out var exp) ? exp : 60;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}
