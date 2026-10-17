using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

// A database of its own: the page sizes are exact only when no other test adds products.
public class CatalogAdminListTests(ApiFactory api)
{
    [Fact]
    public async Task ListProducts_MoreThanAPage_PagesByNameIncludingProductsWithoutSkus()
    {
        var connectionString = await api.CreateDatabaseAsync("admin_product_list");
        await using var host = api.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:grit", connectionString));
        var admin = AdminClient.Create(host);
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(admin);
        for (var index = 0; index < 51; index++)
        {
            var body = new { categoryId, name = $"Produto {index:D2}", description = "", attributes = new { material = "nylon" } };
            (await admin.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        var first = await admin.GetFromJsonAsync<JsonElement>(CatalogAdminApi.ProductsPath, TestContext.Current.CancellationToken);
        var second = await admin.GetFromJsonAsync<JsonElement>($"{CatalogAdminApi.ProductsPath}?page=2", TestContext.Current.CancellationToken);

        first.GetProperty("items").GetArrayLength().ShouldBe(50);
        first.GetProperty("hasNextPage").GetBoolean().ShouldBeTrue();
        first.GetProperty("items")[0].GetProperty("name").GetString().ShouldBe("Produto 00");
        first.GetProperty("items")[0].GetProperty("skuCount").GetInt32().ShouldBe(0);
        second.GetProperty("items").EnumerateArray().ShouldHaveSingleItem().GetProperty("name").GetString().ShouldBe("Produto 50");
        second.GetProperty("hasNextPage").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task ListProducts_PageZero_Returns400()
    {
        var response = await AdminClient.Create(api).GetAsync($"{CatalogAdminApi.ProductsPath}?page=0", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
    }
}
