namespace Codexo.Core.DTOs.Problems;

using Codexo.Core.Enums;

public record ProblemSessionResponse(
    Guid SessionId,
    string UserId,
    ProblemDto Problem,
    HintLevel CurrentLevel,
    SessionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActivityAt);
