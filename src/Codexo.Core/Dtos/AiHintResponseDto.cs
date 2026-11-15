namespace Codexo.Core.Dtos;

using Codexo.Core.Enums;

public record AiHintResponseDto
{
    public HintLevel HintLevel { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
}

