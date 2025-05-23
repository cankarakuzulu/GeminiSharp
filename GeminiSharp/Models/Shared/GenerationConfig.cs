// // Copyright ©  2025 no-pact
// // Author: canka

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models.Shared;

public class GenerationConfig
{
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("topK")]
    public int? TopK { get; set; }

    [JsonPropertyName("topP")]
    public float? TopP { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }

    [JsonPropertyName("stopSequences")]
    public List<string> StopSequences { get; set; }
}