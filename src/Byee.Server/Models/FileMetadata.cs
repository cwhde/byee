using System.Text.Json.Serialization;

namespace Byee.Server.Models;

public class FileMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("encrypted_size")]
    public long EncryptedSize { get; set; }

    [JsonPropertyName("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("claimed")]
    public bool Claimed { get; set; }

    [JsonPropertyName("claimed_at")]
    public DateTime? ClaimedAt { get; set; }

    [JsonPropertyName("claim_token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClaimToken { get; set; }

    [JsonPropertyName("is_folder")]
    public bool IsFolder { get; set; }
}
