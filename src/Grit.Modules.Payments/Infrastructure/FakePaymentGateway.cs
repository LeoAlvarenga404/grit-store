using Grit.Modules.Payments.Domain;
using Grit.SharedKernel;

namespace Grit.Modules.Payments.Infrastructure;

internal sealed class FakePaymentGateway : IPaymentGateway
{
    public const string DeclinedToken = "tok_decline";

    public Task<Result<string>> AuthorizeAsync(Guid orderId, Money amount, string paymentToken, CancellationToken cancellationToken) =>
        Task.FromResult(paymentToken == DeclinedToken
            ? Result.Failure<string>(PaymentsErrors.Declined)
            : Result.Success($"fake_{Guid.NewGuid():N}"));

    public Task RefundAsync(string authorizationId, CancellationToken cancellationToken) => Task.CompletedTask;
}
