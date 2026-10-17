using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Basket.Features.AddBasketLine;

internal sealed record AddBasketLineCommand(Guid CustomerId, Guid SkuId, int Quantity) : ICommand, IRequest<Result>;
