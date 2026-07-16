using SmartTask.Domain.Common.Errors;

namespace SmartTask.Domain.Common;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static SmartTask.Domain.Common.Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static SmartTask.Domain.Common.Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}
