namespace Codexo.Core.Security;

using System.Text.RegularExpressions;

/// <summary>
/// Redacts sensitive credentials, private keys, API keys, and password assignments
/// from user context (problem text, code, selection) before prompt construction or persistence.
/// </summary>
public static class SecretRedactor
{
    private const string Mask = "[REDACTED_SECRET]";

    private static readonly Regex GoogleApiKeyRegex = new(
        @"\bAIza[0-9A-Za-z\-_]{30,40}\b",
        RegexOptions.Compiled);

    private static readonly Regex GenericApiKeyRegex = new(
        @"\b(sk-[a-zA-Z0-9_\-]{20,}|ghp_[a-zA-Z0-9]{36}|glpat-[a-zA-Z0-9\-=_]{20,})\b",
        RegexOptions.Compiled);

    private static readonly Regex PrivateKeyBlockRegex = new(
        @"-----BEGIN (?:[A-Z ]+ )?PRIVATE KEY-----[\s\S]*?-----END (?:[A-Z ]+ )?PRIVATE KEY-----",
        RegexOptions.Compiled);

    private static readonly Regex BearerTokenRegex = new(
        @"(?i)\bBearer\s+[a-zA-Z0-9\-._~+/]+=*",
        RegexOptions.Compiled);

    private static readonly Regex PasswordAssignmentRegex = new(
        @"(?i)\b(password|passwd|pwd|secret|api_?key)\s*[:=]\s*[""']?([^ \t\r\n""';,]+)[""']?",
        RegexOptions.Compiled);

    public static string? Redact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var result = text;
        result = PrivateKeyBlockRegex.Replace(result, Mask);
        result = GoogleApiKeyRegex.Replace(result, Mask);
        result = GenericApiKeyRegex.Replace(result, Mask);
        result = BearerTokenRegex.Replace(result, "Bearer " + Mask);
        result = PasswordAssignmentRegex.Replace(result, m =>
        {
            var keyName = m.Groups[1].Value;
            var delimiter = m.Value.Contains(':') ? ":" : "=";
            return $"{keyName} {delimiter} \"{Mask}\"";
        });

        return result;
    }
}
