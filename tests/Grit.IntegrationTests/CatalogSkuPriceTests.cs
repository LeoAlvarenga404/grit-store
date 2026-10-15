using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

public class CatalogSkuPriceTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ChangeSkuPrice_CurrentVersion_ChangesOnlyThatSkuAndTheVersion()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var blue = await CatalogAdminApi.AddSkuAsync(_client, id, version, "azul");
        var green = await CatalogAdminApi.AddSkuAsync(_client, id, blue.Version, "verde");

        var response = await ChangePriceAsync(id, blue.Id, 149.90m, "BRL", green.Version);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newVersion = (await CatalogAdminApi.ReadAsync(response)).GetProperty("version").GetUInt32();
        newVersion.ShouldNotBe(green.Version);
        var product = await CatalogAdminApi.GetProductAsync(_client, id);
        PriceOf(product, blue.Id).ShouldBe(149.90m);
        PriceOf(product, green.Id).ShouldBe(99.90m);
        product.GetProperty("version").GetUInt32().ShouldBe(newVersion);
    }

    [Fact]
    public async Task ChangeSkuPrice_StaleVersion_Returns409AndKeepsTheFirstPrice()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var sku = await CatalogAdminApi.AddSkuAsync(_client, id, version, "azul");
        (await ChangePriceAsync(id, sku.Id, 120m, "BRL", sku.Version)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await ChangePriceAsync(id, sku.Id, 130m, "BRL", sku.Version);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_changed");
        PriceOf(await CatalogAdminApi.GetProductAsync(_client, id), sku.Id).ShouldBe(120m);
    }

    [Theory]
    [MemberData(nameof(CatalogSkuTests.InvalidPrices), MemberType = typeof(CatalogSkuTests))]
    public async Task ChangeSkuPrice_InvalidPrice_Returns400WithCodeAndKeepsThePrice(decimal priceAmount, string priceCurrency, string code)
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var sku = await CatalogAdminApi.AddSkuAsync(_client, id, version, "azul");

        var response = await ChangePriceAsync(id, sku.Id, priceAmount, priceCurrency, sku.Version);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe(code);
        var product = await CatalogAdminApi.GetProductAsync(_client, id);
        PriceOf(product, sku.Id).ShouldBe(99.90m);
        product.GetProperty("version").GetUInt32().ShouldBe(sku.Version);
    }

    [Fact]
    public async Task ChangeSkuPrice_UnknownSku_Returns404()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);

        var response = await ChangePriceAsync(id, Guid.NewGuid(), 149.90m, "BRL", version);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.sku_not_found");
    }

    [Fact]
    public async Task ChangeSkuPrice_UnknownProduct_Returns404()
    {
        var response = await ChangePriceAsync(Guid.NewGuid(), Guid.NewGuid(), 149.90m, "BRL", 1);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }

    private Task<HttpResponseMessage> ChangePriceAsync(Guid productId, Guid skuId, decimal priceAmount, string priceCurrency, uint version) =>
        _client.PutAsJsonAsync(
            $"{CatalogAdminApi.SkusPath(productId)}/{skuId}/price",
            new { priceAmount, priceCurrency, version },
            TestContext.Current.CancellationToken);

    private static decimal PriceOf(JsonElement product, Guid skuId) =>
        product.GetProperty("skus").EnumerateArray()
            .Single(sku => sku.GetProperty("id").GetGuid() == skuId)
            .GetProperty("priceAmount").GetDecimal();
}
