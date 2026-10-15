using System.Transactions;
using Grit.SharedKernel;
using MediatR;

namespace Grit.BuildingBlocks.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (request is not ICommand)
        {
            return await next(cancellationToken);
        }

        // An ambient transaction lets any module DbContext enlist without this behavior knowing its type.
        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        var response = await next(cancellationToken);
        if (response is not Result { IsFailure: true })
        {
            scope.Complete();
        }

        return response;
    }
}
