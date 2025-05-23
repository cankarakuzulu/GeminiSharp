// // Copyright ©  2025 no-pact
// // Author: canka

using System;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models.FileUpload.Responses;

public class UploadedFile
{
    // This 'Name' is the crucial identifier, e.g., "files/your-file-id"
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [JsonPropertyName("sizeBytes")]
    public string SizeBytes { get; set; } // API returns as string

    [JsonPropertyName("createTime")]
    public DateTime CreateTime { get; set; }

    [JsonPropertyName("updateTime")]
    public DateTime UpdateTime { get; set; }

    [JsonPropertyName("expirationTime")]
    public DateTime ExpirationTime { get; set; }

    [JsonPropertyName("sha256Hash")]
    public string Sha256Hash { get; set; }

    // This URI can also be used to reference the file directly in some contexts
    // or is the unique URI for the file resource itself.
    [JsonPropertyName("uri")]
    public string Uri { get; set; }
}