using System.Text.Json.Serialization;

namespace Byee.Server.Models;

public class FileInfoResponse
{
    [JsonPropertyName("filename")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("size_human")]
    public string SizeHuman { get; set; } = string.Empty;

    [JsonPropertyName("claim_token")]
    public string ClaimToken { get; set; } = string.Empty;
}
