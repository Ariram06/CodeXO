namespace Codexo.Core.Hints;

using Codexo.Core.Enums;

/// <summary>
/// Server-authoritative engine governing progressive disclosure of DSA hints.
/// Enforces strict sequential unlock, boundary limits, and attempt requirements.
/// </summary>
public static class HintLadder
{
    public const HintLevel MaxLevel = HintLevel.L5_Deep;
    public const HintLevel InitialLevel = HintLevel.L1_Understand;

    /// <summary>
    /// Determines whether the given level requires a user attempt before unlocking.
    /// L1 (Understand) is unlocked without an attempt. L2 through L5 require a validated attempt.
    /// </summary>
    public static bool RequiresAttempt(HintLevel targetLevel)
    {
        return targetLevel > HintLevel.L1_Understand;
    }

    /// <summary>
    /// Calculates the next sequential level from the current level.
    /// If current is None, returns L1.
    /// If current is L5, returns L5 (boundary limit).
    /// </summary>
    public static HintLevel GetNextLevel(HintLevel currentLevel)
    {
        return currentLevel switch
        {
            HintLevel.None => HintLevel.L1_Understand,
            HintLevel.L1_Understand => HintLevel.L2_Direction,
            HintLevel.L2_Direction => HintLevel.L3_Concepts,
            HintLevel.L3_Concepts => HintLevel.L4_Steps,
            HintLevel.L4_Steps => HintLevel.L5_Deep,
            HintLevel.L5_Deep => HintLevel.L5_Deep,
            _ => throw new ArgumentOutOfRangeException(nameof(currentLevel), $"Unexpected hint level: {currentLevel}")
        };
    }

    /// <summary>
    /// Validates whether a requested transition from currentLevel to requestedLevel is legal.
    /// Prevents skipping levels, moving backwards, or exceeding L5.
    /// </summary>
    public static bool IsValidProgression(HintLevel currentLevel, HintLevel requestedLevel)
    {
        if (requestedLevel <= HintLevel.None || (int)requestedLevel > (int)MaxLevel)
        {
            return false;
        }

        // Requesting the current level again is idempotent (allowed to re-read)
        if (requestedLevel == currentLevel)
        {
            return true;
        }

        // Advancing must be strictly next sequential level (+1)
        return (int)requestedLevel == (int)currentLevel + 1;
    }

    /// <summary>
    /// Determines the authorized target level for a session request.
    /// Server is authoritative: clients cannot specify or force arbitrary levels.
    /// </summary>
    /// <param name="currentLevel">Current level recorded in database for this session.</param>
    /// <param name="hasApprovedAttemptForNext">Whether the user has submitted an approved attempt for the next level.</param>
    /// <returns>The authorized HintLevel.</returns>
    public static HintLevel DetermineAuthorizedLevel(HintLevel currentLevel, bool hasApprovedAttemptForNext)
    {
        if (currentLevel == HintLevel.None)
        {
            return HintLevel.L1_Understand;
        }

        if (currentLevel >= MaxLevel)
        {
            return MaxLevel;
        }

        var nextLevel = GetNextLevel(currentLevel);
        if (RequiresAttempt(nextLevel) && !hasApprovedAttemptForNext)
        {
            // User has not satisfied attempt requirement to advance; remain at current level
            return currentLevel;
        }

        return nextLevel;
    }
}
