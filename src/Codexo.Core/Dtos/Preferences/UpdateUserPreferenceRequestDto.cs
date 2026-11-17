namespace Codexo.Core.Dtos.Preferences;

public record UpdateUserPreferenceRequestDto(
    string? PreferredLanguage,
    bool? AutoUnlockL1,
    string? ReasoningLevel = null);
