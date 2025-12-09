using System.Text.Json.Serialization;

namespace Byee.Server.Models;

public class UploadResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;
}
