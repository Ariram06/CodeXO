namespace Codexo.Core.Interfaces;

using Codexo.Core.DTOs.Problems;
using Codexo.Core.Enums;

/// <summary>
/// Domain contract for managing problem intake and user study sessions.
/// Enforces per-user session isolation.
/// </summary>
public interface ISessionService
{
    Task<ProblemSessionResponse> GetOrCreateSessionAsync(string userId, ProblemIntakeRequest request, CancellationToken cancellationToken = default);

    Task<ProblemSessionResponse?> GetSessionByIdAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default);

    Task<ProblemSessionResponse?> GetActiveSessionForProblemAsync(string userId, Platform platform, string platformProblemId, CancellationToken cancellationToken = default);
}
