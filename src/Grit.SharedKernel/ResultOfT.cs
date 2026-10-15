namespace Grit.SharedKernel;

public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, Error? error) : base(error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
