// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.FileUpload;

public class GeminiFileDataPart 
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [JsonPropertyName("fileUri")]
    public string FileUri { get; set; }
}