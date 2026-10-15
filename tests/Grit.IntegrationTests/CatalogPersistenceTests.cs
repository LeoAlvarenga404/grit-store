using System.Net;
using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class CatalogPersistenceTests(ApiFactory factory)
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

    [Fact]
    public async Task SaveChanges_NewProductWithSku_RoundTripsAttributesPriceAndStampsBothRoots()
    {
        var before = DateTimeOffset.UtcNow.AddMilliseconds(-1);
        var (id, version) = await SaveNewProductAsync(product => product.AddSku(Attributes("cor", "azul"), Reais(1_000_000_000_000m)));
        var after = DateTimeOffset.UtcNow.AddMilliseconds(1);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await db.Products.Include(saved => saved.Skus).SingleAsync(saved => saved.Id == id, TestContext.Current.CancellationToken);
        product.Attributes["capacidade"].ShouldBe("2");
        var sku = product.Skus.ShouldHaveSingleItem();
        sku.Attributes["cor"].ShouldBe("azul");
        sku.Price.ShouldBe(Reais(1_000_000_000_000m));
        var row = await ReadRowAsync(id);
        row.CreatedAt.ShouldBeInRange(before, after);
        row.UpdatedAt.ShouldBe(row.CreatedAt);
        row.Version.ShouldBe(version);
        version.ShouldNotBe(0u);
        var category = await db.Categories
            .Where(saved => saved.Id == product.CategoryId)
            .Select(saved => new
            {
                CreatedAt = EF.Property<DateTimeOffset>(saved, AuditInterceptor.CreatedAt),
                UpdatedAt = EF.Property<DateTimeOffset>(saved, AuditInterceptor.UpdatedAt),
            })
            .SingleAsync(TestContext.Current.CancellationToken);
        category.CreatedAt.ShouldBeInRange(before, after);
        category.UpdatedAt.ShouldBe(category.CreatedAt);
    }

    [Fact]
    public async Task SaveProductAsync_Rename_StampsUpdatedAtAndKeepsCreatedAt()
    {
        var (id, version) = await SaveNewProductAsync(_ => { });
        var before = await ReadRowAsync(id);

        var renamed = await RenameAsync(id, UniqueName("Renomeada"), version);

        renamed.IsSuccess.ShouldBeTrue();
        var after = await ReadRowAsync(id);
        after.Version.ShouldBe(renamed.Value);
        after.UpdatedAt.ShouldBeGreaterThan(before.UpdatedAt);
        after.CreatedAt.ShouldBe(before.CreatedAt);
    }

    [Fact]
    public async Task SaveProductAsync_SkuAddedToSavedProduct_StampsTheProductAndChangesItsVersion()
    {
        var (id, version) = await SaveNewProductAsync(_ => { });
        var before = await ReadRowAsync(id);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await db.Products.Include(saved => saved.Skus).SingleAsync(saved => saved.Id == id, TestContext.Current.CancellationToken);
        product.AddSku(Attributes("cor", "verde"), Reais(10m));
        var saved = await db.SaveProductAsync(product, version, TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue();
        saved.Value.ShouldNotBe(version);
        var after = await ReadRowAsync(id);
        after.Version.ShouldBe(saved.Value);
        after.UpdatedAt.ShouldBeGreaterThan(before.UpdatedAt);
        after.CreatedAt.ShouldBe(before.CreatedAt);
    }

    [Fact]
    public async Task SaveProductAsync_SkuPriceChanged_StampsTheProductAndChangesItsVersion()
    {
        var (id, version) = await SaveNewProductAsync(product => product.AddSku(Attributes("cor", "azul"), Reais(10m)));
        var before = await ReadRowAsync(id);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await db.Products.Include(saved => saved.Skus).SingleAsync(saved => saved.Id == id, TestContext.Current.CancellationToken);
        product.ChangeSkuPrice(product.Skus[0].Id, Reais(12.5m));
        var saved = await db.SaveProductAsync(product, version, TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue();
        var after = await ReadRowAsync(id);
        after.Version.ShouldBe(saved.Value);
        after.Version.ShouldNotBe(before.Version);
        after.UpdatedAt.ShouldBeGreaterThan(before.UpdatedAt);
        after.CreatedAt.ShouldBe(before.CreatedAt);
    }

    [Fact]
    public async Task SaveProductAsync_StaleVersion_ReturnsProductChangedAndKeepsTheFirstWrite()
    {
        var (id, version) = await SaveNewProductAsync(_ => { });
        var firstName = UniqueName("Primeira");
        (await RenameAsync(id, firstName, version)).IsSuccess.ShouldBeTrue();

        var stale = await RenameAsync(id, UniqueName("Segunda"), version);

        stale.Error.ShouldBe(CatalogErrors.ProductChanged);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var name = await db.Products.Where(product => product.Id == id).Select(product => product.Name).SingleAsync(TestContext.Current.CancellationToken);
        name.ShouldBe(firstName);
    }

    [Fact]
    public async Task SaveProductAsync_SkuAddedWithStaleVersion_ReturnsProductChangedAndSavesNoSku()
    {
        var (id, version) = await SaveNewProductAsync(_ => { });
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await db.Products.Include(saved => saved.Skus).SingleAsync(saved => saved.Id == id, TestContext.Current.CancellationToken);

        (await RenameAsync(id, UniqueName("Renomeada"), version)).IsSuccess.ShouldBeTrue();
        product.AddSku(Attributes("cor", "azul"), Reais(10m));
        var result = await db.SaveProductAsync(product, version, TestContext.Current.CancellationToken);

        result.Error.ShouldBe(CatalogErrors.ProductChanged);
        await using var readScope = factory.Services.CreateAsyncScope();
        var skus = await readScope.ServiceProvider.GetRequiredService<CatalogDbContext>().Products
            .Where(saved => saved.Id == id)
            .SelectMany(saved => saved.Skus)
            .CountAsync(TestContext.Current.CancellationToken);
        skus.ShouldBe(0);
    }

    private async Task<(ProductId Id, uint Version)> SaveNewProductAsync(Action<Product> addSkus)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var category = Category.Create(UniqueName("Categoria")).Value;
        var product = Product.Create(category.Id, UniqueName("Barraca"), "Leve.", Attributes("capacidade", "2")).Value;
        addSkus(product);
        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (product.Id, db.VersionOf(product));
    }

    private async Task<Result<uint>> RenameAsync(ProductId id, string name, uint expectedVersion)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await db.Products.SingleAsync(saved => saved.Id == id, TestContext.Current.CancellationToken);
        product.Update(product.CategoryId, name, product.Description, product.Attributes);
        return await db.SaveProductAsync(product, expectedVersion, TestContext.Current.CancellationToken);
    }

    // A new scope and DbContext, so the row is read as committed, not from the change tracker.
    private async Task<(DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, uint Version)> ReadRowAsync(ProductId id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var row = await db.Products
            .Where(product => product.Id == id)
            .Select(product => new
            {
                CreatedAt = EF.Property<DateTimeOffset>(product, AuditInterceptor.CreatedAt),
                UpdatedAt = EF.Property<DateTimeOffset>(product, AuditInterceptor.UpdatedAt),
                Version = EF.Property<uint>(product, CatalogDbContext.Version),
            })
            .SingleAsync(TestContext.Current.CancellationToken);
        return (row.CreatedAt, row.UpdatedAt, row.Version);
    }

    private static Dictionary<string, string> Attributes(string key, string value) => new() { [key] = value };

    private static Money Reais(decimal amount) => Money.Create(amount, "BRL").Value;

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";
}
