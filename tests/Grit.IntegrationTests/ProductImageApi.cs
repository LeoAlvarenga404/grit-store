using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

internal static class ProductImageApi
{
    // Talks to the presigned URLs directly, like the BFF does.
    internal static readonly HttpClient Storage = new();

    // The colour of the SKU BasketApi.CreateSkuAsync adds.
    public const string SkuColor = "azul";

    public static async Task<(Guid Id, uint Version, string Slug)> ProductWithSkuAsync(HttpClient admin)
    {
        var (productId, version, _) = await BasketApi.CreateSkuAsync(admin);
        var slug = (await CatalogAdminApi.GetProductAsync(admin, productId)).GetProperty("slug").GetString()!;
        return (productId, version, slug);
    }

    public static Task<HttpResponseMessage> StartUploadResponseAsync(HttpClient admin, Guid productId, string contentType, long sizeInBytes) =>
        admin.PostAsJsonAsync($"{CatalogAdminApi.ProductsPath}/{productId}/images/uploads", new { contentType, sizeInBytes }, TestContext.Current.CancellationToken);

    public static async Task<JsonElement> StartUploadAsync(HttpClient admin, Guid productId, string contentType, long sizeInBytes)
    {
        var response = await StartUploadResponseAsync(admin, productId, contentType, sizeInBytes);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await CatalogAdminApi.ReadAsync(response);
    }

    public static async Task<HttpResponseMessage> PutAsync(JsonElement upload, string contentType, int sizeInBytes)
    {
        using var content = new ByteArrayContent(new byte[sizeInBytes]);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return await Storage.PutAsync(upload.GetProperty("uploadUrl").GetString(), content, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> AttachAsync(HttpClient admin, Guid productId, string key, uint version, string? color = SkuColor) =>
        admin.PostAsJsonAsync($"{CatalogAdminApi.ProductsPath}/{productId}/images", new { key, version, color }, TestContext.Current.CancellationToken);

    public static string FileNameOf(string key) => key[(key.LastIndexOf('/') + 1)..];

    public static async Task<uint> AttachImageAsync(HttpClient admin, Guid productId, uint version, int sizeInBytes, string? color = SkuColor)
    {
        var upload = await StartUploadAsync(admin, productId, "image/png", sizeInBytes);
        await PutAsync(upload, "image/png", sizeInBytes);
        var response = await AttachAsync(admin, productId, upload.GetProperty("key").GetString()!, version, color);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await CatalogAdminApi.ReadAsync(response)).GetProperty("version").GetUInt32();
    }
}
