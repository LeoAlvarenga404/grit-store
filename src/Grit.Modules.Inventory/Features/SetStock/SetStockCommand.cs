using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Inventory.Features.SetStock;

internal sealed record SetStockCommand(Guid SkuId, int OnHand) : IRequest<Result>, ICommand;
