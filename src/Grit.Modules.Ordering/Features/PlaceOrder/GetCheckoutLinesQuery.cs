using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal sealed record GetCheckoutLinesQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<CheckoutLine>>>;
