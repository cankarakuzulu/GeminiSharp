// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.ImageGeneration;

public class ImageGenerationParameters
{
    [JsonPropertyName("sampleCount")]
    public int SampleCount { get; set; } = 1; // Default to 1 image
}