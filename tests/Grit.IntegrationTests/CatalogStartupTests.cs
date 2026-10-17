using System.Net;
using Grit.Modules.Catalog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class CatalogStartupTests(ApiFactory factory)
{
    [Fact]
    public async Task ApiStartup_Development_AppliesTheCatalogMigrations()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var applied = await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        var pending = await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

        applied.ShouldContain(migration => migration.EndsWith("_InitialCatalog", StringComparison.Ordinal));
        pending.ShouldBeEmpty();
    }

    // Port 1 refuses connections: a Production start-up that tried to migrate would throw while creating the client.
    [Fact]
    public async Task ApiStartup_Production_DoesNotTouchTheDatabase()
    {
        using var production = factory.WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("ConnectionStrings:grit", "Host=127.0.0.1;Port=1;Database=none;Timeout=1"));

        var response = await production.CreateClient().GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
