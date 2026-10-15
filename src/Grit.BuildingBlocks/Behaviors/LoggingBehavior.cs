using System.Diagnostics;
using Grit.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Grit.BuildingBlocks.Behaviors;

public sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var startedAt = Stopwatch.GetTimestamp();
        var response = await next(cancellationToken);
        var elapsedInMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (response is Result { IsFailure: true } failure)
        {
            LogFailure(logger, typeof(TRequest).Name, failure.Error.Code, elapsedInMilliseconds);
            return response;
        }

        LogSuccess(logger, typeof(TRequest).Name, elapsedInMilliseconds);
        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Request} succeeded in {ElapsedInMilliseconds} ms")]
    private static partial void LogSuccess(ILogger logger, string request, double elapsedInMilliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Request} failed with {ErrorCode} in {ElapsedInMilliseconds} ms")]
    private static partial void LogFailure(ILogger logger, string request, string errorCode, double elapsedInMilliseconds);
}
