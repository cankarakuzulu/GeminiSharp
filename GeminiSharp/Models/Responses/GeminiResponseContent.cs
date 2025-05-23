// // Copyright ©  2025 no-pact
// // Author: canka

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models.Responses;

public class GeminiResponseContent
{
    [JsonPropertyName("parts")]
    public List<GeminiResponsePart> Parts { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }
}