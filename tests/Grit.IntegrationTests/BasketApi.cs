using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

internal static class BasketApi
{
    public const string BasketPath = "/api/v1/basket";

    public const string LinesPath = "/api/v1/basket/lines";

    public static async Task<(Guid ProductId, uint Version, Guid SkuId)> CreateSkuAsync(HttpClient admin)
    {
        var product = await CatalogAdminApi.CreateProductAsync(admin);
        var sku = await CatalogAdminApi.AddSkuAsync(admin, product.Id, product.Version, "azul");
        return (product.Id, sku.Version, sku.Id);
    }

    public static async Task AddLineAsync(HttpClient client, Guid skuId, int quantity)
    {
        var response = await client.PostAsJsonAsync(LinesPath, new { skuId, quantity }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    public static Task<JsonElement> GetAsync(HttpClient client) =>
        client.GetFromJsonAsync<JsonElement>(BasketPath, TestContext.Current.CancellationToken);
}
