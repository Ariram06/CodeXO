namespace Codexo.Core.DTOs.Problems;

using Codexo.Core.Enums;

public record ProblemIntakeRequest(
    Platform Platform,
    string PlatformProblemId,
    string Title,
    string Slug,
    string Url,
    string Description,
    string? Difficulty);
