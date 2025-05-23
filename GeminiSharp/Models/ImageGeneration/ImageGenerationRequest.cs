// // Copyright ©  2025 no-pact
// // Author: canka

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models.ImageGeneration;

public class ImageGenerationRequest
{
    // As per persona, instances is an array of objects, each object having a prompt.
    [JsonPropertyName("instances")]
    public List<ImageGenerationInstance> Instances { get; set; }

    [JsonPropertyName("parameters")]
    public ImageGenerationParameters Parameters { get; set; }
}