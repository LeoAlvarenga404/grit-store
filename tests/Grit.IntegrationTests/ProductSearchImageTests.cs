using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

public class ProductSearchImageTests(ProductImageHost images) : IClassFixture<ProductImageHost>
{
    private readonly HttpClient _admin = AdminClient.Create(images.Host);

    [Fact]
    public async Task Search_ProductWithTwoImages_ReturnsTheFirstImageUrl()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        version = await ProductImageApi.AttachImageAsync(_admin, productId, version, 100);
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 200);

        await SearchItemAsync(productId);
        var item = await SearchItemAsync(productId);

        (await ProductImageApi.Storage.GetByteArrayAsync(item.GetProperty("imageUrl").GetString(), TestContext.Current.CancellationToken)).Length.ShouldBe(100);
    }

    [Fact]
    public async Task Search_ProductWithoutImages_ReturnsNullImageUrl()
    {
        var (productId, _, _) = await ProductImageApi.ProductWithSkuAsync(_admin);

        var item = await SearchItemAsync(productId);

        item.GetProperty("imageUrl").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Search_ImageAttachedAfterACachedRead_ReturnsItsUrl()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        await SearchItemAsync(productId);
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 100);

        var item = await SearchItemAsync(productId);

        (await ProductImageApi.Storage.GetByteArrayAsync(item.GetProperty("imageUrl").GetString(), TestContext.Current.CancellationToken)).Length.ShouldBe(100);
    }

    [Fact]
    public async Task Search_TwoProductsOnlyTheOlderWithAnImage_KeepsEachImageWithItsProduct()
    {
        var color = Guid.NewGuid().ToString("N");
        var (olderId, olderVersion) = await CatalogAdminApi.CreateProductAsync(_admin);
        var olderSku = await CatalogAdminApi.AddSkuAsync(_admin, olderId, olderVersion, color);
        await ProductImageApi.AttachImageAsync(_admin, olderId, olderSku.Version, 100, color);
        var (newerId, newerVersion) = await CatalogAdminApi.CreateProductAsync(_admin);
        await CatalogAdminApi.AddSkuAsync(_admin, newerId, newerVersion, color);

        var page = await _admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/catalog/products?attribute=cor:{color}&limit=50",
            TestContext.Current.CancellationToken);

        var items = page.GetProperty("items");
        items.GetArrayLength().ShouldBe(2);
        items[0].GetProperty("id").GetGuid().ShouldBe(newerId);
        items[0].GetProperty("imageUrl").ValueKind.ShouldBe(JsonValueKind.Null);
        items[1].GetProperty("id").GetGuid().ShouldBe(olderId);
        var olderImageUrl = items[1].GetProperty("imageUrl").GetString();
        (await ProductImageApi.Storage.GetByteArrayAsync(olderImageUrl, TestContext.Current.CancellationToken)).Length.ShouldBe(100);
    }

    private async Task<JsonElement> SearchItemAsync(Guid productId)
    {
        var name = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("name").GetString()!;
        var page = await _admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/catalog/products?q={Uri.EscapeDataString(name)}&limit=50",
            TestContext.Current.CancellationToken);
        return page.GetProperty("items").EnumerateArray().Where(item => item.GetProperty("id").GetGuid() == productId).ShouldHaveSingleItem();
    }
}
