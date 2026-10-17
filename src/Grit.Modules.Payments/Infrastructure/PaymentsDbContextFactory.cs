using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Grit.Modules.Payments.Infrastructure;

// Used only by dotnet ef: adding a migration needs neither the API host nor a reachable database.
internal sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>();
        PaymentsDbContext.Configure(options, NpgsqlDataSource.Create("Host=localhost"));
        return new PaymentsDbContext(options.Options);
    }
}
