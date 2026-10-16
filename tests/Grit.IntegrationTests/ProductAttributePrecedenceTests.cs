using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

// A database of its own: the search corpus has no key on both a product and its SKU, and adding one would move its counts.
public class ProductAttributePrecedenceTests(ApiFactory api)
{
    [Fact]
    public async Task SearchAndFacets_ProductAndSkuShareAKey_UseTheSkuValue()
    {
        var connectionString = await api.CreateDatabaseAsync("attribute_precedence");
        await using var host = api.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:grit", connectionString));
        var client = host.CreateClient();
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(client);
        var body = new { categoryId, name = "Capacete Urbano", description = "", attributes = new { cor = "azul" } };
        var created = await CatalogAdminApi.ReadAsync(await client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken));
        await CatalogAdminApi.AddSkuAsync(client, created.GetProperty("id").GetGuid(), created.GetProperty("version").GetUInt32(), "vermelho");

        var red = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/products?attribute=cor:vermelho", TestContext.Current.CancellationToken);
        var blue = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/products?attribute=cor:azul", TestContext.Current.CancellationToken);
        var facets = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/facets", TestContext.Current.CancellationToken);

        var item = red.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("name").GetString().ShouldBe("Capacete Urbano");
        item.GetProperty("priceAmount").GetDecimal().ShouldBe(99.90m);
        blue.GetProperty("items").EnumerateArray().ShouldBeEmpty();
        var colors = facets.GetProperty("attributes").EnumerateArray().Single(attribute => attribute.GetProperty("key").GetString() == "cor");
        colors.GetProperty("values").EnumerateArray()
            .Select(value => $"{value.GetProperty("value").GetString()} {value.GetProperty("count").GetInt64()}")
            .ShouldBe(["vermelho 1"]);
    }
}
