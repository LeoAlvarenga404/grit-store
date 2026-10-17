using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Grit.IntegrationTests;

// The migration tests build a database of their own up to the migration before the colours, write rows the old way,
// then migrate. The first SKU of the shirt by id is azul, although preto is inserted first; the sock has no image.
public class CatalogImagePersistenceTests(ApiFactory api)
{
    private const string BeforeColors = "AuditUsers";

    private const string ShirtId = "0192f0c1-0000-7000-8000-000000000010";

    private const string CapId = "0192f0c1-0000-7000-8000-000000000011";

    private const string SockId = "0192f0c1-0000-7000-8000-000000000012";

    private const string OldRows = $$"""
        INSERT INTO catalog.categories (id, name, slug, created_at, updated_at)
        VALUES ('0192f0c1-0000-7000-8000-000000000001', 'Camisetas', 'camisetas', now(), now());
        INSERT INTO catalog.products (id, category_id, name, slug, description, attributes, image_keys, created_at, updated_at)
        VALUES ('{{ShirtId}}', '0192f0c1-0000-7000-8000-000000000001', 'Camiseta', 'camiseta', 'Leve.', '{}',
                ARRAY['products/{{ShirtId}}/a.webp', 'products/{{ShirtId}}/b.webp'], now(), now()),
               ('{{CapId}}', '0192f0c1-0000-7000-8000-000000000001', 'Boné', 'bone', 'Leve.', '{}',
                ARRAY['products/{{CapId}}/c.webp'], now(), now()),
               ('{{SockId}}', '0192f0c1-0000-7000-8000-000000000001', 'Meia', 'meia', 'Leve.', '{}',
                ARRAY[]::text[], now(), now());
        INSERT INTO catalog.skus (id, product_id, attributes, price_amount, price_currency)
        VALUES ('0192f0c1-0000-7000-8000-000000000022', '{{ShirtId}}', '{"cor": "preto"}', 10, 'BRL'),
               ('0192f0c1-0000-7000-8000-000000000021', '{{ShirtId}}', '{"cor": "azul"}', 10, 'BRL');
        """;

    [Fact]
    public async Task SaveChanges_ImagesOfTwoColorsAndNone_RoundTripsKeysColorsAndOrder()
    {
        var category = Category.Create(CatalogAdminApi.UniqueName("Camisetas")).Value;
        var product = Product.Create(category.Id, CatalogAdminApi.UniqueName("Camiseta"), "Leve.", new Dictionary<string, string>()).Value;
        product.AddSku(new Dictionary<string, string> { ["cor"] = "azul" }, Money.Create(10m, "BRL").Value);
        product.AddSku(new Dictionary<string, string> { ["tamanho"] = "U" }, Money.Create(10m, "BRL").Value);
        var prefix = Product.ImageKeyPrefix(product.Id);
        product.AddImage(prefix + "a.png", "azul").IsSuccess.ShouldBeTrue();
        product.AddImage(prefix + "b.png", null).IsSuccess.ShouldBeTrue();
        product.AddImage(prefix + "c.png", "azul").IsSuccess.ShouldBeTrue();
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            db.Categories.Add(category);
            db.Products.Add(product);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readScope = api.Services.CreateAsyncScope();
        var saved = await readScope.ServiceProvider.GetRequiredService<CatalogDbContext>().Products.AsNoTracking()
            .SingleAsync(saved => saved.Id == product.Id, TestContext.Current.CancellationToken);
        saved.Images.ShouldBe([new ProductImage(prefix + "a.png", "azul"), new ProductImage(prefix + "b.png", null), new ProductImage(prefix + "c.png", "azul")]);
        var json = await ScalarAsync<string>(api.Services.GetRequiredService<NpgsqlDataSource>(), $"SELECT images::text FROM catalog.products WHERE id = '{product.Id.Value}'");
        json.ShouldBe($$"""[{"key": "{{prefix}}a.png", "color": "azul"}, {"key": "{{prefix}}b.png", "color": null}, {"key": "{{prefix}}c.png", "color": "azul"}]""");
    }

    [Fact]
    public async Task Migrate_ImageKeysOfOldRows_BecomeImagesWithTheFirstSkuColor()
    {
        await using var dataSource = NpgsqlDataSource.Create(await api.CreateDatabaseAsync("catalog_image_colors_up"));
        await using var db = Context(dataSource);
        await db.GetService<IMigrator>().MigrateAsync(BeforeColors, TestContext.Current.CancellationToken);
        await ExecuteAsync(dataSource, OldRows);

        await db.GetService<IMigrator>().MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var products = await db.Products.AsNoTracking().ToDictionaryAsync(product => product.Id.Value.ToString(), TestContext.Current.CancellationToken);
        products[ShirtId].Images.ShouldBe([new ProductImage($"products/{ShirtId}/a.webp", "azul"), new ProductImage($"products/{ShirtId}/b.webp", "azul")]);
        products[CapId].Images.ShouldBe([new ProductImage($"products/{CapId}/c.webp", null)]);
        products[SockId].Images.ShouldBeEmpty();
        (await ColumnCountAsync(dataSource, "image_keys")).ShouldBe(0L);
    }

    [Fact]
    public async Task Migrate_BackBeforeTheColors_RestoresTheImageKeysInOrder()
    {
        await using var dataSource = NpgsqlDataSource.Create(await api.CreateDatabaseAsync("catalog_image_colors_down"));
        await using var db = Context(dataSource);
        await db.GetService<IMigrator>().MigrateAsync(BeforeColors, TestContext.Current.CancellationToken);
        await ExecuteAsync(dataSource, OldRows);
        await db.GetService<IMigrator>().MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        await db.GetService<IMigrator>().MigrateAsync(BeforeColors, TestContext.Current.CancellationToken);

        var keys = await ScalarAsync<string>(dataSource, $"SELECT image_keys::text FROM catalog.products WHERE id = '{ShirtId}'");
        keys.ShouldBe($"{{products/{ShirtId}/a.webp,products/{ShirtId}/b.webp}}");
        (await ScalarAsync<string>(dataSource, $"SELECT image_keys::text FROM catalog.products WHERE id = '{SockId}'")).ShouldBe("{}");
        (await ColumnCountAsync(dataSource, "images")).ShouldBe(0L);
    }

    private static CatalogDbContext Context(NpgsqlDataSource dataSource)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        CatalogDbContext.Configure(options, dataSource);
        return new CatalogDbContext(options.Options);
    }

    private static Task<long> ColumnCountAsync(NpgsqlDataSource dataSource, string column) =>
        ScalarAsync<long>(dataSource, $"SELECT count(*) FROM information_schema.columns WHERE table_schema = 'catalog' AND table_name = 'products' AND column_name = '{column}'");

    private static async Task ExecuteAsync(NpgsqlDataSource dataSource, string sql)
    {
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlDataSource dataSource, string sql)
    {
        await using var command = dataSource.CreateCommand(sql);
        return (T)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
