// // Copyright ©  2025 no-pact
// // Author: canka

using GeminiSharp.Models.FileUpload;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models;

public class GeminiRequestPart
{
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Only include if not null
    public string Text { get; set; }

    [JsonPropertyName("fileData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Only include if not null
    public GeminiFileDataPart FileData { get; set; } // Add this for file references
}