// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.FileUpload;

public class MinimalFileUploadRequestMetadata
{
    [JsonPropertyName("file")]
    public FileDetails File { get; set; }
}