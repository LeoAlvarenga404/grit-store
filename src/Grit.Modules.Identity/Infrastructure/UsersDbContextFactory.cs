using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Grit.Modules.Identity.Infrastructure;

// Used only by dotnet ef: adding a migration needs neither the API host nor a reachable database.
internal sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>();
        UsersDbContext.Configure(options, NpgsqlDataSource.Create("Host=localhost"));
        return new UsersDbContext(options.Options);
    }
}
