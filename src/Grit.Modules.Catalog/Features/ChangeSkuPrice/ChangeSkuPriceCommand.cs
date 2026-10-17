using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Catalog.Features.ChangeSkuPrice;

internal sealed record ChangeSkuPriceCommand(
    Guid ProductId,
    Guid SkuId,
    decimal PriceAmount,
    string PriceCurrency,
    uint Version) : IRequest<Result<ProductVersionResponse>>, ICommand;
