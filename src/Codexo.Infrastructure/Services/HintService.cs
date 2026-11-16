namespace Codexo.Infrastructure.Services;

using System.Collections.Concurrent;
using System.Text.Json;
using Codexo.Core.AI;
using Codexo.Core.Dtos.Hints;
using Codexo.Core.Entities;
using Codexo.Core.Enums;
using Codexo.Core.Hints;
using Codexo.Core.Interfaces;
using Codexo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

public class HintService : IHintService
{
    private readonly AppDbContext _dbContext;
    private readonly IAiClient _aiClient;
    private readonly HybridCache _cache;
    private readonly ILogger<HintService> _logger;

    // Concurrency protection: Single-flight request coalescing ensures parallel requests for the same session
    // return the same generated hint rather than skipping levels or wasting LLM quota
    private static readonly ConcurrentDictionary<Guid, Lazy<Task<HintResponseDto>>> InFlightRequests = new();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> SessionLocks = new();

    public HintService(
        AppDbContext dbContext,
        IAiClient aiClient,
        HybridCache cache,
        ILogger<HintService> logger)
    {
        _dbContext = dbContext;
        _aiClient = aiClient;
        _cache = cache;
        _logger = logger;
    }

    public Task<HintResponseDto> RequestNextHintAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var lazyTask = InFlightRequests.GetOrAdd(
            sessionId,
            id => new Lazy<Task<HintResponseDto>>(() =>
            {
                var task = ExecuteRequestNextHintAsync(userId, id, cancellationToken);
                _ = task.ContinueWith(_ => InFlightRequests.TryRemove(id, out Lazy<Task<HintResponseDto>>? _), TaskScheduler.Default);
                return task;
            }));

        return lazyTask.Value;
    }

    private async Task<HintResponseDto> ExecuteRequestNextHintAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var sessionLock = SessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await sessionLock.WaitAsync(cancellationToken);

        try
        {
            var session = await _dbContext.ProblemSessions
                .Include(s => s.Problem)
                .Include(s => s.Hints)
                .Include(s => s.Attempts)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

            if (session is null)
            {
                throw new KeyNotFoundException($"Session {sessionId} was not found for the authenticated user.");
            }

            if (session.Status != SessionStatus.Active)
            {
                throw new InvalidOperationException("Hints cannot be requested for a completed or abandoned session.");
            }

            var nextLevel = GetNextHintLevelToDeliver(session);

            // Idempotency check: If hint for target level already exists, return it immediately
            var existingHint = session.Hints.FirstOrDefault(h => h.Level == nextLevel);
            if (existingHint != null)
            {
                return MapToDto(existingHint);
            }

            // Gating check: L2 through L5 require a validated attempt
            if (HintLadder.RequiresAttempt(nextLevel))
            {
                var hasAcceptedAttempt = session.Attempts
                    .Any(a => a.TargetLevel == nextLevel && a.Status == AttemptStatus.Approved);

                if (!hasAcceptedAttempt)
                {
                    throw new InvalidOperationException(
                        $"Level {nextLevel} is locked. A meaningful written attempt is required before unlocking this hint.");
                }
            }

            // Resolve hint via cache (L1/L2) or generation with SolutionGuard pipeline (L3..L5)
            AiHintResponse resolvedResponse;
            var generationAttempts = 1;
            var isFallback = false;

            if (nextLevel is HintLevel.L1_Understand or HintLevel.L2_Direction)
            {
                var cacheKey = $"hint:problem:{session.ProblemId}:level:{nextLevel}";
                resolvedResponse = await _cache.GetOrCreateAsync(
                    cacheKey,
                    async cancelToken =>
                    {
                        var (response, attempts, fallback) = await GenerateWithGuardAsync(
                            session, nextLevel, cancelToken);
                        generationAttempts = attempts;
                        isFallback = fallback;
                        return response;
                    },
                    cancellationToken: cancellationToken);
            }
            else
            {
                (resolvedResponse, generationAttempts, isFallback) = await GenerateWithGuardAsync(
                    session, nextLevel, cancellationToken);
            }

            // Persist hint
            var hintEntity = new Hint
            {
                Id = Guid.NewGuid(),
                ProblemSessionId = session.Id,
                Level = nextLevel,
                Content = resolvedResponse.Hint,
                Explanation = JsonSerializer.Serialize(resolvedResponse),
                IsFallback = isFallback,
                GenerationAttempts = generationAttempts,
                CreatedAt = DateTimeOffset.UtcNow
            };

            session.CurrentLevel = nextLevel;
            session.LastActivityAt = DateTimeOffset.UtcNow;
            _dbContext.Hints.Add(hintEntity);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Delivered hint {Level} for session {SessionId}. Attempts: {Attempts}, Fallback: {IsFallback}",
                nextLevel, session.Id, generationAttempts, isFallback);

            return MapToDto(hintEntity);
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public async Task<AttemptEvaluationResultDto> SubmitAttemptAsync(
        string userId,
        Guid sessionId,
        SubmitAttemptRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessionLock = SessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await sessionLock.WaitAsync(cancellationToken);

        try
        {
            var session = await _dbContext.ProblemSessions
                .Include(s => s.Attempts)
                .Include(s => s.Hints)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

            if (session is null)
            {
                throw new KeyNotFoundException($"Session {sessionId} was not found for the authenticated user.");
            }

            if (session.Status != SessionStatus.Active)
            {
                throw new InvalidOperationException("Attempts cannot be submitted for an inactive session.");
            }

            var targetLevel = GetNextHintLevelToDeliver(session);

            if (session.Hints.Any(h => h.Level == HintLevel.L5_Deep))
            {
                return new AttemptEvaluationResultDto(
                    AttemptId: Guid.Empty,
                    Unlocked: false,
                    TargetLevel: HintLevel.L5_Deep,
                    Status: AttemptStatus.Rejected,
                    RejectionReason: "MaxLevelReached",
                    FeedbackMessage: "You have already reached the maximum hint level (L5 Deep) for this problem.");
            }

            // Validate attempt via AttemptGatingValidator
            var validationResult = AttemptGatingValidator.Validate(request.AttemptText);

            var attemptEntity = new UserAttempt
            {
                Id = Guid.NewGuid(),
                ProblemSessionId = session.Id,
                TargetLevel = targetLevel,
                AttemptText = request.AttemptText.Trim(),
                Status = validationResult.IsApproved ? AttemptStatus.Approved : AttemptStatus.Rejected,
                RejectionReason = validationResult.IsApproved ? null : validationResult.RejectionReason,
                SubmittedAt = DateTimeOffset.UtcNow
            };

            _dbContext.UserAttempts.Add(attemptEntity);
            session.LastActivityAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new AttemptEvaluationResultDto(
                AttemptId: attemptEntity.Id,
                Unlocked: validationResult.IsApproved,
                TargetLevel: targetLevel,
                Status: attemptEntity.Status,
                RejectionReason: validationResult.IsApproved ? null : validationResult.RejectionReason.ToString(),
                FeedbackMessage: validationResult.FeedbackMessage);
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public async Task<IReadOnlyList<HintResponseDto>> GetHintsForSessionAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var sessionExists = await _dbContext.ProblemSessions
            .AnyAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (!sessionExists)
        {
            throw new KeyNotFoundException($"Session {sessionId} was not found for the authenticated user.");
        }

        var hints = await _dbContext.Hints
            .Where(h => h.ProblemSessionId == sessionId)
            .OrderBy(h => h.Level)
            .ToListAsync(cancellationToken);

        return hints.Select(MapToDto).ToList();
    }

    public async Task<SessionHintStatusDto> GetSessionHintStatusAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.ProblemSessions
            .Include(s => s.Attempts)
            .Include(s => s.Hints)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (session is null)
        {
            throw new KeyNotFoundException($"Session {sessionId} was not found for the authenticated user.");
        }

        var currentLevel = session.Hints.OrderBy(h => h.Level).LastOrDefault()?.Level ?? HintLevel.None;
        var isAtMax = session.Hints.Any(h => h.Level == HintLevel.L5_Deep);
        var nextLevel = isAtMax ? null : (HintLevel?)GetNextHintLevelToDeliver(session);

        if (isAtMax)
        {
            return new SessionHintStatusDto(
                SessionId: session.Id,
                CurrentLevel: currentLevel,
                CanRequestNextHint: false,
                NextLevel: null,
                AttemptRequiredForNextLevel: false,
                Reason: "Maximum hint level reached.");
        }

        var attemptRequired = nextLevel.HasValue && HintLadder.RequiresAttempt(nextLevel.Value);
        var hasValidAttempt = attemptRequired && session.Attempts
            .Any(a => a.TargetLevel == nextLevel!.Value && a.Status == AttemptStatus.Approved);

        var canRequest = !attemptRequired || hasValidAttempt;
        var reason = canRequest
            ? "Ready to request hint."
            : $"A valid attempt is required to unlock {nextLevel}.";

        return new SessionHintStatusDto(
            SessionId: session.Id,
            CurrentLevel: currentLevel,
            CanRequestNextHint: canRequest,
            NextLevel: nextLevel,
            AttemptRequiredForNextLevel: attemptRequired,
            Reason: reason);
    }

    private static HintLevel GetNextHintLevelToDeliver(ProblemSession session)
    {
        if (!session.Hints.Any(h => h.Level == HintLevel.L1_Understand))
        {
            return HintLevel.L1_Understand;
        }

        if (!session.Hints.Any(h => h.Level == HintLevel.L2_Direction))
        {
            return HintLevel.L2_Direction;
        }

        if (!session.Hints.Any(h => h.Level == HintLevel.L3_Concepts))
        {
            return HintLevel.L3_Concepts;
        }

        if (!session.Hints.Any(h => h.Level == HintLevel.L4_Steps))
        {
            return HintLevel.L4_Steps;
        }

        return HintLevel.L5_Deep;
    }

    private async Task<(AiHintResponse Response, int Attempts, bool IsFallback)> GenerateWithGuardAsync(
        ProblemSession session,
        HintLevel targetLevel,
        CancellationToken cancellationToken)
    {
        var previousHints = session.Hints
            .OrderBy(h => h.Level)
            .Select(h => h.Content)
            .ToArray();

        var latestAttempt = session.Attempts
            .Where(a => a.Status == AttemptStatus.Approved)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefault()?.AttemptText ?? string.Empty;

        var promptRequest = new AiPromptRequest(
            Level: targetLevel,
            ProblemTitle: session.Problem?.Title ?? "DSA Problem",
            ProblemDescription: session.Problem?.Description ?? string.Empty,
            UserAttempt: latestAttempt,
            PreviousHints: previousHints);

        // Attempt 1: Standard generation
        try
        {
            var response1 = await _aiClient.GenerateHintAsync(promptRequest, cancellationToken);
            var guardResult1 = SolutionGuard.Inspect(targetLevel, response1.Hint);

            if (guardResult1.IsValid)
            {
                return (response1, Attempts: 1, IsFallback: false);
            }

            _logger.LogWarning(
                "SolutionGuard failed on Attempt 1 for level {Level}: {Violations}. Retrying with constrained prompt.",
                targetLevel, string.Join(", ", guardResult1.Details));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI client failed on Attempt 1 for level {Level}.", targetLevel);
        }

        // Attempt 2: Constrained regeneration with strict warning
        try
        {
            var constrainedDescription = (session.Problem?.Description ?? string.Empty) +
                "\n\n[CRITICAL PEDAGOGICAL REQUIREMENT: The previous hint attempt contained code, spoilers, or improper formatting. " +
                "You must strictly NOT provide any code, data structure names, or algorithm solutions. " +
                "Output pure conceptual guidance and guiding questions strictly adhering to level constraints.]";

            var constrainedRequest = promptRequest with { ProblemDescription = constrainedDescription };

            var response2 = await _aiClient.GenerateHintAsync(constrainedRequest, cancellationToken);
            var guardResult2 = SolutionGuard.Inspect(targetLevel, response2.Hint);

            if (guardResult2.IsValid)
            {
                return (response2, Attempts: 2, IsFallback: false);
            }

            _logger.LogWarning(
                "SolutionGuard failed on Attempt 2 for level {Level}: {Violations}. Falling back to safe static hint.",
                targetLevel, string.Join(", ", guardResult2.Details));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI client failed on Attempt 2 for level {Level}.", targetLevel);
        }

        // Attempt 3: Guaranteed-safe static fallback
        var fallback = SafeFallbackHints.GetFallback(targetLevel, session.Problem?.Title);
        return (fallback, Attempts: 2, IsFallback: true);
    }

    private static HintResponseDto MapToDto(Hint hint)
    {
        AiHintResponse? parsedEnvelope = null;
        if (!string.IsNullOrWhiteSpace(hint.Explanation))
        {
            try
            {
                parsedEnvelope = JsonSerializer.Deserialize<AiHintResponse>(hint.Explanation);
            }
            catch
            {
                // Fall back gracefully if explanation is not JSON
            }
        }

        return new HintResponseDto(
            Id: hint.Id,
            SessionId: hint.ProblemSessionId,
            Level: hint.Level,
            Hint: hint.Content,
            GuidingQuestions: parsedEnvelope?.GuidingQuestions ?? Array.Empty<string>(),
            Concepts: parsedEnvelope?.Concepts ?? Array.Empty<string>(),
            Steps: parsedEnvelope?.Steps ?? Array.Empty<string>(),
            Pitfalls: parsedEnvelope?.Pitfalls ?? Array.Empty<string>(),
            IsFallback: hint.IsFallback,
            CreatedAt: hint.CreatedAt);
    }
}
