// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.FileUpload.Responses;

public class FileUploadResponse
{
    [JsonPropertyName("file")]
    public UploadedFile File { get; set; }
}