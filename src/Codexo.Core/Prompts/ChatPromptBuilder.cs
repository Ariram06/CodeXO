namespace Codexo.Core.Prompts;

using System.Reflection;
using System.Text;
using Codexo.Core.Dtos.Chat;
using Codexo.Core.Entities;
using Codexo.Core.Enums;
using Codexo.Core.Security;

/// <summary>
/// Assembles system prompts, historical turns, and structured data-fenced user prompts for chat generation.
/// Implements prompt-injection defense by treating code, problem descriptions, and selections strictly as DATA.
/// </summary>
public class ChatPromptBuilder
{
    public const int MaxHistoryMessages = 12;
    private static string? _cachedSystemPrompt;
    private static readonly object LockObj = new();

    public string BuildSystemPrompt()
    {
        lock (LockObj)
        {
            if (_cachedSystemPrompt != null)
            {
                return _cachedSystemPrompt;
            }

            const string resourceName = "Codexo.Core.Prompts.Templates.Chat.txt";
            var assembly = typeof(ChatPromptBuilder).Assembly;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Embedded resource '{resourceName}' not found in {assembly.FullName}.");
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            _cachedSystemPrompt = reader.ReadToEnd();
            return _cachedSystemPrompt;
        }
    }

    public string BuildUserPrompt(string userMessage, PageContext? context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

        var sb = new StringBuilder();

        if (context != null)
        {
            sb.AppendLine("=== [ACTIVE PAGE CONTEXT (DATA ONLY)] ===");
            if (!string.IsNullOrWhiteSpace(context.Platform))
            {
                sb.AppendLine($"Platform: {context.Platform}");
            }
            if (!string.IsNullOrWhiteSpace(context.Title))
            {
                sb.AppendLine($"Title: {context.Title}");
            }
            if (!string.IsNullOrWhiteSpace(context.Url))
            {
                sb.AppendLine($"URL: {context.Url}");
            }
            if (!string.IsNullOrWhiteSpace(context.Language))
            {
                sb.AppendLine($"Language: {context.Language}");
            }
            if (context.IsTruncated)
            {
                sb.AppendLine("[Notice: Input context was truncated to fit model token limits]");
            }
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(context.ProblemText))
            {
                var redactedProblem = SecretRedactor.Redact(context.ProblemText.Trim());
                sb.AppendLine("=== [PROBLEM DESCRIPTION (DATA ONLY)] ===");
                sb.AppendLine(redactedProblem);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(context.Code))
            {
                var lang = string.IsNullOrWhiteSpace(context.Language) ? "" : context.Language.ToLowerInvariant();
                var redactedCode = SecretRedactor.Redact(context.Code);
                sb.AppendLine("=== [CURRENT EDITOR CODE (DATA ONLY)] ===");
                sb.AppendLine($"```{lang}");
                sb.AppendLine(redactedCode);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(context.Selection))
            {
                var redactedSelection = SecretRedactor.Redact(context.Selection.Trim());
                sb.AppendLine("=== [SELECTED CODE / TEXT (DATA ONLY)] ===");
                sb.AppendLine(redactedSelection);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(context.ErrorText))
            {
                var redactedError = SecretRedactor.Redact(context.ErrorText.Trim());
                sb.AppendLine("=== [RUNTIME / ERROR OUTPUT (DATA ONLY)] ===");
                sb.AppendLine(redactedError);
                sb.AppendLine();
            }
        }

        sb.AppendLine("=== [USER QUESTION] ===");
        sb.Append(userMessage.Trim());

        return sb.ToString();
    }

    public IEnumerable<(string Role, string Content)> FormatHistory(IEnumerable<Message>? messages)
    {
        if (messages == null)
        {
            return Enumerable.Empty<(string Role, string Content)>();
        }

        // Keep strictly the last MaxHistoryMessages turns
        var recent = messages
            .OrderBy(m => m.CreatedAtUtc)
            .TakeLast(MaxHistoryMessages);

        return recent.Select(m => (
            Role: m.Role == MessageRole.Assistant ? "assistant" : "user",
            Content: m.Content
        )).ToList();
    }
}
