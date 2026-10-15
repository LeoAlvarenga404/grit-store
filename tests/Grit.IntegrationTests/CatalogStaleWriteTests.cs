using System.Net;
using System.Net.Http.Json;

namespace Grit.IntegrationTests;

// A stale write whose values already match the current row must still be rejected, not answered 200 with the old version.
public class CatalogStaleWriteTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UpdateProduct_SameValuesWithStaleVersion_Returns409ProductChanged()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var categoryId = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("categoryId").GetGuid();
        var path = $"{CatalogAdminApi.ProductsPath}/{id}";
        var body = new { categoryId, name = "Primeira", description = "", version };
        var first = await _client.PutAsJsonAsync(path, body, TestContext.Current.CancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var currentVersion = (await CatalogAdminApi.ReadAsync(first)).GetProperty("version").GetUInt32();

        var response = await _client.PutAsJsonAsync(path, body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_changed");
        (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("version").GetUInt32().ShouldBe(currentVersion);
    }

    [Fact]
    public async Task ChangeSkuPrice_SamePriceWithStaleVersion_Returns409ProductChanged()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var sku = await CatalogAdminApi.AddSkuAsync(_client, id, version, "azul");
        var path = $"{CatalogAdminApi.SkusPath(id)}/{sku.Id}/price";
        var body = new { priceAmount = 120m, priceCurrency = "BRL", version = sku.Version };
        var first = await _client.PutAsJsonAsync(path, body, TestContext.Current.CancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var currentVersion = (await CatalogAdminApi.ReadAsync(first)).GetProperty("version").GetUInt32();

        var response = await _client.PutAsJsonAsync(path, body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_changed");
        (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("version").GetUInt32().ShouldBe(currentVersion);
    }
}
