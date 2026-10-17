using Grit.Modules.Inventory.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Inventory.Features.GetStockLevels;

internal sealed class GetStockLevelsHandler(InventoryDbContext db, TimeProvider clock)
    : IRequestHandler<GetStockLevelsQuery, Result<StockLevelsResponse>>
{
    public async Task<Result<StockLevelsResponse>> Handle(GetStockLevelsQuery query, CancellationToken cancellationToken)
    {
        var items = await db.StockItems
            .AsNoTracking()
            .Where(item => query.SkuIds.Contains(item.Id))
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        return new StockLevelsResponse([.. items.Select(item => new StockLevelResponse(item.Id, item.OnHand, item.Held(now), item.Available(now)))]);
    }
}
