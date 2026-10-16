using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Basket.Features.ChangeBasketLine;

internal sealed record ChangeBasketLineCommand(Guid CustomerId, Guid SkuId, int Quantity) : ICommand, IRequest<Result>;
