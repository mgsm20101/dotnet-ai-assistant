namespace AiAssistant.Application.Common;

/// <summary>Discriminated union for operation outcomes — no exception for expected errors.</summary>
public sealed class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    private Result(T value) => Value = value;
    private Result(string error) => Error = error;

    public static Result<T> Ok(T value)       => new(value);
    public static Result<T> Fail(string error) => new(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> fn) =>
        IsSuccess ? Result<TOut>.Ok(fn(Value!)) : Result<TOut>.Fail(Error!);
}
