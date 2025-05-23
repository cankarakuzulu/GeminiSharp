// // Copyright ©  2025 no-pact
// // Author: canka

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models.ImageGeneration.Responses;

public class ImageGenerationResponse
{
    [JsonPropertyName("predictions")]
    public List<ImagePrediction> Predictions { get; set; }
}