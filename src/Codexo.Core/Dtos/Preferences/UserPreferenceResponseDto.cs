namespace Codexo.Core.Dtos.Preferences;

public record UserPreferenceResponseDto(
    Guid Id,
    string UserId,
    string PreferredLanguage,
    bool AutoUnlockL1,
    DateTimeOffset UpdatedAt,
    string ReasoningLevel = "low",
    string? Model = null);
