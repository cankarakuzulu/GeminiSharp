// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.FileUpload;

public class FileDetails
{
    [JsonPropertyName("display_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string DisplayName { get; set; }
}