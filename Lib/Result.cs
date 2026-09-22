namespace Lib;

public struct Result<T>
{
    public bool IsSuccess => TheError == null;
    public bool IsFailure => !IsSuccess;

    public Exception? TheError { get; private set; } = null;

    private T? _value = default;

    public T Value => _value ?? throw new InvalidOperationException($"Cannot convert Result to {typeof(T)}.");
    
    private Result(T v)
    {
        _value = v;
    }

    private Result(Exception err)
    {
        TheError = err;
    }

    public static Result<T1> Success<T1>(T1 v) => new(v);

    public static Result<T1> Error<T1>(Exception e) => new(e);

    public static Result<T1> Error<T1>(string msg) => Error<T1>(new Exception(msg));

    public static implicit operator Result<T>(Exception exception)
        => new(exception);

    public static implicit operator Result<T>(T v)
        => new(v);

    public static implicit operator bool(Result<T> result)
        => result.IsSuccess;

    public (T?, Exception?) Decompose => (_value, TheError);

    public static implicit operator T(Result<T> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Cannot convert Result to {typeof(T)}.");
        }

        return result._value!;
    }
}