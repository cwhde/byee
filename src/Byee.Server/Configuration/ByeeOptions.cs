namespace Byee.Server.Configuration;

public class ByeeOptions
{
    public const string SectionName = "Byee";

    /// <summary>
    /// Public URL of this instance (e.g., https://byee.example.com)
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    /// Path to store encrypted files
    /// </summary>
    public string StoragePath { get; set; } = "./data";

    /// <summary>
    /// Maximum file size in bytes (default 100GB)
    /// </summary>
    public long MaxFileSize { get; set; } = 107374182400;

    /// <summary>
    /// Number of words in generated file IDs
    /// </summary>
    public int IdWordCount { get; set; } = 4;

    /// <summary>
    /// How long to keep claimed files before auto-cleanup (in case download never completes)
    /// </summary>
    public TimeSpan ClaimedFileTimeout { get; set; } = TimeSpan.FromHours(24);
}
