namespace Codexo.Infrastructure.Services;

using Codexo.Core.DTOs.Problems;
using Codexo.Core.Entities;
using Codexo.Core.Enums;
using Codexo.Core.Interfaces;
using Codexo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Manages problem intake, deduplication, and user study sessions.
/// Strictly enforces per-user isolation so one user can never view another user's session.
/// </summary>
public class SessionService : ISessionService
{
    private readonly AppDbContext _context;

    public SessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProblemSessionResponse> GetOrCreateSessionAsync(
        string userId,
        ProblemIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(request);

        var trimmedProblemId = request.PlatformProblemId.Trim();
        var trimmedTitle = request.Title.Trim();
        var trimmedSlug = request.Slug.Trim();
        var trimmedUrl = request.Url.Trim();
        var trimmedDescription = request.Description.Trim();
        var trimmedDifficulty = string.IsNullOrWhiteSpace(request.Difficulty)
            ? "Unknown"
            : request.Difficulty.Trim();

        // 1. Get or create problem definition
        var problem = await _context.Problems
            .FirstOrDefaultAsync(p => p.Platform == request.Platform && p.PlatformProblemId == trimmedProblemId, cancellationToken);

        if (problem == null)
        {
            problem = new Problem
            {
                Id = Guid.NewGuid(),
                Platform = request.Platform,
                PlatformProblemId = trimmedProblemId,
                Title = trimmedTitle,
                Slug = trimmedSlug,
                Url = trimmedUrl,
                Difficulty = trimmedDifficulty,
                Description = trimmedDescription,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Problems.Add(problem);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Update problem details if changed
            var hasChanged = false;
            if (problem.Title != trimmedTitle) { problem.Title = trimmedTitle; hasChanged = true; }
            if (problem.Slug != trimmedSlug) { problem.Slug = trimmedSlug; hasChanged = true; }
            if (problem.Url != trimmedUrl) { problem.Url = trimmedUrl; hasChanged = true; }
            if (problem.Description != trimmedDescription) { problem.Description = trimmedDescription; hasChanged = true; }
            if (!string.IsNullOrWhiteSpace(request.Difficulty) && problem.Difficulty != trimmedDifficulty)
            {
                problem.Difficulty = trimmedDifficulty;
                hasChanged = true;
            }

            if (hasChanged)
            {
                problem.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // 2. Look for active session belonging strictly to the requesting user
        var session = await _context.ProblemSessions
            .Include(s => s.Problem)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ProblemId == problem.Id && s.Status == SessionStatus.Active, cancellationToken);

        if (session == null)
        {
            session = new ProblemSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProblemId = problem.Id,
                Problem = problem,
                CurrentLevel = HintLevel.L1_Understand,
                Status = SessionStatus.Active,
                StartedAt = DateTimeOffset.UtcNow,
                LastActivityAt = DateTimeOffset.UtcNow
            };

            _context.ProblemSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            session.LastActivityAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapToResponse(session, problem);
    }

    public async Task<ProblemSessionResponse?> GetSessionByIdAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Strict per-user filter: never return a session belonging to another user
        var session = await _context.ProblemSessions
            .Include(s => s.Problem)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        return session == null ? null : MapToResponse(session, session.Problem!);
    }

    public async Task<ProblemSessionResponse?> GetActiveSessionForProblemAsync(
        string userId,
        Platform platform,
        string platformProblemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var trimmedProblemId = platformProblemId.Trim();

        var session = await _context.ProblemSessions
            .Include(s => s.Problem)
            .FirstOrDefaultAsync(s => s.UserId == userId
                && s.Status == SessionStatus.Active
                && s.Problem!.Platform == platform
                && s.Problem.PlatformProblemId == trimmedProblemId, cancellationToken);

        return session == null ? null : MapToResponse(session, session.Problem!);
    }

    private static ProblemSessionResponse MapToResponse(ProblemSession session, Problem problem)
    {
        return new ProblemSessionResponse(
            SessionId: session.Id,
            UserId: session.UserId,
            Problem: new ProblemDto(
                Id: problem.Id,
                Platform: problem.Platform,
                PlatformProblemId: problem.PlatformProblemId,
                Title: problem.Title,
                Slug: problem.Slug,
                Url: problem.Url,
                Description: problem.Description,
                Difficulty: problem.Difficulty),
            CurrentLevel: session.CurrentLevel,
            Status: session.Status,
            StartedAt: session.StartedAt,
            LastActivityAt: session.LastActivityAt);
    }
}
