using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Ordering.Features.GetOrder;

internal sealed record GetOrderQuery(Guid CustomerId, Guid OrderId) : IRequest<Result<OrderResponse>>;
