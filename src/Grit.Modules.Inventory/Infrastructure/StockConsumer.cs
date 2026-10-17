using Grit.Modules.Inventory.Contracts;
using Grit.Modules.Inventory.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Inventory.Infrastructure;

internal sealed class StockConsumer(InventoryDbContext db, TimeProvider clock)
    : IConsumer<ReserveStock>, IConsumer<CommitStock>, IConsumer<ReleaseStock>, IConsumer<ShipStock>
{
    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var message = context.Message;
        var skuIds = message.Lines.Select(line => line.SkuId).ToList();
        await db.LockStockAsync(skuIds, context.CancellationToken);
        var items = await db.StockItems
            .Where(item => skuIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, context.CancellationToken);
        var now = clock.GetUtcNow();

        // All lines or none: checking first means a failed order leaves no partial reservation behind.
        if (!message.Lines.All(line => items.TryGetValue(line.SkuId, out var item) && item.CanReserve(message.OrderId, line.Quantity, now)))
        {
            await context.Publish(new StockReservationFailed(message.OrderId, InventoryErrors.InsufficientStock.Code), context.CancellationToken);
            return;
        }

        foreach (var line in message.Lines)
        {
            items[line.SkuId].Reserve(message.OrderId, line.Quantity, now);
        }

        await context.Publish(new StockReserved(message.OrderId), context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<CommitStock> context)
    {
        foreach (var item in await ItemsReservedFor(context.Message.OrderId, context.CancellationToken))
        {
            item.Commit(context.Message.OrderId);
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<ReleaseStock> context)
    {
        foreach (var item in await ItemsReservedFor(context.Message.OrderId, context.CancellationToken))
        {
            item.Release(context.Message.OrderId);
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    // Locked like a reservation: an admin stock change in between would otherwise be overwritten by this on-hand update.
    public async Task Consume(ConsumeContext<ShipStock> context)
    {
        var orderId = context.Message.OrderId;
        var skuIds = await db.StockItems
            .Where(item => item.Reservations.Any(reservation => reservation.OrderId == orderId))
            .Select(item => item.Id)
            .ToListAsync(context.CancellationToken);
        await db.LockStockAsync(skuIds, context.CancellationToken);
        foreach (var item in await db.StockItems.Where(item => skuIds.Contains(item.Id)).ToListAsync(context.CancellationToken))
        {
            item.Ship(orderId);
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    private Task<List<StockItem>> ItemsReservedFor(Guid orderId, CancellationToken cancellationToken) =>
        db.StockItems
            .Where(item => item.Reservations.Any(reservation => reservation.OrderId == orderId))
            .ToListAsync(cancellationToken);
}
