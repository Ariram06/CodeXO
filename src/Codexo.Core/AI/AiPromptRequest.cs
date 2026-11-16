namespace Codexo.Core.AI;

using Codexo.Core.Enums;

/// <summary>
/// Structured input for constructing AI hint generation prompts.
/// </summary>
public record AiPromptRequest(
    HintLevel Level,
    string ProblemTitle,
    string ProblemDescription,
    string UserAttempt,
    IReadOnlyList<string> PreviousHints);
