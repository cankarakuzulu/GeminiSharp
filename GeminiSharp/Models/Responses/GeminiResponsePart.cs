// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.Responses;

public class GeminiResponsePart
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}