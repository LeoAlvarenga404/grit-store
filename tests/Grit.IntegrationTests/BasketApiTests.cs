using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

public class BasketApiTests(ApiFactory factory)
{
    private readonly HttpClient _admin = AdminClient.Create(factory);
    private readonly HttpClient _customer = CustomerClient.Create(factory, Guid.NewGuid());

    [Fact]
    public async Task GetBasket_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync(BasketApi.BasketPath, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddLine_ThenGetBasket_ReturnsTheLineWithSnapshotAndCurrentPrice()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);

        await BasketApi.AddLineAsync(_customer, skuId, 2);

        var basket = await BasketApi.GetAsync(_customer);
        var line = basket.GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();
        line.GetProperty("skuId").GetGuid().ShouldBe(skuId);
        line.GetProperty("quantity").GetInt32().ShouldBe(2);
        line.GetProperty("unitPriceAmount").GetDecimal().ShouldBe(99.90m);
        line.GetProperty("currentUnitPriceAmount").GetDecimal().ShouldBe(99.90m);
        line.GetProperty("hasPriceChanged").GetBoolean().ShouldBeFalse();
        line.GetProperty("isAvailable").GetBoolean().ShouldBeTrue();
        line.GetProperty("productName").GetString()!.ShouldStartWith("Camiseta");
        line.GetProperty("color").GetString().ShouldBe("azul");
        line.GetProperty("size").ValueKind.ShouldBe(JsonValueKind.Null);
        line.GetProperty("imageUrl").ValueKind.ShouldBe(JsonValueKind.Null);
        basket.GetProperty("subtotalAmount").GetDecimal().ShouldBe(199.80m);
        basket.GetProperty("currency").GetString().ShouldBe("BRL");
    }

    [Fact]
    public async Task GetBasket_SkuWithColorAndSize_ShowsThemWithoutAnImage()
    {
        var (productId, version) = await CatalogAdminApi.CreateProductAsync(_admin);
        var sku = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "preto", "42");
        await BasketApi.AddLineAsync(_customer, sku.Id, 1);

        var line = (await BasketApi.GetAsync(_customer)).GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();

        line.GetProperty("color").GetString().ShouldBe("preto");
        line.GetProperty("size").GetString().ShouldBe("42");
        line.GetProperty("imageUrl").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task AddLine_UnknownSku_Returns404()
    {
        var response = await _customer.PostAsJsonAsync(BasketApi.LinesPath, new { skuId = Guid.NewGuid(), quantity = 1 }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CatalogAdminApi.ReadAsync(response)).GetProperty("code").GetString().ShouldBe("basket.sku_not_found");
    }

    [Fact]
    public async Task AddLine_ZeroQuantity_Returns400()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);

        var response = await _customer.PostAsJsonAsync(BasketApi.LinesPath, new { skuId, quantity = 0 }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CatalogAdminApi.ReadAsync(response)).GetProperty("code").GetString().ShouldBe("validation.failed");
    }

    [Fact]
    public async Task GetBasket_PriceChangedAfterAdding_FlagsTheLineAndUsesTheCurrentPrice()
    {
        var (productId, version, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await BasketApi.AddLineAsync(_customer, skuId, 2);

        var changed = await _admin.PutAsJsonAsync(
            $"{CatalogAdminApi.SkusPath(productId)}/{skuId}/price",
            new { priceAmount = 89.90m, priceCurrency = "BRL", version },
            TestContext.Current.CancellationToken);
        changed.StatusCode.ShouldBe(HttpStatusCode.OK);

        var basket = await BasketApi.GetAsync(_customer);
        var line = basket.GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();
        line.GetProperty("unitPriceAmount").GetDecimal().ShouldBe(99.90m);
        line.GetProperty("currentUnitPriceAmount").GetDecimal().ShouldBe(89.90m);
        line.GetProperty("hasPriceChanged").GetBoolean().ShouldBeTrue();
        basket.GetProperty("subtotalAmount").GetDecimal().ShouldBe(179.80m);
    }

    [Fact]
    public async Task ChangeAndRemoveLine_UpdateTheBasket()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await BasketApi.AddLineAsync(_customer, skuId, 1);
        var linePath = $"{BasketApi.LinesPath}/{skuId}";

        (await _customer.PutAsJsonAsync(linePath, new { quantity = 5 }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await BasketApi.GetAsync(_customer)).GetProperty("lines")[0].GetProperty("quantity").GetInt32().ShouldBe(5);

        (await _customer.DeleteAsync(linePath, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await BasketApi.GetAsync(_customer)).GetProperty("lines").GetArrayLength().ShouldBe(0);

        var missing = await _customer.PutAsJsonAsync(linePath, new { quantity = 5 }, TestContext.Current.CancellationToken);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CatalogAdminApi.ReadAsync(missing)).GetProperty("code").GetString().ShouldBe("basket.line_not_found");
    }

    [Fact]
    public async Task GetBasket_OtherCustomer_DoesNotSeeTheLines()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await BasketApi.AddLineAsync(_customer, skuId, 2);
        var stranger = CustomerClient.Create(factory, Guid.NewGuid());

        (await BasketApi.GetAsync(_customer)).GetProperty("lines").GetArrayLength().ShouldBe(1);
        (await BasketApi.GetAsync(stranger)).GetProperty("lines").GetArrayLength().ShouldBe(0);
    }
}
