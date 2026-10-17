using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

internal static class CatalogAdminApi
{
    public const string CategoriesPath = "/api/v1/admin/categories";

    public const string ProductsPath = "/api/v1/admin/products";

    public static async Task<Guid> CreateCategoryAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(CategoriesPath, new { name = UniqueName("Categoria") }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await ReadAsync(response)).GetProperty("id").GetGuid();
    }

    public static async Task<(Guid Id, uint Version)> CreateProductAsync(HttpClient client)
    {
        var categoryId = await CreateCategoryAsync(client);
        var response = await client.PostAsJsonAsync(
            ProductsPath,
            new { categoryId, name = UniqueName("Camiseta"), description = "Dry fit.", attributes = new { material = "poliester" } },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await ReadAsync(response);
        return (created.GetProperty("id").GetGuid(), created.GetProperty("version").GetUInt32());
    }

    public static string SkusPath(Guid productId) => $"{ProductsPath}/{productId}/skus";

    public static async Task<(Guid Id, uint Version)> AddSkuAsync(HttpClient client, Guid productId, uint version, string color)
    {
        var response = await client.PostAsJsonAsync(
            SkusPath(productId),
            new { attributes = new { cor = color }, priceAmount = 99.90m, priceCurrency = "BRL", version },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await ReadAsync(response);
        return (created.GetProperty("id").GetGuid(), created.GetProperty("version").GetUInt32());
    }

    public static Task<JsonElement> GetProductAsync(HttpClient client, Guid id) =>
        client.GetFromJsonAsync<JsonElement>($"{ProductsPath}/{id}", TestContext.Current.CancellationToken);

    public static Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    public static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";
}
