using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Grit.Modules.Inventory.Infrastructure;

// Used only by dotnet ef: adding a migration needs neither the API host nor a reachable database.
internal sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        InventoryDbContext.Configure(options, NpgsqlDataSource.Create("Host=localhost"));
        return new InventoryDbContext(options.Options);
    }
}
