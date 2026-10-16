using Grit.Modules.Inventory.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Grit.Modules.Inventory.Infrastructure;

internal sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public const string Schema = "inventory";

    public DbSet<StockItem> StockItems => Set<StockItem>();

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, NpgsqlDataSource dataSource) =>
        options
            .UseNpgsql(dataSource, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .UseSnakeCaseNamingConvention();

    // Row locks in a fixed order: two orders reserving the same SKUs wait for each other instead of deadlocking.
    public Task LockStockAsync(IReadOnlyCollection<Guid> skuIds, CancellationToken cancellationToken) =>
        Database.ExecuteSqlAsync(
            $"SELECT sku_id FROM inventory.stock_items WHERE sku_id = ANY({skuIds.ToArray()}) ORDER BY sku_id FOR UPDATE",
            cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
