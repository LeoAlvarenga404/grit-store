using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Inventory.Features.GetStockLevels;

internal sealed record GetStockLevelsQuery(IReadOnlyList<Guid> SkuIds) : IRequest<Result<StockLevelsResponse>>;
