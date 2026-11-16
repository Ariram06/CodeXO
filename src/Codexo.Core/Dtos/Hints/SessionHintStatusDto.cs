namespace Codexo.Core.Dtos.Hints;

using Codexo.Core.Enums;

public record SessionHintStatusDto(
    Guid SessionId,
    HintLevel CurrentLevel,
    bool CanRequestNextHint,
    HintLevel? NextLevel,
    bool AttemptRequiredForNextLevel,
    string? Reason);
