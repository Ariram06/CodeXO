namespace Codexo.Core.DTOs.Auth;

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string UserId,
    string Email);
