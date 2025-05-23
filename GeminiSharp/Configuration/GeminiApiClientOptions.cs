// // Copyright ©  2025 no-pact
// // Author: canka

namespace GeminiSharp.Configuration;

public class GeminiApiClientOptions
{
    public const string SectionName = "GeminiApi";
    public string ApiKey { get; set; }
    public string DefaultModelName { get; set; } = "gemini-2.0-flash";

    public string GenerativeLanguageBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/";
    public string FileApiUploadBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/upload/v1beta/files";
    public string StreamGenerateContentEndpointSuffix { get; set; } = ":streamGenerateContent"; 
    public string ImageGenerationDefaultModel { get; set; } = "imagen-3.0-generate-002";
    public string ImageGenerationBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/";

}