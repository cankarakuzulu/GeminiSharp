// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.ImageGeneration;

public class ImageGenerationInstance
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }
}