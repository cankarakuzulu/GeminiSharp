// // Copyright ©  2025 no-pact
// // Author: canka

using System;

namespace GeminiSharp.Common;

// You can put this in a new file, e.g., Result.cs, or within your Models file.

public readonly struct Result<TSuccess, TError>
{
    public bool IsSuccess { get; }
    public readonly bool isDefault = true;
    public bool IsFailure => !IsSuccess;

    private readonly TSuccess _value;
    public TSuccess Value => IsSuccess ? _value : throw new InvalidOperationException("Cannot access Value when Result is a failure.");

    private readonly TError _error;
    public TError Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access Error when Result is a success.");

    private Result(TSuccess value)
    {
        IsSuccess = true;
        _value = value;
        _error = default; // Should be the default value for TError (e.g., null for classes)
        isDefault = false;
    }

    private Result(TError error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error), "Error value cannot be null for a failure Result.");
        IsSuccess = false;
        _value = default;
        _error = error;
        isDefault = false;
    }

    public static Result<TSuccess, TError> Ok(TSuccess value) => new Result<TSuccess, TError>(value);
    public static Result<TSuccess, TError> Fail(TError error) => new Result<TSuccess, TError>(error);

    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<TError, TResult> onFailure)
    {
        if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));

        return IsSuccess ? onSuccess(_value) : onFailure(_error);
    }
    
    public void Match(Action<TSuccess> onSuccess, Action<TError> onFailure)
    {
        if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));

        if (IsSuccess)
        {
            onSuccess(_value);
        }
        else
        {
            onFailure(_error);
        }
    }

    // Optional: Implicit conversion from TSuccess to Result<TSuccess, TError>
    // public static implicit operator Result<TSuccess, TError>(TSuccess value) => Ok(value);
    // Optional: Implicit conversion from TError to Result<TSuccess, TError>
    // public static implicit operator Result<TSuccess, TError>(TError error) => Fail(error);
}