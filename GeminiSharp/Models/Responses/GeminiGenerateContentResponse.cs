// // Copyright ©  2025 no-pact
// // Author: canka

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeminiSharp.Models.Responses;

public class GeminiGenerateContentResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate> Candidates { get; set; }

    [JsonPropertyName("promptFeedback")]
    public PromptFeedback PromptFeedback { get; set; } // Add this
}