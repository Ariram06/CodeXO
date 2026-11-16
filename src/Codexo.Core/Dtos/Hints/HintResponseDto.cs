namespace Codexo.Core.Dtos.Hints;

using Codexo.Core.Enums;

public record HintResponseDto(
    Guid Id,
    Guid SessionId,
    HintLevel Level,
    string Hint,
    IReadOnlyList<string> GuidingQuestions,
    IReadOnlyList<string> Concepts,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Pitfalls,
    bool IsFallback,
    DateTimeOffset CreatedAt);
