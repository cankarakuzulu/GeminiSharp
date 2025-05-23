using GeminiSharp.Common;
using GeminiSharp.Models;
using GeminiSharp.Models.FileUpload.Responses;
using GeminiSharp.Models.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeminiSharp;

public interface IGeminiApiClient
{
    /// <summary>
    /// Uploads a file for use with the Gemini API.
    /// </summary>
    /// <param name="filePath">The local path to the file to upload.</param>
    /// <param name="displayName">An optional display name for the file on the server.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A Result containing the UploadedFile metadata on success, or an ApiError on failure.</returns>
    Task<Result<UploadedFile, ApiError>> UploadFileAsync(string filePath, string displayName = null,
                                                         CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat response based on the provided history and new user input parts.
    /// </summary>
    /// <param name="history">The existing conversation history.</param>
    /// <param name="newUserParts">A list of parts for the new user message (can include text and file data).</param>
    /// <param name="modelName">Optional: Specific model name to use, overriding the default.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A Result containing the model's text response on success, or an ApiError on failure.</returns>
    Task<Result<string, ApiError>> GenerateChatResponseAsync(
        List<GeminiRequestContent> history,
        List<GeminiRequestPart> newUserParts,
        string modelName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat response based on the provided history and a new user text prompt.
    /// </summary>
    /// <param name="history">The existing conversation history.</param>
    /// <param name="newUserPromptText">The new text prompt from the user.</param>
    /// <param name="modelName">Optional: Specific model name to use, overriding the default.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A Result containing the model's text response on success, or an ApiError on failure.</returns>
    Task<Result<string, ApiError>> GenerateChatResponseAsync(
        List<GeminiRequestContent> history,
        string newUserPromptText,
        string modelName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an image based on a text prompt.
    /// </summary>
    /// <param name="prompt">The text prompt describing the image to generate.</param>
    /// <param name="sampleCount">Number of images to generate (typically 1).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A Result containing the base64 encoded image string on success, or an ApiError on failure.</returns>
    Task<Result<string, ApiError>> GenerateImageAsync(string prompt, int sampleCount = 1, string modelName = null,
                                                      CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat response as an asynchronous stream of content chunks.
    /// Each yielded item is a Result containing a GeminiGenerateContentResponse chunk or an ApiError.
    /// </summary>
    IAsyncEnumerable<Result<GeminiGenerateContentResponse, ApiError>> StreamGenerateChatResponseAsync(
        List<GeminiRequestContent> history,
        List<GeminiRequestPart> newUserParts,
        string modelName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat response as an asynchronous stream of content chunks using a simple text prompt.
    /// Each yielded item is a Result containing a GeminiGenerateContentResponse chunk or an ApiError.
    /// </summary>
    IAsyncEnumerable<Result<GeminiGenerateContentResponse, ApiError>> StreamGenerateChatResponseAsync(
        List<GeminiRequestContent> history,
        string newUserPromptText,
        string modelName = null,
        CancellationToken cancellationToken = default);
}