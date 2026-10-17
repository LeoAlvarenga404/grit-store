using Grit.Modules.Payments.Domain;
using Grit.SharedKernel;
using Stripe;

namespace Grit.Modules.Payments.Infrastructure;

// The checkout's paymentToken is a Stripe PaymentMethod id (pm_card_visa in test mode). The charge is captured at once:
// the saga has no capture step, and a cancel refunds it.
internal sealed class StripePaymentGateway(StripeClient stripe) : IPaymentGateway
{
    public async Task<Result<string>> AuthorizeAsync(Guid orderId, Money amount, string paymentToken, CancellationToken cancellationToken)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = decimal.ToInt64(amount.Amount * 100),
            Currency = amount.Currency.ToLowerInvariant(),
            PaymentMethod = paymentToken,
            Confirm = true,
            // The checkout cannot send the customer to a 3-D Secure page, so Stripe declines it instead of waiting. Stripe rejects
            // error_on_requires_action on intents with dashboard-managed (automatic) payment methods, so the type is explicit.
            ErrorOnRequiresAction = true,
            AllowedPaymentMethodTypes = ["card"],
            Metadata = new Dictionary<string, string> { ["order_id"] = orderId.ToString() },
        };

        // A redelivered AuthorizePayment gets the first answer back instead of a second charge.
        var request = new RequestOptions { IdempotencyKey = $"authorize-{orderId}" };
        try
        {
            var intent = await stripe.V1.PaymentIntents.CreateAsync(options, request, cancellationToken);
            // ponytail: a confirmed card intent ends succeeded or in an error; any other status is declined without cancelling the intent.
            return intent.Status == "succeeded" ? intent.Id : PaymentsErrors.DeclinedBy(intent.Status);
        }
        catch (StripeException exception) when (IsCustomerFault(exception.StripeError))
        {
            return PaymentsErrors.DeclinedBy(exception.StripeError.DeclineCode ?? exception.StripeError.Code);
        }
    }

    public Task RefundAsync(string authorizationId, CancellationToken cancellationToken) =>
        stripe.V1.Refunds.CreateAsync(
            new RefundCreateOptions { PaymentIntent = authorizationId },
            new RequestOptions { IdempotencyKey = $"refund-{authorizationId}" },
            cancellationToken);

    // A declined card, or a payment method id Stripe does not know (customer input). Anything else, such as a wrong API key
    // (also an invalid_request_error), is an infrastructure failure and throws.
    private static bool IsCustomerFault(StripeError? error) =>
        error?.Type == "card_error" || error?.Param == "payment_method";
}
