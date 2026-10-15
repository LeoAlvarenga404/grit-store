using System.Net;
using System.Net.Http.Json;

namespace Grit.IntegrationTests;

public class CatalogSkuTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    public static TheoryData<decimal, string, string> InvalidPrices => new()
    {
        { -1m, "BRL", "validation.failed" },
        { 10.555m, "BRL", "money.too_many_decimal_places" },
        { 1_000_000_000_000.01m, "BRL", "money.amount_too_large" },
        { 10m, "brl", "money.invalid_currency" },
        { 10m, "USD", "catalog.unsupported_currency" },
    };

    [Fact]
    public async Task AddSku_NewVariant_Returns201AndTheProductListsIt()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var body = new { attributes = new { cor = "azul", tamanho = "M" }, priceAmount = 199.90m, priceCurrency = "BRL", version };

        var response = await _client.PostAsJsonAsync(CatalogAdminApi.SkusPath(id), body, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await CatalogAdminApi.ReadAsync(response);
        created.GetProperty("version").GetUInt32().ShouldNotBe(version);
        var product = await CatalogAdminApi.GetProductAsync(_client, id);
        var sku = product.GetProperty("skus").EnumerateArray().ShouldHaveSingleItem();
        sku.GetProperty("id").GetGuid().ShouldBe(created.GetProperty("id").GetGuid());
        sku.GetProperty("attributes").GetProperty("cor").GetString().ShouldBe("azul");
        sku.GetProperty("attributes").GetProperty("tamanho").GetString().ShouldBe("M");
        sku.GetProperty("priceAmount").GetDecimal().ShouldBe(199.90m);
        sku.GetProperty("priceCurrency").GetString().ShouldBe("BRL");
        product.GetProperty("version").GetUInt32().ShouldBe(created.GetProperty("version").GetUInt32());
    }

    [Fact]
    public async Task AddSku_ExistingVariant_Returns409()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var first = await CatalogAdminApi.AddSkuAsync(_client, id, version, "azul");

        var response = await _client.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(id),
            new { attributes = new { cor = "azul" }, priceAmount = 10m, priceCurrency = "BRL", version = first.Version },
            TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("catalog.duplicate_variant");
    }

    [Fact]
    public async Task AddSku_StaleVersion_Returns409ProductChanged()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        await CatalogAdminApi.AddSkuAsync(_client, id, version, "azul");

        var response = await _client.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(id),
            new { attributes = new { cor = "verde" }, priceAmount = 10m, priceCurrency = "BRL", version },
            TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_changed");
        (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("skus").GetArrayLength().ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(InvalidPrices))]
    public async Task AddSku_InvalidPrice_Returns400WithCodeAndAddsNothing(decimal priceAmount, string priceCurrency, string code)
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);

        var response = await _client.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(id),
            new { attributes = new { cor = "azul" }, priceAmount, priceCurrency, version },
            TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe(code);
        (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("skus").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task AddSku_InvalidAttributes_Returns400WithFieldError()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var attributes = new Dictionary<string, string> { ["Cor"] = "azul" };

        var response = await _client.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(id),
            new { attributes, priceAmount = 10m, priceCurrency = "BRL", version },
            TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").GetProperty("attributes").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task AddSku_UnknownProduct_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(Guid.NewGuid()),
            new { attributes = new { cor = "azul" }, priceAmount = 10m, priceCurrency = "BRL", version = 1 },
            TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }
}
