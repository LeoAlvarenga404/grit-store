using Grit.SharedKernel;

namespace Grit.Modules.Payments.Domain;

// Anti-corruption port with two implementations: FakePaymentGateway (default) and StripePaymentGateway (design §8).
internal interface IPaymentGateway
{
    Task<Result<string>> AuthorizeAsync(Guid orderId, Money amount, string paymentToken, CancellationToken cancellationToken);

    Task RefundAsync(string authorizationId, CancellationToken cancellationToken);
}
