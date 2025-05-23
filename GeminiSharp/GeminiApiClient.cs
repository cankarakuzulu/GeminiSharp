using GeminiSharp;
using GeminiSharp.Common;
using GeminiSharp.Configuration;
using GeminiSharp.Models;
using GeminiSharp.Models.FileUpload;
using GeminiSharp.Models.FileUpload.Responses;
using GeminiSharp.Models.ImageGeneration;
using GeminiSharp.Models.ImageGeneration.Responses;
using GeminiSharp.Models.Responses;
using GeminiSharp.Models.Shared.Responses;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Options; // For IOptions
using System.Runtime.CompilerServices; // For IAsyncEnumerable

public class GeminiApiClient : IGeminiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiApiClientOptions _options;

    public GeminiApiClient(HttpClient httpClient, IOptions<GeminiApiClientOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ??
                   throw new ArgumentNullException(nameof(options), "Gemini API options cannot be null.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new ArgumentException("API key in options cannot be null or empty.",
                $"{nameof(options)}.{nameof(_options.ApiKey)}");

        if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    // ... (UploadFileAsync, SendGenerationRequestInternalAsync, non-streaming GenerateChatResponseAsync methods) ...

#region NonStreamingMethods

    public async Task<Result<UploadedFile, ApiError>> UploadFileAsync(string filePath, string displayName = null,
                                                                      CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return Result<UploadedFile, ApiError>.Fail(new ApiError($"File not found: {filePath}",
                ErrorType.FileNotFound));

        var fileName = Path.GetFileName(filePath);
        var actualMimeType = GetMimeType(fileName);
        byte[] fileBytes;
        try
        {
            fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UploadedFile, ApiError>.Fail(new ApiError("File read operation was cancelled.",
                ErrorType.OperationCancelled));
        }
        catch (Exception ex)
        {
            return Result<UploadedFile, ApiError>.Fail(ApiError.FromException(ex, ErrorType.FileNotFound,
                $"Failed to read file: {filePath}"));
        }

        long numBytes = fileBytes.Length;

        var initiationUrl = $"{_options.FileApiUploadBaseUrl}?key={_options.ApiKey}";
        var metadataForRequest = new MinimalFileUploadRequestMetadata
                                 {
                                     File = new FileDetails
                                            {
                                                DisplayName =
                                                    string.IsNullOrWhiteSpace(displayName) ? fileName : displayName
                                            }
                                 };
        string jsonMetadataPayload;
        try
        {
            jsonMetadataPayload = JsonSerializer.Serialize(metadataForRequest,
                new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        }
        catch (JsonException ex)
        {
            return Result<UploadedFile, ApiError>.Fail(
                ApiError.SerializationFailure("Failed to serialize file initiation metadata.", ex));
        }

        string uploadUrl = null;
        try
        {
            using var initiationRequest = new HttpRequestMessage(HttpMethod.Post, initiationUrl);
            initiationRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
            initiationRequest.Headers.Add("X-Goog-Upload-Command", "start");
            initiationRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", numBytes.ToString());
            initiationRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", actualMimeType);
            initiationRequest.Content = new StringContent(jsonMetadataPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage initiationResponse = await _httpClient.SendAsync(initiationRequest, cancellationToken);
            string initiationResponseBody = await initiationResponse.Content.ReadAsStringAsync(cancellationToken);

            if (initiationResponse.IsSuccessStatusCode)
            {
                if (initiationResponse.Headers.TryGetValues("X-Goog-Upload-URL", out var urls))
                    uploadUrl = urls.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(uploadUrl) && initiationResponse.Headers.Location != null)
                    uploadUrl = initiationResponse.Headers.Location.OriginalString;

                if (string.IsNullOrWhiteSpace(uploadUrl))
                    return Result<UploadedFile, ApiError>.Fail(new ApiError(
                        "File upload initiation succeeded but X-Goog-Upload-URL header was not found.",
                        ErrorType.UploadFailed, (int)initiationResponse.StatusCode, details: initiationResponseBody));
            }
            else
            {
                var errorResponse = TryDeserialize<GeminiErrorResponse>(initiationResponseBody);
                if (errorResponse?.Error != null)
                    return Result<UploadedFile, ApiError>.Fail(ApiError.FromGeminiError(errorResponse.Error,
                        (int)initiationResponse.StatusCode));
                else
                    return Result<UploadedFile, ApiError>.Fail(ApiError.FromHttpError("File upload initiation failed.",
                        (int)initiationResponse.StatusCode, initiationResponseBody));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UploadedFile, ApiError>.Fail(new ApiError("File upload initiation was cancelled.",
                ErrorType.OperationCancelled));
        }
        catch (HttpRequestException ex)
        {
            return Result<UploadedFile, ApiError>.Fail(ApiError.FromException(ex, ErrorType.NetworkError,
                "Network error during file upload initiation."));
        }
        catch (Exception ex)
        {
            return Result<UploadedFile, ApiError>.Fail(ApiError.FromException(ex, ErrorType.Unknown,
                "Unexpected error during file upload initiation."));
        }

        try
        {
            using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
            uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");
            uploadRequest.Content = new ByteArrayContent(fileBytes);
            uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(actualMimeType);

            HttpResponseMessage uploadResponseMsg = await _httpClient.SendAsync(uploadRequest, cancellationToken);
            string uploadResponseBody = await uploadResponseMsg.Content.ReadAsStringAsync(cancellationToken);

            if (uploadResponseMsg.IsSuccessStatusCode)
            {
                FileUploadResponse finalResponse;
                try
                {
                    finalResponse = JsonSerializer.Deserialize<FileUploadResponse>(uploadResponseBody);
                }
                catch (JsonException jsonEx)
                {
                    return Result<UploadedFile, ApiError>.Fail(
                        ApiError.DeserializationFailure("Failed to deserialize final file upload response.", jsonEx));
                }

                if (finalResponse?.File == null)
                    return Result<UploadedFile, ApiError>.Fail(new ApiError(
                        "File byte upload succeeded but the final response did not contain file metadata.",
                        ErrorType.UploadFailed, (int)uploadResponseMsg.StatusCode, details: uploadResponseBody));
                return Result<UploadedFile, ApiError>.Ok(finalResponse.File);
            }
            else
            {
                var errorResponse = TryDeserialize<GeminiErrorResponse>(uploadResponseBody);
                if (errorResponse?.Error != null)
                    return Result<UploadedFile, ApiError>.Fail(ApiError.FromGeminiError(errorResponse.Error,
                        (int)uploadResponseMsg.StatusCode));
                else
                    return Result<UploadedFile, ApiError>.Fail(ApiError.FromHttpError("File byte upload failed.",
                        (int)uploadResponseMsg.StatusCode, uploadResponseBody));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UploadedFile, ApiError>.Fail(new ApiError("File byte upload was cancelled.",
                ErrorType.OperationCancelled));
        }
        catch (HttpRequestException ex)
        {
            return Result<UploadedFile, ApiError>.Fail(ApiError.FromException(ex, ErrorType.NetworkError,
                "Network error during file byte upload."));
        }
        catch (Exception ex)
        {
            return Result<UploadedFile, ApiError>.Fail(ApiError.FromException(ex, ErrorType.Unknown,
                "Unexpected error during file byte upload."));
        }
    }

    private async Task<Result<GeminiGenerateContentResponse, ApiError>> SendGenerationRequestInternalAsync(
        GeminiGenerateContentRequest requestPayload, string modelNameToUse = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveModelName = string.IsNullOrWhiteSpace(modelNameToUse) ? _options.DefaultModelName : modelNameToUse;
        if (string.IsNullOrWhiteSpace(effectiveModelName))
            return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                new ApiError("Model name is not specified and no default is configured.",
                    ErrorType.ConfigurationError));

        var requestUrl =
            $"{_options.GenerativeLanguageBaseUrl}{effectiveModelName}:generateContent?key={_options.ApiKey}";
        string jsonPayload;
        try
        {
            jsonPayload = JsonSerializer.Serialize(requestPayload,
                new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        }
        catch (JsonException ex)
        {
            return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                ApiError.SerializationFailure("Failed to serialize generation request.", ex));
        }

        var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, httpContent, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                GeminiGenerateContentResponse geminiResponse;
                try
                {
                    geminiResponse = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(responseBody);
                }
                catch (JsonException jsonEx)
                {
                    return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                        ApiError.DeserializationFailure("Failed to deserialize successful generation response.",
                            jsonEx));
                }

                if (geminiResponse == null)
                    return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                        new ApiError("Generation response was null after deserialization.",
                            ErrorType.DeserializationError, (int)response.StatusCode, details: responseBody));
                return Result<GeminiGenerateContentResponse, ApiError>.Ok(geminiResponse);
            }
            else
            {
                var errorResponse = TryDeserialize<GeminiErrorResponse>(responseBody);
                if (errorResponse?.Error != null)
                    return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                        ApiError.FromGeminiError(errorResponse.Error, (int)response.StatusCode));
                else
                    return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                        ApiError.FromHttpError($"API Error ({effectiveModelName})", (int)response.StatusCode,
                            responseBody));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                new ApiError("Content generation request was cancelled.", ErrorType.OperationCancelled));
        }
        catch (HttpRequestException ex)
        {
            return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                ApiError.FromException(ex, ErrorType.NetworkError, "Network error during content generation."));
        }
        catch (Exception ex)
        {
            return Result<GeminiGenerateContentResponse, ApiError>.Fail(ApiError.FromException(ex, ErrorType.Unknown,
                "Unexpected error during content generation."));
        }
    }

    public async Task<Result<string, ApiError>> GenerateChatResponseAsync(
        List<GeminiRequestContent> history, List<GeminiRequestPart> newUserParts, string modelName = null,
        CancellationToken cancellationToken = default)
    {
        if (newUserParts == null || !newUserParts.Any())
            return Result<string, ApiError>.Fail(new ApiError("New user parts cannot be null or empty.",
                ErrorType.InvalidInput));
        var newUserContent = new GeminiRequestContent { Parts = newUserParts, Role = "user" };
        var combinedContents = (history ?? new List<GeminiRequestContent>()).ToList();
        combinedContents.Add(newUserContent);
        var requestPayload = new GeminiGenerateContentRequest { Contents = combinedContents };
        var generationResult = await SendGenerationRequestInternalAsync(requestPayload, modelName, cancellationToken);
        return generationResult.Match(
            onSuccess: geminiResponse =>
            {
                if (geminiResponse?.Candidates != null && geminiResponse.Candidates.Any() &&
                    geminiResponse.Candidates[0].Content?.Parts != null &&
                    geminiResponse.Candidates[0].Content.Parts.Any())
                {
                    return Result<string, ApiError>.Ok(string.Join(" ",
                        geminiResponse.Candidates[0].Content.Parts.Where(p => !string.IsNullOrEmpty(p.Text))
                                      .Select(p => p.Text)));
                }

                if (geminiResponse?.PromptFeedback?.BlockReason != null)
                    return Result<string, ApiError>.Ok(
                        $"Blocked due to: {geminiResponse.PromptFeedback.BlockReasonMessage ?? geminiResponse.PromptFeedback.BlockReason}");
                return Result<string, ApiError>.Ok("No content generated or unexpected response structure.");
            },
            onFailure: error => Result<string, ApiError>.Fail(error));
    }

    public async Task<Result<string, ApiError>> GenerateChatResponseAsync(
        List<GeminiRequestContent> history, string newUserPromptText, string modelName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newUserPromptText))
            return Result<string, ApiError>.Fail(new ApiError("New user prompt text cannot be null or empty.",
                ErrorType.InvalidInput));
        var newUserParts = new List<GeminiRequestPart> { new GeminiRequestPart { Text = newUserPromptText } };
        return await GenerateChatResponseAsync(history, newUserParts, modelName, cancellationToken);
    }

    public async Task<Result<string, ApiError>> GenerateImageAsync(string prompt, int sampleCount = 1,
                                                                   string imageModelName = null,
                                                                   CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return Result<string, ApiError>.Fail(new ApiError("Prompt cannot be null or empty for image generation.",
                ErrorType.InvalidInput));
        if (sampleCount < 1)
            return Result<string, ApiError>.Fail(new ApiError("Sample count must be at least 1.",
                ErrorType.InvalidInput));

        var effectiveImageModelName = string.IsNullOrWhiteSpace(imageModelName)
                                          ? _options.ImageGenerationDefaultModel
                                          : imageModelName;
        if (string.IsNullOrWhiteSpace(effectiveImageModelName))
            return Result<string, ApiError>.Fail(new ApiError(
                "Image generation model name is not specified and no default is configured.",
                ErrorType.ConfigurationError));
        if (string.IsNullOrWhiteSpace(_options.ImageGenerationBaseUrl))
            return Result<string, ApiError>.Fail(new ApiError("Image generation base URL is not configured in options.",
                ErrorType.ConfigurationError));

        var requestUrl =
            $"{_options.ImageGenerationBaseUrl.TrimEnd('/')}/{effectiveImageModelName}:predict?key={_options.ApiKey}";
        var requestPayload = new ImageGenerationRequest
                             {
                                 Instances = new List<ImageGenerationInstance>
                                             { new ImageGenerationInstance { Prompt = prompt } },
                                 Parameters = new ImageGenerationParameters { SampleCount = sampleCount }
                             };
        string jsonPayload;
        try
        {
            jsonPayload = JsonSerializer.Serialize(requestPayload,
                new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        }
        catch (JsonException ex)
        {
            return Result<string, ApiError>.Fail(
                ApiError.SerializationFailure("Failed to serialize image generation request.", ex));
        }

        var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, httpContent, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                ImageGenerationResponse imageResponse;
                try
                {
                    imageResponse = JsonSerializer.Deserialize<ImageGenerationResponse>(responseBody);
                }
                catch (JsonException jsonEx)
                {
                    return Result<string, ApiError>.Fail(
                        ApiError.DeserializationFailure("Failed to deserialize image generation response.", jsonEx));
                }

                if (imageResponse?.Predictions != null && imageResponse.Predictions.Any())
                {
                    var firstPrediction = imageResponse.Predictions[0];
                    if (!string.IsNullOrWhiteSpace(firstPrediction.BytesBase64Encoded))
                        return Result<string, ApiError>.Ok(firstPrediction.BytesBase64Encoded);
                    return Result<string, ApiError>.Fail(new ApiError(
                        "Image generation succeeded but the prediction did not contain image data.",
                        ErrorType.ImageGenerationFailed, (int)response.StatusCode, details: responseBody));
                }

                return Result<string, ApiError>.Fail(new ApiError(
                    "Image generation response did not contain any predictions.", ErrorType.ImageGenerationFailed,
                    (int)response.StatusCode, details: responseBody));
            }
            else
            {
                var errorResponse = TryDeserialize<GeminiErrorResponse>(responseBody);
                if (errorResponse?.Error != null)
                    return Result<string, ApiError>.Fail(ApiError.FromGeminiError(errorResponse.Error,
                        (int)response.StatusCode));
                else
                    return Result<string, ApiError>.Fail(ApiError.FromHttpError(
                        $"Image generation API request failed ({effectiveImageModelName}).", (int)response.StatusCode,
                        responseBody));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<string, ApiError>.Fail(new ApiError("Image generation request was cancelled.",
                ErrorType.OperationCancelled));
        }
        catch (HttpRequestException ex)
        {
            return Result<string, ApiError>.Fail(ApiError.FromException(ex, ErrorType.NetworkError,
                "Network error during image generation."));
        }
        catch (Exception ex)
        {
            return Result<string, ApiError>.Fail(ApiError.FromException(ex, ErrorType.Unknown,
                "Unexpected error during image generation."));
        }
    }

#endregion NonStreamingMethods

    // --- Streaming Method ---
    public async IAsyncEnumerable<Result<GeminiGenerateContentResponse, ApiError>> StreamGenerateChatResponseAsync(
        List<GeminiRequestContent> history,
        List<GeminiRequestPart> newUserParts,
        string modelName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (newUserParts == null || !newUserParts.Any())
        {
            yield return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                new ApiError("New user parts cannot be null or empty.", ErrorType.InvalidInput));
            yield break;
        }

        var newUserContent = new GeminiRequestContent { Parts = newUserParts, Role = "user" };
        var combinedContents = (history ?? new List<GeminiRequestContent>()).ToList();
        combinedContents.Add(newUserContent);

        var requestPayload = new GeminiGenerateContentRequest { Contents = combinedContents };

        var effectiveModelName = string.IsNullOrWhiteSpace(modelName) ? _options.DefaultModelName : modelName;
        if (string.IsNullOrWhiteSpace(effectiveModelName))
        {
            yield return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                new ApiError("Model name is not specified and no default is configured.",
                    ErrorType.ConfigurationError));
            yield break;
        }

        if (string.IsNullOrWhiteSpace(_options.StreamGenerateContentEndpointSuffix))
        {
            yield return Result<GeminiGenerateContentResponse, ApiError>.Fail(new ApiError(
                "StreamGenerateContentEndpointSuffix is not configured in options.", ErrorType.ConfigurationError));
            yield break;
        }

        var requestUrl =
            $"{_options.GenerativeLanguageBaseUrl}{effectiveModelName}{_options.StreamGenerateContentEndpointSuffix}?key={_options.ApiKey}";

        string jsonPayload;
        ApiError serializationError = null;
        try
        {
            jsonPayload = JsonSerializer.Serialize(requestPayload,
                new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        }
        catch (JsonException ex)
        {
            serializationError = ApiError.SerializationFailure("Failed to serialize streaming generation request.", ex);
            jsonPayload = null; // Ensure it's not used
        }

        if (serializationError != null)
        {
            yield return Result<GeminiGenerateContentResponse, ApiError>.Fail(serializationError);
            yield break;
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = null;
        ApiError outerErrorToYield = null;
        Result<GeminiGenerateContentResponse, ApiError> resultToYield = default;
        bool isTheOperationOk = false;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead,
                           cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorResponse = TryDeserialize<GeminiErrorResponse>(errorBody);
                if (errorResponse?.Error != null)
                    outerErrorToYield = ApiError.FromGeminiError(errorResponse.Error, (int)response.StatusCode);
                else
                    outerErrorToYield = ApiError.FromHttpError($"API Error ({effectiveModelName}) for streaming",
                        (int)response.StatusCode, errorBody);
            }
            else
            {
                isTheOperationOk = true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outerErrorToYield =
                (new ApiError("Streaming content generation was cancelled.", ErrorType.OperationCancelled));
            response?.Dispose();
        }
        catch (HttpRequestException ex)
        {
            outerErrorToYield = (ApiError.FromException(ex,
                                        ErrorType.NetworkError, "Network error during streaming content generation."));
            response?.Dispose();
        }
        catch (IOException ex)
        {
            outerErrorToYield =
                ApiError.FromException(ex, ErrorType.StreamProcessingError, "Error reading from API stream.");
            response?.Dispose();
        }
        catch (Exception ex)
        {
            outerErrorToYield = (ApiError.FromException(ex,
                                        ErrorType.Unknown, "Unexpected error during streaming content generation."));
            response?.Dispose();
        }

        if (isTheOperationOk)
        {
            await foreach (var chunkResult in ProcessStreamInternalAsync(response, cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                yield return chunkResult;
            }
        }
        else
        {
            resultToYield = Result<GeminiGenerateContentResponse, ApiError>.Fail(outerErrorToYield);
            yield return resultToYield;
        }

        response?.Dispose();
    }

     private async IAsyncEnumerable<Result<GeminiGenerateContentResponse, ApiError>> ProcessStreamInternalAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        ApiError error = default;
        IAsyncEnumerable<GeminiGenerateContentResponse> chunks = null;
        try
        {
            // Configure JsonSerializerOptions if needed, e.g., PropertyNameCaseInsensitive = true
            // For now, assuming default options are fine if your C# models match JSON casing.
            chunks = JsonSerializer.DeserializeAsyncEnumerable<GeminiGenerateContentResponse>(
                responseStream,
                new JsonSerializerOptions { /* PropertyNameCaseInsensitive = true, etc. */ },
                cancellationToken);
        }
        catch (JsonException jsonEx) // This can happen if the stream isn't a valid JSON array start
        {
            error= 
                ApiError.DeserializationFailure("Stream was not a valid JSON array or initial deserialization failed.", jsonEx);
        }
        catch (Exception ex) // Other exceptions during DeserializeAsyncEnumerable setup
        {
            error = 
                ApiError.FromException(ex, ErrorType.StreamProcessingError, "Error setting up asynchronous deserialization of stream.");
        }

        if (error != null)
        {
            yield return Result<GeminiGenerateContentResponse, ApiError>.Fail(error); ;
            yield break;
        }

        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            if (chunk != null)
            {
                yield return Result<GeminiGenerateContentResponse, ApiError>.Ok(chunk);
            }
            else
            {
                yield return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                    ApiError.DeserializationFailure("A null chunk was encountered in the stream."));
            }
        }
    }
     
    public IAsyncEnumerable<Result<GeminiGenerateContentResponse, ApiError>> StreamGenerateChatResponseAsync(
        List<GeminiRequestContent> history,
        string newUserPromptText,
        string modelName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newUserPromptText))
        {
            async IAsyncEnumerable<Result<GeminiGenerateContentResponse, ApiError>> ErrorStream()
            {
                yield return Result<GeminiGenerateContentResponse, ApiError>.Fail(
                    new ApiError("New user prompt text cannot be null or empty.", ErrorType.InvalidInput));
            }

            return ErrorStream();
        }

        var newUserParts = new List<GeminiRequestPart> { new GeminiRequestPart { Text = newUserPromptText } };
        return StreamGenerateChatResponseAsync(history, newUserParts, modelName, cancellationToken);
    }

    private T TryDeserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        switch (extension)
        {
            case ".txt": return "text/plain";
            case ".pdf": return "application/pdf";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".png": return "image/png";
            case ".webp": return "image/webp";
            case ".heic": return "image/heic";
            case ".heif": return "image/heif";
            case ".mp3": return "audio/mpeg";
            case ".wav": return "audio/wav";
            case ".mp4": return "video/mp4";
            default: return "application/octet-stream";
        }
    }
}