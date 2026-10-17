using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

// The cover (100 bytes) is black; the blue SKU must show the first blue image (200), not the cover nor the last blue one (300).
public class BasketImageTests(ProductImageHost images) : IClassFixture<ProductImageHost>
{
    private readonly HttpClient _admin = AdminClient.Create(images.Host);

    private readonly HttpClient _customer = CustomerClient.Create(images.Host, Guid.NewGuid());

    [Fact]
    public async Task GetBasket_SkuOfAColorWithImages_ShowsThatColorsFirstImage()
    {
        var (productId, version, blueSkuId) = await BasketApi.CreateSkuAsync(_admin);
        var black = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "preto");
        var green = await CatalogAdminApi.AddSkuAsync(_admin, productId, black.Version, "verde");
        version = await ProductImageApi.AttachImageAsync(_admin, productId, green.Version, 100, "preto");
        version = await ProductImageApi.AttachImageAsync(_admin, productId, version, 200, "azul");
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 300, "azul");
        await BasketApi.AddLineAsync(_customer, blueSkuId, 1);
        await BasketApi.AddLineAsync(_customer, green.Id, 1);

        var lines = (await BasketApi.GetAsync(_customer)).GetProperty("lines").EnumerateArray().ToDictionary(line => line.GetProperty("skuId").GetGuid());

        lines[blueSkuId].GetProperty("color").GetString().ShouldBe("azul");
        (await SizeOfAsync(lines[blueSkuId].GetProperty("imageUrl"))).ShouldBe(200);
        lines[green.Id].GetProperty("imageUrl").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // Blue image first, then two images without colour: the colourless SKU takes the first colourless one, never the blue.
    [Fact]
    public async Task GetBasket_SkuWithoutColor_ShowsTheFirstImageWithoutColor()
    {
        var (productId, version) = await CatalogAdminApi.CreateProductAsync(_admin);
        var blue = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "azul");
        var plain = await AddSkuWithoutColorAsync(productId, blue.Version);
        version = await ProductImageApi.AttachImageAsync(_admin, productId, plain.Version, 100, "azul");
        version = await ProductImageApi.AttachImageAsync(_admin, productId, version, 200, color: null);
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 300, color: null);
        await BasketApi.AddLineAsync(_customer, blue.Id, 1);
        await BasketApi.AddLineAsync(_customer, plain.Id, 1);

        var lines = (await BasketApi.GetAsync(_customer)).GetProperty("lines").EnumerateArray().ToDictionary(line => line.GetProperty("skuId").GetGuid());

        (await SizeOfAsync(lines[blue.Id].GetProperty("imageUrl"))).ShouldBe(100);
        lines[plain.Id].GetProperty("color").ValueKind.ShouldBe(JsonValueKind.Null);
        lines[plain.Id].GetProperty("size").GetString().ShouldBe("U");
        (await SizeOfAsync(lines[plain.Id].GetProperty("imageUrl"))).ShouldBe(200);
    }

    [Fact]
    public async Task GetBasket_SkuWithoutColorAndOnlyColoredImages_ShowsNoImage()
    {
        var (productId, version) = await CatalogAdminApi.CreateProductAsync(_admin);
        var blue = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "azul");
        var plain = await AddSkuWithoutColorAsync(productId, blue.Version);
        await ProductImageApi.AttachImageAsync(_admin, productId, plain.Version, 100, "azul");
        await BasketApi.AddLineAsync(_customer, plain.Id, 1);

        var line = (await BasketApi.GetAsync(_customer)).GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();

        line.GetProperty("imageUrl").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    private async Task<(Guid Id, uint Version)> AddSkuWithoutColorAsync(Guid productId, uint version)
    {
        var response = await _admin.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(productId),
            new { attributes = new { tamanho = "U" }, priceAmount = 59.90m, priceCurrency = "BRL", version },
            TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var sku = await CatalogAdminApi.ReadAsync(response);
        return (sku.GetProperty("id").GetGuid(), sku.GetProperty("version").GetUInt32());
    }

    private static async Task<int> SizeOfAsync(JsonElement url) =>
        (await ProductImageApi.Storage.GetByteArrayAsync(url.GetString(), TestContext.Current.CancellationToken)).Length;
}
