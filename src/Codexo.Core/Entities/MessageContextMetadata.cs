namespace Codexo.Core.Entities;

/// <summary>
/// Persisted metadata about the editor and browser context active when a message was sent.
/// Stores ONLY structural and auditing metadata (counts, platform, titles); NEVER raw code or page text.
/// </summary>
public class MessageContextMetadata
{
    public string? Platform { get; set; }
    public string? Host { get; set; }
    public string? Title { get; set; }
    public string? Language { get; set; }
    public int? CodeLineCount { get; set; }
    public bool IsTruncated { get; set; }
}
