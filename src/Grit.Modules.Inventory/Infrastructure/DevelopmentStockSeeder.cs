using Grit.Modules.Catalog.Contracts;
using Grit.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grit.Modules.Inventory.Infrastructure;

// Starts after the catalog seed (Catalog registers its hosted service first), so every seeded SKU gets stock on the first run.
internal sealed class DevelopmentStockSeeder(IServiceScopeFactory scopes) : IHostedService
{
    public const int UnitsPerSku = 10;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogReader>();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var stocked = await db.StockItems.Select(item => item.Id).ToListAsync(cancellationToken);
        var missing = (await catalog.GetSkuIdsAsync(cancellationToken)).Except(stocked);
        db.StockItems.AddRange(missing.Select(skuId => StockItem.Create(skuId, UnitsPerSku)));
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
