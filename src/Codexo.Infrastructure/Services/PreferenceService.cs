namespace Codexo.Infrastructure.Services;

using Codexo.Core.Dtos.Preferences;
using Codexo.Core.Entities;
using Codexo.Core.Interfaces;
using Codexo.Infrastructure.AI;
using Codexo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class PreferenceService : IPreferenceService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PreferenceService> _logger;
    private readonly AISettings? _aiSettings;

    public PreferenceService(
        AppDbContext dbContext,
        ILogger<PreferenceService> logger,
        IOptions<AISettings>? aiSettings = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _aiSettings = aiSettings?.Value;
    }

    public async Task<UserPreferenceResponseDto> GetPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var pref = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (pref != null)
        {
            return MapToDto(pref);
        }

        // Return or create default preferences for the user
        var defaultPref = new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PreferredLanguage = "C#",
            AutoUnlockL1 = true,
            ReasoningLevel = "low",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.UserPreferences.Add(defaultPref);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created default preferences for user {UserId}", userId);
        return MapToDto(defaultPref);
    }

    public async Task<UserPreferenceResponseDto> UpsertPreferencesAsync(
        string userId,
        UpdateUserPreferenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(request);

        var trimmedLang = string.IsNullOrWhiteSpace(request.PreferredLanguage)
            ? "C#"
            : request.PreferredLanguage.Trim();

        var reasoningLevel = !string.IsNullOrWhiteSpace(request.ReasoningLevel)
            ? request.ReasoningLevel.Trim().ToLowerInvariant()
            : null;

        var pref = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (pref != null)
        {
            pref.PreferredLanguage = trimmedLang;
            if (request.AutoUnlockL1.HasValue)
            {
                pref.AutoUnlockL1 = request.AutoUnlockL1.Value;
            }
            if (reasoningLevel != null)
            {
                pref.ReasoningLevel = reasoningLevel;
            }
            pref.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated preferences for user {UserId}: Lang={Lang}, AutoUnlockL1={AutoUnlock}, Reasoning={Reasoning}", userId, trimmedLang, pref.AutoUnlockL1, pref.ReasoningLevel);

            return MapToDto(pref);
        }

        var newPref = new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PreferredLanguage = trimmedLang,
            AutoUnlockL1 = request.AutoUnlockL1 ?? true,
            ReasoningLevel = reasoningLevel ?? "low",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.UserPreferences.Add(newPref);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created preferences for user {UserId}: Lang={Lang}, AutoUnlockL1={AutoUnlock}, Reasoning={Reasoning}", userId, trimmedLang, request.AutoUnlockL1, newPref.ReasoningLevel);

        return MapToDto(newPref);
    }

    private UserPreferenceResponseDto MapToDto(UserPreference pref) =>
        new(
            Id: pref.Id,
            UserId: pref.UserId,
            PreferredLanguage: pref.PreferredLanguage,
            AutoUnlockL1: pref.AutoUnlockL1,
            UpdatedAt: pref.UpdatedAt,
            ReasoningLevel: pref.ReasoningLevel ?? "low",
            Model: _aiSettings?.Model ?? "gemini-2.5-flash");
}
