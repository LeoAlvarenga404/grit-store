using Grit.SharedKernel;

namespace Grit.Modules.Payments.Domain;

// Anti-corruption port: the fake now, Stripe as the second implementation (design §8).
internal interface IPaymentGateway
{
    Task<Result<string>> AuthorizeAsync(Money amount, string paymentToken, CancellationToken cancellationToken);

    Task RefundAsync(string authorizationId, CancellationToken cancellationToken);
}
