// // Copyright ©  2025 no-pact
// // Author: canka

namespace GeminiSharp.Common;

public enum ErrorType
{
    NetworkError,
    HttpError,
    ApiLogicError,
    DeserializationError,
    SerializationError,
    InvalidInput,
    FileNotFound,
    UploadFailed,
    OperationCancelled,
    ImageGenerationFailed,
    Base64DecodingError,
    FileSaveError,
    ConfigurationError, 
    StreamProcessingError, 
    Unknown
}