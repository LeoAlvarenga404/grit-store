using Grit.Modules.Inventory.Domain;
using Grit.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

internal static class InventoryStock
{
    public static async Task SetAsync(ApiFactory factory, Guid skuId, int onHand)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        db.StockItems.Add(StockItem.Create(skuId, onHand));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public static async Task<StockItem> GetAsync(ApiFactory factory, Guid skuId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await db.StockItems.AsNoTracking().SingleAsync(item => item.Id == skuId, TestContext.Current.CancellationToken);
    }
}
