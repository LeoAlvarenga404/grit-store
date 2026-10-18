using System.Net;
using System.Text.Json;

namespace Grit.IntegrationTests;

// The order stores no image: each read asks the catalogue for the SKU colour's current first image.
public class OrderLineImageTests(ProductImageHost images) : IClassFixture<ProductImageHost>
{
    private readonly HttpClient _admin = AdminClient.Create(images.Host);

    private readonly HttpClient _customer = CustomerClient.Create(images.Host, Guid.NewGuid());

    [Fact]
    public async Task GetOrder_LineOfAColorWithImages_ShowsThatColorsFirstImage()
    {
        var (productId, version, blueSkuId) = await BasketApi.CreateSkuAsync(_admin);
        var black = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "preto");
        version = await ProductImageApi.AttachImageAsync(_admin, productId, black.Version, 100, "preto");
        await ProductImageApi.AttachImageAsync(_admin, productId, version, 200, "azul");

        var line = await OrderedLineAsync(blueSkuId);

        (await SizeOfAsync(line.GetProperty("imageUrl"))).ShouldBe(200);
    }

    [Fact]
    public async Task GetOrder_ImageRemovedAfterCheckout_ShowsNoImage()
    {
        var (productId, version, blueSkuId) = await BasketApi.CreateSkuAsync(_admin);
        var (key, afterAttach) = await AttachAsync(productId, version, 100);
        await BasketApi.AddLineAsync(_customer, blueSkuId, 1);
        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 99.90m);

        var removed = await _admin.DeleteAsync(
            $"{CatalogAdminApi.ProductsPath}/{productId}/images/{ProductImageApi.FileNameOf(key)}?version={afterAttach}",
            TestContext.Current.CancellationToken);
        removed.StatusCode.ShouldBe(HttpStatusCode.OK);

        var line = (await CheckoutApi.GetOrderAsync(_customer, orderId)).GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();
        line.GetProperty("imageUrl").ValueKind.ShouldBe(JsonValueKind.Null);
        line.GetProperty("color").GetString().ShouldBe("azul");
    }

    private async Task<JsonElement> OrderedLineAsync(Guid skuId)
    {
        await BasketApi.AddLineAsync(_customer, skuId, 1);
        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 99.90m);
        return (await CheckoutApi.GetOrderAsync(_customer, orderId)).GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();
    }

    private async Task<(string Key, uint Version)> AttachAsync(Guid productId, uint version, int sizeInBytes)
    {
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", sizeInBytes);
        await ProductImageApi.PutAsync(upload, "image/png", sizeInBytes);
        var key = upload.GetProperty("key").GetString()!;
        var response = await ProductImageApi.AttachAsync(_admin, productId, key, version, "azul");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (key, (await CatalogAdminApi.ReadAsync(response)).GetProperty("version").GetUInt32());
    }

    private static async Task<int> SizeOfAsync(JsonElement url) =>
        (await ProductImageApi.Storage.GetByteArrayAsync(url.GetString(), TestContext.Current.CancellationToken)).Length;
}
