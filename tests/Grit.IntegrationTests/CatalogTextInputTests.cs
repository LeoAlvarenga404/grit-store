using System.Net;
using System.Net.Http.Json;

namespace Grit.IntegrationTests;

public class CatalogTextInputTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateProduct_NulInDescription_Returns400WithFieldError()
    {
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(_client);
        var body = new { categoryId, name = CatalogAdminApi.UniqueName("Lanterna"), description = "Leve\u0000" };

        var response = await _client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").GetProperty("description").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task UpdateProduct_NulInAttributeValue_Returns400AndKeepsTheProduct()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var categoryId = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("categoryId").GetGuid();
        var attributes = new Dictionary<string, string> { ["material"] = "algodao\u0000" };
        var body = new { categoryId, name = "Outro Nome", description = "", attributes, version };

        var response = await _client.PutAsJsonAsync($"{CatalogAdminApi.ProductsPath}/{id}", body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").GetProperty("attributes").GetArrayLength().ShouldBe(1);
        var product = await CatalogAdminApi.GetProductAsync(_client, id);
        product.GetProperty("version").GetUInt32().ShouldBe(version);
    }
}
