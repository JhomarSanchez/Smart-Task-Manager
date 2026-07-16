using System;
using SmartTask.Domain.Common.Errors;

namespace SmartTask.Domain.Common;

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static implicit operator Result<TValue>(TValue? value) =>
        value is null ? Failure<TValue>(Error.Failure("NullValue", "The provided value is null.")) : Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
