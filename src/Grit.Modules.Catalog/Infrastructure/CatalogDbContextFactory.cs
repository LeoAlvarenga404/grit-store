using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Grit.Modules.Catalog.Infrastructure;

// Used only by dotnet ef: adding a migration needs neither the API host nor a reachable database.
internal sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        CatalogDbContext.Configure(options, NpgsqlDataSource.Create("Host=localhost"));
        return new CatalogDbContext(options.Options);
    }
}
