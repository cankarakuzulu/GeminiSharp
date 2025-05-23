// // Copyright ©  2025 no-pact
// // Author: canka

using GeminiSharp.Models.Shared;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models;

public class GeminiGenerateContentRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiRequestContent> Contents { get; set; }

    [JsonPropertyName("generationConfig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GenerationConfig GenerationConfig { get; set; }

    [JsonPropertyName("safetySettings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SafetySetting> SafetySettings { get; set; }
}