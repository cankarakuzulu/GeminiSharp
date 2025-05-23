// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.Shared;

public class SafetySetting
{
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("threshold")]
    public string Threshold { get; set; }
}