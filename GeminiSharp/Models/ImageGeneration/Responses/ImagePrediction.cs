// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.ImageGeneration.Responses;

public class ImagePrediction
{
    [JsonPropertyName("bytesBase64Encoded")]
    public string BytesBase64Encoded { get; set; }
    // Other potential fields like "mimeType" might be present, add if needed.
}