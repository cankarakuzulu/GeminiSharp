// // Copyright ©  2025 no-pact
// // Author: canka

using GeminiSharp.Models.Shared;
using System;

namespace GeminiSharp.Common;

public class ApiError
{
 public string Message { get; }
    public ErrorType ErrorType { get; }
    public int? HttpStatusCode { get; }
    public string ApiErrorCode { get; } // Could be GeminiError.Status or similar
    public string Details { get; }

    public ApiError(string message, ErrorType errorType, int? httpStatusCode = null, string apiErrorCode = null, string details = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ErrorType = errorType;
        HttpStatusCode = httpStatusCode;
        ApiErrorCode = apiErrorCode;
        Details = details;
    }

    public override string ToString()
    {
        return $"ApiError [{ErrorType}]: {Message} (HTTP: {HttpStatusCode?.ToString() ?? "N/A"}, API Code: {ApiErrorCode ?? "N/A"}, Details: {Details?.Truncate(200) ?? "N/A"})";
    }

    // Factory methods
    public static ApiError FromHttpError(string message, int statusCode, string responseBody = null, string apiErrorCode = null) =>
        new ApiError(message, ErrorType.HttpError, statusCode, apiErrorCode, responseBody);

    public static ApiError FromGeminiError(GeminiError geminiError, int statusCode) =>
        new ApiError(geminiError.Message, ErrorType.ApiLogicError, statusCode, geminiError.Status, $"Gemini Error Code: {geminiError.Code}");

    public static ApiError FromException(Exception ex, ErrorType errorType = ErrorType.Unknown, string customMessage = null) =>
        new ApiError(customMessage ?? ex.Message, errorType, details: ex.ToString());

    public static ApiError DeserializationFailure(string details, Exception ex = null) =>
        new ApiError("Failed to deserialize API response.", ErrorType.DeserializationError, details: details + (ex != null ? $"\nException: {ex.Message}" : ""));

    public static ApiError SerializationFailure(string details, Exception ex = null) =>
        new ApiError("Failed to serialize request.", ErrorType.SerializationError, details: details + (ex != null ? $"\nException: {ex.Message}" : ""));

}