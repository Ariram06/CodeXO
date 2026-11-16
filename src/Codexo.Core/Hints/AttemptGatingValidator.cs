namespace Codexo.Core.Hints;

using System.Text.RegularExpressions;
using Codexo.Core.Enums;

/// <summary>
/// Validates user-submitted problem solving attempts before granting progressive hint unlocks.
/// Rejects empty, trivial, keysmash, or solution-soliciting submissions.
/// </summary>
public static class AttemptGatingValidator
{
    private const int MinCharacterLength = 15;
    private const int MinWordCount = 3;

    private static readonly HashSet<string> TrivialPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "idk",
        "i dont know",
        "i don't know",
        "no idea",
        "dunno",
        "help",
        "help me",
        "please help",
        "pls help",
        "give hint",
        "give me hint",
        "next hint",
        "next",
        "test",
        "testing",
        "asdf",
        "qwerty",
        "give answer",
        "give solution",
        "show answer"
    };

    private static readonly Regex FullSolutionSolicitationRegex = new(
        @"\b(give|show|tell|provide|write|send)\s+(me\s+)?(the\s+)?((full|complete|entire|whole)\s+)*(solution|code|answer|implementation)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches any alphanumeric character repeated 5 or more times (e.g. "aaaaa", "11111")
    private static readonly Regex RepeatedCharPattern = new(
        @"([a-zA-Z0-9])\1{4,}",
        RegexOptions.Compiled);

    // Matches consecutive consonants of length >= 6 (keysmash like "qwrtypsdfg", "zxvbnm")
    private static readonly Regex KeySmashPattern = new(
        @"^[bcdfghjklmnpqrstvwxyz]{6,}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches repeated keysmash n-grams like "asdfasdf", "qwertyqwerty"
    private static readonly Regex RepetitiveKeySequencePattern = new(
        @"^([a-zA-Z]{2,4})\1{2,}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Validates whether the written attempt meets the minimum quality criteria to unlock a hint.
    /// Requires at least one meaningful line detailing what the user attempted or understood.
    /// </summary>
    public static AttemptValidationResult Validate(string? attemptText)
    {
        if (string.IsNullOrWhiteSpace(attemptText))
        {
            return AttemptValidationResult.Reject(
                AttemptRejectionReason.EmptyOrWhitespace,
                "Your attempt cannot be empty. Write at least one line explaining what you tried or understood so far.");
        }

        var trimmed = attemptText.Trim();

        // 1. Check if user is begging for full solution rather than describing an attempt
        if (FullSolutionSolicitationRegex.IsMatch(trimmed))
        {
            return AttemptValidationResult.Reject(
                AttemptRejectionReason.FullSolutionSolicitation,
                "Codexo does not provide full solutions. Write a line describing what you have tried or what you understand about the problem constraints.");
        }

        // 2. Check for repeated character floods (e.g. "aaaaaa", "111111")
        if (RepeatedCharPattern.IsMatch(trimmed))
        {
            return AttemptValidationResult.Reject(
                AttemptRejectionReason.JunkOrGibberish,
                "Your submission contains repetitive characters. Please provide genuine problem notes.");
        }

        // 3. Check for repetitive n-grams (e.g. "asdfasdfasdf", "qwertyqwerty")
        if (RepetitiveKeySequencePattern.IsMatch(trimmed))
        {
            return AttemptValidationResult.Reject(
                AttemptRejectionReason.JunkOrGibberish,
                "Your submission appears to contain repetitive keyboard patterns.");
        }

        // 4. Check for keysmash tokens (e.g. long consonant strings with zero vowels)
        var words = trimmed.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            var cleanedWord = Regex.Replace(word, @"[^a-zA-Z]", "");
            if (KeySmashPattern.IsMatch(cleanedWord))
            {
                return AttemptValidationResult.Reject(
                    AttemptRejectionReason.JunkOrGibberish,
                    "Your submission appears to contain random keyboard typing. Please explain your genuine thought process.");
            }
        }

        // 5. Check trivial one-liners
        if (TrivialPhrases.Contains(trimmed))
        {
            return AttemptValidationResult.Reject(
                AttemptRejectionReason.JunkOrGibberish,
                "Trivial responses do not qualify as an attempt. Explain what approach you considered or what part confuses you.");
        }

        // 6. Minimum character length requirement
        if (trimmed.Length < MinCharacterLength)
        {
            return AttemptValidationResult.Reject(
                AttemptRejectionReason.TooShort,
                $"Your attempt is too short ({trimmed.Length} characters). Please write at least one complete sentence about your thoughts or attempt.");
        }

        // 7. Minimum word count requirement
        if (words.Length < MinWordCount)
        {
            return AttemptValidationResult.Reject(
                AttemptRejectionReason.TooShort,
                "Please describe your thinking in at least 3 words.");
        }

        return AttemptValidationResult.Success();
    }
}
