using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Grit.Modules.Catalog.Infrastructure;

internal sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public const string Schema = "catalog";

    public const string Version = "Version";

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, NpgsqlDataSource dataSource) =>
        options
            .UseNpgsql(dataSource, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .UseSnakeCaseNamingConvention();

    public uint VersionOf(Product product) => Entry(product).Property<uint>(Version).CurrentValue;

    // The version the client read becomes the original xmin, so a stale write updates no row and EF throws.
    public async Task<Result<uint>> SaveProductAsync(Product product, uint expectedVersion, CancellationToken cancellationToken)
    {
        var version = Entry(product).Property<uint>(Version);
        version.OriginalValue = expectedVersion;
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CatalogErrors.ProductChanged;
        }

        return version.CurrentValue;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
