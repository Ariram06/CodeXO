namespace Codexo.Infrastructure.Services;

using Codexo.Core.Dtos.Preferences;
using Codexo.Core.Entities;
using Codexo.Core.Interfaces;
using Codexo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class PreferenceService : IPreferenceService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PreferenceService> _logger;

    public PreferenceService(AppDbContext dbContext, ILogger<PreferenceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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

        var pref = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (pref != null)
        {
            pref.PreferredLanguage = trimmedLang;
            pref.AutoUnlockL1 = request.AutoUnlockL1;
            pref.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated preferences for user {UserId}: Lang={Lang}, AutoUnlockL1={AutoUnlock}", userId, trimmedLang, request.AutoUnlockL1);

            return MapToDto(pref);
        }

        var newPref = new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PreferredLanguage = trimmedLang,
            AutoUnlockL1 = request.AutoUnlockL1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.UserPreferences.Add(newPref);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created preferences for user {UserId}: Lang={Lang}, AutoUnlockL1={AutoUnlock}", userId, trimmedLang, request.AutoUnlockL1);

        return MapToDto(newPref);
    }

    private static UserPreferenceResponseDto MapToDto(UserPreference pref) =>
        new(
            Id: pref.Id,
            UserId: pref.UserId,
            PreferredLanguage: pref.PreferredLanguage,
            AutoUnlockL1: pref.AutoUnlockL1,
            UpdatedAt: pref.UpdatedAt);
}
