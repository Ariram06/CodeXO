namespace Codexo.Core.DTOs.Problems;

using Codexo.Core.Enums;

public record ProblemDto(
    Guid Id,
    Platform Platform,
    string PlatformProblemId,
    string Title,
    string Slug,
    string Url,
    string Description,
    string Difficulty);
