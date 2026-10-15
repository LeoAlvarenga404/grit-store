using System.Text.Json;
using FluentValidation;
using Grit.SharedKernel;
using MediatR;

namespace Grit.BuildingBlocks.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var results = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(request, cancellationToken)));
        var fieldErrors = results
            .SelectMany(result => result.Errors)
            .GroupBy(failure => ToJsonName(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        if (fieldErrors.Count == 0)
        {
            return await next(cancellationToken);
        }

        var error = new Error("validation.failed", "One or more validation errors occurred.", ErrorType.Validation)
        {
            FieldErrors = fieldErrors,
        };
        return Failure(error);
    }

    // Keys follow the camelCase JSON of the request body: "address.zipCode", not "Address.ZipCode".
    private static string ToJsonName(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));

    private static TResponse Failure(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (!typeof(TResponse).IsGenericType || typeof(TResponse).GetGenericTypeDefinition() != typeof(Result<>))
        {
            throw new InvalidOperationException($"{typeof(TRequest).Name} must return Result or Result<T> to be validated.");
        }

        // Result<T>.Failure is generic in T, which only exists here as TResponse's type argument.
        var failure = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
            .MakeGenericMethod(typeof(TResponse).GetGenericArguments()[0]);
        return (TResponse)failure.Invoke(null, [error])!;
    }
}
