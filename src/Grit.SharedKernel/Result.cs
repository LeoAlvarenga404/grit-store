namespace Grit.SharedKernel;

public class Result
{
    private readonly Error? _error;

    private protected Result(Error? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new Result(null);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(error);
    }

    public static Result<T> Success<T>(T value) => new(value, null);

    public static Result<T> Failure<T>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<T>(default, error);
    }

    public static implicit operator Result(Error error) => Failure(error);
}
