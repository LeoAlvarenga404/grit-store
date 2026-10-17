using Grit.Modules.Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class CatalogReaderTests(ApiFactory factory)
{
    [Fact]
    public async Task GetSkusAsync_KnownAndUnknownIds_ReturnsOnlyTheKnownSnapshot()
    {
        var (productId, _, skuId) = await BasketApi.CreateSkuAsync(factory.CreateClient());
        var product = await CatalogAdminApi.GetProductAsync(factory.CreateClient(), productId);
        using var scope = factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ICatalogReader>();

        var skus = await reader.GetSkusAsync([skuId, Guid.NewGuid()], TestContext.Current.CancellationToken);

        skus.ShouldBe([new SkuSnapshot(skuId, product.GetProperty("name").GetString()!, product.GetProperty("slug").GetString()!, 99.90m, "BRL")]);
    }

    [Fact]
    public async Task GetSkuIdsAsync_AfterAddingASku_ContainsIt()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(factory.CreateClient());
        using var scope = factory.Services.CreateScope();

        var ids = await scope.ServiceProvider.GetRequiredService<ICatalogReader>().GetSkuIdsAsync(TestContext.Current.CancellationToken);

        ids.ShouldContain(skuId);
    }
}
