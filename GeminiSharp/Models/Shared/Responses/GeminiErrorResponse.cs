// // Copyright ©  2025 no-pact
// // Author: canka

using System.Text.Json.Serialization;

namespace GeminiSharp.Models.Shared.Responses;

public class GeminiErrorResponse
{
    [JsonPropertyName("error")]
    public GeminiError Error { get; set; }
}