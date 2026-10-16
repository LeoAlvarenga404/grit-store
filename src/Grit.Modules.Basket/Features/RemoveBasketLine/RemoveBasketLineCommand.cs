using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Basket.Features.RemoveBasketLine;

internal sealed record RemoveBasketLineCommand(Guid CustomerId, Guid SkuId) : ICommand, IRequest<Result>;
