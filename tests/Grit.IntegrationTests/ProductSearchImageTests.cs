using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Grit.Modules.Catalog.Features.SearchProducts;
using Grit.Modules.Catalog.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task Search_ColorsWithImages_ShowTheFirstImageOfEachColorInImageOrder()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var black = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "preto");
        version = await ProductImageApi.AttachImageAsync(_admin, productId, black.Version, 100, "preto");
        version = await ProductImageApi.AttachImageAsync(_admin, productId, version, 200, "azul");
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 300, "preto");

        var item = await SearchItemAsync(productId);

        var colors = item.GetProperty("colors").EnumerateArray().ToList();
        colors.Select(color => color.GetProperty("name").GetString()).ShouldBe(["preto", "azul"]);
        (await SizeOfAsync(colors[0].GetProperty("imageUrl"))).ShouldBe(100);
        (await SizeOfAsync(colors[1].GetProperty("imageUrl"))).ShouldBe(200);
        (await SizeOfAsync(item.GetProperty("imageUrl"))).ShouldBe(100);
    }

    // The cache holds keys only (ADR 0017): a warmed entry has no URL, and every response signs its own.
    [Fact]
    public async Task Search_CachedPage_HoldsImageKeysAndNoUrls()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 100);
        var name = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("name").GetString()!;
        await SearchItemAsync(productId);

        var cache = images.Host.Services.GetRequiredService<CatalogCache>();
        var query = new SearchProductsQuery(name, null, null, null, [], ProductSort.Relevance, null, 50);
        var page = await cache.GetOrCreateAsync<SearchProductsPage>(
            CatalogCache.KeyOf("search-page-v2", query),
            _ => throw new InvalidOperationException("The page was not cached."),
            TestContext.Current.CancellationToken);

        var index = page.Response.Items.ToList().FindIndex(item => item.Id == productId);
        page.Response.Items[index].ImageUrl.ShouldBeNull();
        page.Response.Items[index].Colors.ShouldAllBe(color => color.ImageUrl == null);
        page.ImageKeys[index].Cover.ShouldNotBeNull().ShouldStartWith($"products/{productId}/");
        page.ImageKeys[index].Colors.ShouldBe([page.ImageKeys[index].Cover]);
        (await SizeOfAsync((await SearchItemAsync(productId)).GetProperty("imageUrl"))).ShouldBe(100);
    }

    // The cover is the product's first image even when it has no colour; the colour list starts at the first coloured image.
    [Fact]
    public async Task Search_FirstImageWithoutColor_IsTheCoverButNoColorsImage()
    {
        var (productId, version) = await CatalogAdminApi.CreateProductAsync(_admin);
        var blue = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "azul");
        var plain = await _admin.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(productId),
            new { attributes = new { tamanho = "U" }, priceAmount = 59.90m, priceCurrency = "BRL", version = blue.Version },
            TestContext.Current.CancellationToken);
        plain.StatusCode.ShouldBe(HttpStatusCode.Created);
        version = (await CatalogAdminApi.ReadAsync(plain)).GetProperty("version").GetUInt32();
        version = await ProductImageApi.AttachImageAsync(_admin, productId, version, 100, color: null);
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 200, "azul");

        var item = await SearchItemAsync(productId);

        (await SizeOfAsync(item.GetProperty("imageUrl"))).ShouldBe(100);
        var color = item.GetProperty("colors").EnumerateArray().ShouldHaveSingleItem();
        color.GetProperty("name").GetString().ShouldBe("azul");
        (await SizeOfAsync(color.GetProperty("imageUrl"))).ShouldBe(200);
    }

    private static async Task<int> SizeOfAsync(JsonElement url) =>
        (await ProductImageApi.Storage.GetByteArrayAsync(url.GetString(), TestContext.Current.CancellationToken)).Length;

    private async Task<JsonElement> SearchItemAsync(Guid productId)
    {
        var name = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("name").GetString()!;
        var page = await _admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/catalog/products?q={Uri.EscapeDataString(name)}&limit=50",
            TestContext.Current.CancellationToken);
        return page.GetProperty("items").EnumerateArray().Where(item => item.GetProperty("id").GetGuid() == productId).ShouldHaveSingleItem();
    }
}
