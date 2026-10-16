using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal sealed record PlaceOrderCommand(Guid CustomerId, string IdempotencyKey, string PaymentToken, decimal ExpectedSubtotalAmount, IReadOnlyList<CheckoutLine> Lines) : ICommand, IRequest<Result<PlaceOrderResponse>>;
