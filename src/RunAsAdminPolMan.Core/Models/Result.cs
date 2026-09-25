namespace RunAsAdminPolMan.Core.Models;

/// <summary>
/// Represents the result of a business logic operation.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents no error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}

/// <summary>
/// Represents the result of a business logic operation.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the operation failed.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A success result cannot have an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failure result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Fail(Error error) => new(false, error);
}

/// <summary>
/// Represents the result of a business logic operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value returned.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value returned by the operation. Throws if the result is a failure.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    private Result(bool isSuccess, Error error, T? value)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result containing a value.
    /// </summary>
    public static Result<T> Success(T value) => new(true, Error.None, value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static new Result<T> Fail(Error error) => new(false, error, default);

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}