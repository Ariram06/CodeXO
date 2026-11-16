namespace Codexo.Core.Interfaces;

using Codexo.Core.Dtos.Preferences;

public interface IPreferenceService
{
    Task<UserPreferenceResponseDto> GetPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserPreferenceResponseDto> UpsertPreferencesAsync(
        string userId,
        UpdateUserPreferenceRequestDto request,
        CancellationToken cancellationToken = default);
}
