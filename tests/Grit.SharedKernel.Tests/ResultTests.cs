namespace Grit.SharedKernel.Tests;

public class ResultTests
{
    private static readonly Error SomeError = new("some.code", "Some message.", ErrorType.Conflict);

    [Fact]
    public void Success_IsSuccess()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Success_ReadingError_Throws()
    {
        var result = Result.Success();

        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_ExposesTheError()
    {
        var result = Result.Failure(SomeError);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBeSameAs(SomeError);
    }

    [Fact]
    public void Failure_NullError_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void ImplicitConversionFromError_CreatesFailure()
    {
        Result result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBeSameAs(SomeError);
    }

    [Fact]
    public void SuccessWithValue_ExposesTheValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void SuccessWithValue_ReadingError_Throws()
    {
        var result = Result.Success(42);

        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void SuccessWithNullValue_IsAllowed()
    {
        var result = Result.Success<string?>(null);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void FailureWithValueType_ReadingValue_Throws()
    {
        var result = Result.Failure<int>(SomeError);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBeSameAs(SomeError);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void FailureWithReferenceType_ReadingValue_Throws()
    {
        var result = Result.Failure<string>(SomeError);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBeSameAs(SomeError);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void FailureWithValueType_NullError_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure<int>(null!));
    }

    [Fact]
    public void ImplicitConversionFromValue_CreatesSuccess()
    {
        Result<string> result = "abc";

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe("abc");
    }

    [Fact]
    public void ImplicitConversionFromErrorToResultWithValue_CreatesFailure()
    {
        Result<string> result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBeSameAs(SomeError);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }
}
