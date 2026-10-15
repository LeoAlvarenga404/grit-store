using FluentValidation;
using Grit.BuildingBlocks.Behaviors;
using Grit.SharedKernel;

namespace Grit.BuildingBlocks.Tests;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var result = await new ValidationBehavior<TestRequest, Result<int>>([]).Handle(
            new TestRequest(""), _ => Task.FromResult(Result.Success(7)), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(7);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty();

        var result = await new ValidationBehavior<TestRequest, Result<int>>([validator]).Handle(
            new TestRequest("Ana"), _ => Task.FromResult(Result.Success(7)), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(7);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ReturnsFailureOfTWithFieldErrors()
    {
        var wasNextCalled = false;
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty().WithMessage("Name is required.");
        validator.RuleFor(request => request.Name).MinimumLength(3).WithMessage("Name is too short.");

        var result = await new ValidationBehavior<TestRequest, Result<int>>([validator]).Handle(new TestRequest(""), _ =>
        {
            wasNextCalled = true;
            return Task.FromResult(Result.Success(1));
        }, TestContext.Current.CancellationToken);

        wasNextCalled.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("validation.failed");
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.FieldErrors!["name"].ShouldBe(["Name is required.", "Name is too short."]);
    }

    [Fact]
    public async Task Handle_TwoValidators_ReportsEachMessageOnce()
    {
        var required = new InlineValidator<TestRequest>();
        required.RuleFor(request => request.Name).NotEmpty().WithMessage("A");
        var length = new InlineValidator<TestRequest>();
        length.RuleFor(request => request.Name).MinimumLength(3).WithMessage("B");

        var result = await new ValidationBehavior<TestRequest, Result<int>>([required, length]).Handle(
            new TestRequest(""), _ => Task.FromResult(Result.Success(1)), TestContext.Current.CancellationToken);

        result.Error.FieldErrors!["name"].ShouldBe(["A", "B"]);
    }

    [Fact]
    public async Task Handle_InvalidRequestReturningResult_ReturnsFailure()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty();

        var result = await new ValidationBehavior<TestRequest, Result>([validator]).Handle(
            new TestRequest(""), _ => Task.FromResult(Result.Success()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("validation.failed");
    }

    [Fact]
    public async Task Handle_InvalidRequestNotReturningResult_Throws()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty();

        await Should.ThrowAsync<InvalidOperationException>(() => new ValidationBehavior<TestRequest, string>([validator]).Handle(
            new TestRequest(""), _ => Task.FromResult("next"), TestContext.Current.CancellationToken));
    }

    public sealed record TestRequest(string Name);
}
