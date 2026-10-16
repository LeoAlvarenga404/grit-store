using MassTransit;

namespace Grit.Modules.Ordering.Features.Checkout;

// The saga row; CorrelationId is the order id. The state machine itself arrives in Task 8.
internal sealed class CheckoutState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string PaymentToken { get; set; } = string.Empty;

    public string? AuthorizationId { get; set; }
}
