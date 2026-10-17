using System.Net;
using Grit.Modules.Payments.Infrastructure;
using Grit.SharedKernel;
using Stripe;

namespace Grit.Modules.Payments.Tests.Infrastructure;

// Response bodies follow the Stripe API reference (PaymentIntent, Refund, error object); no test reaches the network.
public class StripePaymentGatewayTests
{
    private const string SucceededIntent = """
        {"id":"pi_3Q8succeeded","object":"payment_intent","amount":12990,"currency":"brl","status":"succeeded","livemode":false}
        """;

    private const string ProcessingIntent = """
        {"id":"pi_3Q8processing","object":"payment_intent","amount":12990,"currency":"brl","status":"processing","livemode":false}
        """;

    private const string CardDeclined = """
        {"error":{"type":"card_error","code":"card_declined","decline_code":"insufficient_funds","message":"Your card has insufficient funds."}}
        """;

    private const string UnknownPaymentMethod = """
        {"error":{"type":"invalid_request_error","code":"resource_missing","param":"payment_method","message":"No such PaymentMethod: 'tok_visa'"}}
        """;

    private const string InvalidApiKey = """
        {"error":{"type":"invalid_request_error","message":"Invalid API Key provided: sk_test_****stub"}}
        """;

    private const string SucceededRefund = """
        {"id":"re_3Q8refund","object":"refund","amount":12990,"currency":"brl","payment_intent":"pi_3Q8succeeded","status":"succeeded"}
        """;

    private static readonly Money Total = Money.Create(129.90m, "BRL").Value;

    [Fact]
    public async Task AuthorizeAsync_Succeeded_ReturnsThePaymentIntentId()
    {
        var stub = new StripeStub(HttpStatusCode.OK, SucceededIntent);

        var result = await AuthorizeAsync(stub, Guid.NewGuid());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("pi_3Q8succeeded");
    }

    [Fact]
    public async Task AuthorizeAsync_Always_ConfirmsACardChargeInCentsKeyedByTheOrder()
    {
        var stub = new StripeStub(HttpStatusCode.OK, SucceededIntent);
        var orderId = Guid.NewGuid();

        await AuthorizeAsync(stub, orderId);

        stub.Request!.Method.ShouldBe(HttpMethod.Post);
        stub.Request.RequestUri!.AbsolutePath.ShouldBe("/v1/payment_intents");
        var form = stub.RequestForm.Split('&');
        form.ShouldContain("amount=12990");
        form.ShouldContain("currency=brl");
        form.ShouldContain("payment_method=pm_card_visa");
        form.ShouldContain("confirm=true");
        form.ShouldContain("error_on_requires_action=true");
        form.ShouldContain("allowed_payment_method_types[0]=card");
        form.ShouldNotContain(field => field.StartsWith("automatic_payment_methods", StringComparison.Ordinal));
        form.ShouldContain($"metadata[order_id]={orderId}");
        stub.Header("Idempotency-Key").ShouldBe($"authorize-{orderId}");
    }

    [Fact]
    public async Task AuthorizeAsync_CardDeclined_ReturnsDeclinedWithTheDeclineCode()
    {
        var stub = new StripeStub(HttpStatusCode.PaymentRequired, CardDeclined);

        var result = await AuthorizeAsync(stub, Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("payments.declined");
        result.Error.Message.ShouldContain("insufficient_funds");
    }

    [Fact]
    public async Task AuthorizeAsync_UnknownPaymentMethod_ReturnsDeclined()
    {
        var stub = new StripeStub(HttpStatusCode.BadRequest, UnknownPaymentMethod);

        var result = await AuthorizeAsync(stub, Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("payments.declined");
        result.Error.Message.ShouldContain("resource_missing");
    }

    [Fact]
    public async Task AuthorizeAsync_IntentNotSucceeded_ReturnsDeclinedWithTheStatus()
    {
        var stub = new StripeStub(HttpStatusCode.OK, ProcessingIntent);

        var result = await AuthorizeAsync(stub, Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("processing");
    }

    [Fact]
    public async Task AuthorizeAsync_InvalidApiKey_ThrowsInsteadOfDeclining()
    {
        var stub = new StripeStub(HttpStatusCode.Unauthorized, InvalidApiKey);

        await Should.ThrowAsync<StripeException>(() => AuthorizeAsync(stub, Guid.NewGuid()));
    }

    [Fact]
    public async Task RefundAsync_Always_RefundsThePaymentIntentKeyedByIt()
    {
        var stub = new StripeStub(HttpStatusCode.OK, SucceededRefund);

        await new StripePaymentGateway(stub.Client()).RefundAsync("pi_3Q8succeeded", TestContext.Current.CancellationToken);

        stub.Request!.Method.ShouldBe(HttpMethod.Post);
        stub.Request.RequestUri!.AbsolutePath.ShouldBe("/v1/refunds");
        stub.RequestForm.ShouldBe("payment_intent=pi_3Q8succeeded");
        stub.Header("Idempotency-Key").ShouldBe("refund-pi_3Q8succeeded");
    }

    private static Task<Result<string>> AuthorizeAsync(StripeStub stub, Guid orderId) =>
        new StripePaymentGateway(stub.Client()).AuthorizeAsync(orderId, Total, "pm_card_visa", TestContext.Current.CancellationToken);
}
