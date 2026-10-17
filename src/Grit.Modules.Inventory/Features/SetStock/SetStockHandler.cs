using Grit.Modules.Inventory.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Inventory.Features.SetStock;

internal sealed class SetStockHandler(InventoryDbContext db, TimeProvider clock) : IRequestHandler<SetStockCommand, Result>
{
    public async Task<Result> Handle(SetStockCommand command, CancellationToken cancellationToken)
    {
        // A SKU created in the admin has no stock row yet; inserting an empty one first gives every path a row to lock.
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO inventory.stock_items (sku_id, on_hand) VALUES ({command.SkuId}, 0) ON CONFLICT (sku_id) DO NOTHING",
            cancellationToken);
        // The same lock a reservation takes: a checkout in between cannot hold units this check did not see.
        await db.LockStockAsync([command.SkuId], cancellationToken);
        var item = await db.StockItems.SingleAsync(item => item.Id == command.SkuId, cancellationToken);
        var changed = item.SetOnHand(command.OnHand, clock.GetUtcNow());
        if (changed.IsFailure)
        {
            return changed;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
