namespace Codexo.Core.Prompts;

using System.Reflection;
using System.Text;
using Codexo.Core.AI;
using Codexo.Core.Enums;

/// <summary>
/// Assembles system and user prompts for AI hint generation using modular level templates.
/// </summary>
public class HintPromptBuilder
{
    private static readonly Dictionary<string, string> TemplateCache = new();
    private static readonly object LockObject = new();

    public (string SystemPrompt, string UserPrompt) BuildPrompt(AiPromptRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var systemPrompt = BuildSystemPrompt(request.Level);
        var userPrompt = BuildUserPrompt(request);

        return (systemPrompt, userPrompt);
    }

    public string BuildSystemPrompt(HintLevel level)
    {
        var baseTemplate = LoadTemplate("Base");
        var levelKey = level switch
        {
            HintLevel.L1_Understand => "L1",
            HintLevel.L2_Direction => "L2",
            HintLevel.L3_Concepts => "L3",
            HintLevel.L4_Steps => "L4",
            HintLevel.L5_Deep => "L5",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Hint level must be between L1 and L5.")
        };

        var levelTemplate = LoadTemplate(levelKey);

        var sb = new StringBuilder();
        sb.AppendLine(baseTemplate);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(levelTemplate);

        return sb.ToString();
    }

    public string BuildUserPrompt(AiPromptRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sb = new StringBuilder();

        sb.AppendLine("=== DATA: PROBLEM ===");
        sb.AppendLine($"Title: {request.ProblemTitle}");
        sb.AppendLine("Description:");
        sb.AppendLine(request.ProblemDescription.Trim());
        sb.AppendLine();

        sb.AppendLine("=== DATA: USER_ATTEMPT ===");
        if (string.IsNullOrWhiteSpace(request.UserAttempt))
        {
            sb.AppendLine("None (First hint request or L1 understand level).");
        }
        else
        {
            sb.AppendLine(request.UserAttempt.Trim());
        }
        sb.AppendLine();

        sb.AppendLine("=== DATA: PREVIOUS_HINTS ===");
        if (request.PreviousHints == null || request.PreviousHints.Count == 0)
        {
            sb.AppendLine("None (No prior hints provided for this problem session).");
        }
        else
        {
            for (var i = 0; i < request.PreviousHints.Count; i++)
            {
                sb.AppendLine($"[Hint {i + 1}] {request.PreviousHints[i].Trim()}");
            }
        }

        return sb.ToString();
    }

    private static string LoadTemplate(string name)
    {
        lock (LockObject)
        {
            if (TemplateCache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            var resourceName = $"Codexo.Core.Prompts.Templates.{name}.txt";
            var assembly = typeof(HintPromptBuilder).Assembly;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"Embedded prompt template '{resourceName}' was not found in assembly {assembly.FullName}. " +
                    $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var content = reader.ReadToEnd();
            TemplateCache[name] = content;
            return content;
        }
    }
}
