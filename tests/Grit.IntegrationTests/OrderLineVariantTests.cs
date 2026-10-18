using System.Text.Json;

namespace Grit.IntegrationTests;

public class OrderLineVariantTests(ApiFactory factory)
{
    private readonly HttpClient _admin = AdminClient.Create(factory);

    private readonly HttpClient _customer = CustomerClient.Create(factory, Guid.NewGuid());

    [Fact]
    public async Task GetOrder_SkuWithColorAndSize_ShowsTheSlugColorAndSizeBought()
    {
        var (productId, version) = await CatalogAdminApi.CreateProductAsync(_admin);
        var sku = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "azul", "M");

        var line = await OrderedLineAsync(sku.Id);

        var slug = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("slug").GetString();
        line.GetProperty("productSlug").GetString().ShouldBe(slug);
        line.GetProperty("color").GetString().ShouldBe("azul");
        line.GetProperty("size").GetString().ShouldBe("M");
        line.GetProperty("imageUrl").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetOrder_SkuWithoutSize_ShowsANullSize()
    {
        var (productId, version) = await CatalogAdminApi.CreateProductAsync(_admin);
        var sku = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "preto");

        var line = await OrderedLineAsync(sku.Id);

        line.GetProperty("color").GetString().ShouldBe("preto");
        line.GetProperty("size").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    private async Task<JsonElement> OrderedLineAsync(Guid skuId)
    {
        await BasketApi.AddLineAsync(_customer, skuId, 1);
        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 99.90m);
        return (await CheckoutApi.GetOrderAsync(_customer, orderId)).GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();
    }
}
