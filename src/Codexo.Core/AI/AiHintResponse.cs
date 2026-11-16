namespace Codexo.Core.AI;

using Codexo.Core.Enums;

/// <summary>
/// Fixed JSON response schema from the AI hint generation engine.
/// Unused fields are guaranteed to be non-null empty arrays.
/// </summary>
public record AiHintResponse(
    HintLevel Level,
    string Hint,
    string[] GuidingQuestions,
    string[] Concepts,
    string[] Steps,
    string[] Pitfalls);
