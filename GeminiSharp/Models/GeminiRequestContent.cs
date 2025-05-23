// // Copyright ©  2025 no-pact
// // Author: canka

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models;

public class GeminiRequestContent
{
    [JsonPropertyName("parts")]
    public List<GeminiRequestPart> Parts { get; set; }

    [JsonPropertyName("role")] // Optional, e.g., "user" or "model"
    public string Role { get; set; }
}