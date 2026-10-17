using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Ordering.Features.ShipOrder;

internal sealed record ShipOrderCommand(Guid OrderId) : ICommand, IRequest<Result>;
