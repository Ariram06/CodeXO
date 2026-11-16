namespace Codexo.Core.Dtos.Chat;

public record PageContext(
    string Platform,
    string Url,
    string Title,
    string? ProblemText,
    string? Language,
    string? Code,
    string? Selection,
    string? ErrorText,
    bool IsTruncated);
