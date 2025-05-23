// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.Responses;

public class SafetyRating
{
    [JsonPropertyName("category")]
    public string Category { get; set; } // e.g., "HARM_CATEGORY_SEXUALLY_EXPLICIT"
    [JsonPropertyName("probability")]
    public string Probability { get; set; } // e.g., "NEGLIGIBLE", "LOW", "MEDIUM", "HIGH"
}