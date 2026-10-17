using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Grit.Modules.Ordering.Infrastructure;

// Used only by dotnet ef: adding a migration needs neither the API host nor a reachable database.
internal sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>();
        OrderingDbContext.Configure(options, NpgsqlDataSource.Create("Host=localhost"));
        return new OrderingDbContext(options.Options);
    }
}
