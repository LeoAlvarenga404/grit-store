using System.Net.Http.Json;
using System.Text.Json;
using Grit.Modules.Catalog.Features.GetProductBySlug;
using Grit.Modules.Catalog.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

// The host has the cache on: the second read comes from the cache and must group and sign the same way.
public class ProductPageImageTests(ProductImageHost images) : IClassFixture<ProductImageHost>
{
    private readonly HttpClient _admin = AdminClient.Create(images.Host);

    [Fact]
    public async Task GetProductBySlug_ImagesOfTwoColors_GroupsThemByColorInImageOrder()
    {
        var (productId, version, slug) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var black = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "preto");
        version = await ProductImageApi.AttachImageAsync(_admin, productId, black.Version, 100, "preto");
        version = await ProductImageApi.AttachImageAsync(_admin, productId, version, 200, "azul");
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 300, "preto");

        foreach (var page in new[] { await PageAsync(slug), await PageAsync(slug) })
        {
            var colors = page.GetProperty("colors").EnumerateArray().ToList();
            colors.Select(color => color.GetProperty("name").GetString()).ShouldBe(["preto", "azul"]);
            (await SizesOfAsync(colors[0].GetProperty("imageUrls"))).ShouldBe([100, 300]);
            (await SizesOfAsync(colors[1].GetProperty("imageUrls"))).ShouldBe([200]);
            (await SizesOfAsync(page.GetProperty("imageUrls"))).ShouldBe([100, 200, 300]);
        }
    }

    [Fact]
    public async Task GetProductBySlug_ColorWithoutImages_ListsItWithoutUrls()
    {
        var (productId, version, slug) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var green = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "verde");
        await ProductImageApi.AttachImageAsync(_admin, productId, green.Version, 100, "azul");

        var page = await PageAsync(slug);

        var colors = page.GetProperty("colors").EnumerateArray().ToList();
        colors.Select(color => color.GetProperty("name").GetString()).ShouldBe(["azul", "verde"]);
        (await SizesOfAsync(colors[0].GetProperty("imageUrls"))).ShouldBe([100]);
        colors[1].GetProperty("imageUrls").GetArrayLength().ShouldBe(0);
    }

    // The cache holds keys only (ADR 0017): the warmed row has no URL, and every response signs its own.
    [Fact]
    public async Task GetProductBySlug_CachedRow_HoldsImageKeysAndNoUrls()
    {
        var (productId, version, slug) = await ProductImageApi.ProductWithSkuAsync(_admin);
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 100);
        await PageAsync(slug);

        var cache = images.Host.Services.GetRequiredService<CatalogCache>();
        var row = await cache.GetOrCreateAsync<GetProductBySlugRow?>(
            $"catalog:product-v2:{slug}",
            _ => throw new InvalidOperationException("The page was not cached."),
            TestContext.Current.CancellationToken);

        row.ShouldNotBeNull();
        row.Product.ImageUrls.ShouldBeEmpty();
        row.Product.Colors.ShouldBeEmpty();
        row.Images.ShouldHaveSingleItem().Key.ShouldStartWith($"products/{productId}/");
        (await SizesOfAsync((await PageAsync(slug)).GetProperty("colors")[0].GetProperty("imageUrls"))).ShouldBe([100]);
    }

    private Task<JsonElement> PageAsync(string slug) =>
        _admin.GetFromJsonAsync<JsonElement>($"/api/v1/catalog/products/{slug}", TestContext.Current.CancellationToken);

    private static async Task<List<int>> SizesOfAsync(JsonElement urls)
    {
        var sizes = new List<int>();
        foreach (var url in urls.EnumerateArray())
        {
            sizes.Add((await ProductImageApi.Storage.GetByteArrayAsync(url.GetString(), TestContext.Current.CancellationToken)).Length);
        }

        return sizes;
    }
}
