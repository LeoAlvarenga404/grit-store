using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Ordering.Features.CancelOrder;

internal sealed record CancelOrderCommand(Guid CustomerId, Guid OrderId) : ICommand, IRequest<Result>;
