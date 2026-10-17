using System.Net;
using System.Net.Http.Json;

namespace Grit.IntegrationTests;

// Both requests pass the slug pre-check before either commits; several rounds make the race near certain without the fix.
public class CatalogDoubleSubmitTests(ApiFactory factory)
{
    private const int Rounds = 5;

    private readonly HttpClient _client = AdminClient.Create(factory);

    [Fact]
    public async Task CreateCategory_SameNameTwiceAtOnce_Returns201And409()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var body = new { name = CatalogAdminApi.UniqueName("Escalada") };

            var responses = await Task.WhenAll(
                _client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, body, TestContext.Current.CancellationToken),
                _client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, body, TestContext.Current.CancellationToken));

            await AssertOneCreatedAndOneConflictAsync(responses, "catalog.category_slug_taken");
        }
    }

    [Fact]
    public async Task CreateProduct_SameNameTwiceAtOnce_Returns201And409()
    {
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(_client);
        for (var round = 0; round < Rounds; round++)
        {
            var body = new { categoryId, name = CatalogAdminApi.UniqueName("Mochila"), description = "" };

            var responses = await Task.WhenAll(
                _client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken),
                _client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken));

            await AssertOneCreatedAndOneConflictAsync(responses, "catalog.product_slug_taken");
        }
    }

    private static async Task AssertOneCreatedAndOneConflictAsync(HttpResponseMessage[] responses, string conflictCode)
    {
        responses.Select(response => response.StatusCode).Order().ShouldBe([HttpStatusCode.Created, HttpStatusCode.Conflict]);
        var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        var problem = await ProblemResponse.ReadAsync(conflict, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe(conflictCode);
    }
}
