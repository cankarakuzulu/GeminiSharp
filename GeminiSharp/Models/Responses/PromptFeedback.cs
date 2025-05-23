// // Copyright ©  2025 no-pact
// // Author: canka

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models.Responses;

public class PromptFeedback
{
    [JsonPropertyName("blockReason")]
    public string BlockReason { get; set; } // e.g., "SAFETY"

    [JsonPropertyName("blockReasonMessage")]
    public string BlockReasonMessage { get; set; } // More descriptive if available

    [JsonPropertyName("safetyRatings")]
    public List<SafetyRating> SafetyRatings { get; set; }
}