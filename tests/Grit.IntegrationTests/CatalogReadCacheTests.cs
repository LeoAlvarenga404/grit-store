using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Grit.Modules.Catalog.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

// The only host with the cache on (ApiFactory turns it off). A row changed behind the API tells a cached read from a
// fresh one: the cached read still answers the old value.
public sealed class CatalogReadCacheTests(ApiFactory api) : IAsyncLifetime
{
    private const string CatalogPath = "/api/v1/catalog";

    private const string Description = "Mantém a água gelada.";

    private WebApplicationFactory<Program> _host = null!;

    private HttpClient _admin = null!;

    private HttpClient _client = null!;

    public ValueTask InitializeAsync()
    {
        _host = api.WithWebHostBuilder(builder => builder.UseSetting("Caching:Enabled", "true"));
        _admin = AdminClient.Create(_host);
        _client = _host.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _admin.Dispose();
        _client.Dispose();
        await _host.DisposeAsync();
    }

    [Fact]
    public async Task CatalogReads_ChangedBehindTheApi_AreServedFromTheCache()
    {
        var product = await CreatePublicProductAsync();
        var reads = await ReadAllAsync(product);

        await ChangeBehindTheApiAsync(product.Id, $"Cantil Renomeado {product.Token}", 300m);

        (await ReadAllAsync(product)).ShouldBe(reads);
    }

    [Fact]
    public async Task ProductPage_AfterAnAdminUpdate_ShowsTheNewName()
    {
        var product = await CreatePublicProductAsync();
        await GetTextAsync(DetailPath(product));

        var updated = await UpdateNameAsync(product, $"Cantil Atualizado {product.Token}");

        updated.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetJsonAsync(DetailPath(product))).GetProperty("name").GetString().ShouldBe($"Cantil Atualizado {product.Token}");
    }

    [Fact]
    public async Task ProductPage_WithoutSkus_IsShownOnceTheFirstSkuIsAdded()
    {
        var product = await CreateProductAsync();
        (await _client.GetAsync(DetailPath(product), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        await CatalogAdminApi.AddSkuAsync(_admin, product.Id, product.Version, "azul");

        (await _client.GetAsync(DetailPath(product), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchAndFacets_AfterAPriceChange_ShowTheNewPrice()
    {
        var product = await CreatePublicProductAsync();
        await GetTextAsync(SearchPath(product));
        await GetTextAsync(FacetsPath(product));

        var changed = await _admin.PutAsJsonAsync(
            $"{CatalogAdminApi.ProductsPath}/{product.Id}/skus/{product.SkuId}/price",
            new { priceAmount = 300m, priceCurrency = "BRL", version = product.Version },
            TestContext.Current.CancellationToken);

        changed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetJsonAsync(SearchPath(product))).GetProperty("items")[0].GetProperty("priceAmount").GetDecimal().ShouldBe(300m);
        var prices = (await GetJsonAsync(FacetsPath(product))).GetProperty("prices");
        prices.GetArrayLength().ShouldBe(1);
        prices[0].GetProperty("minAmount").GetDecimal().ShouldBe(250m);
    }

    [Fact]
    public async Task Suggestions_SpacesCaseAndAccents_ShareOneCachedEntry()
    {
        var product = await CreatePublicProductAsync();
        var first = await GetTextAsync(SuggestionsPath($"Térmico {product.Token}"));

        await ChangeBehindTheApiAsync(product.Id, $"Cantil Renomeado {product.Token}", 99.90m);

        (await GetTextAsync(SuggestionsPath($" TERMICO {product.Token.ToUpperInvariant()} "))).ShouldBe(first);
        JsonDocument.Parse(first).RootElement.EnumerateArray()
            .Select(suggestion => suggestion.GetProperty("name").GetString())
            .ShouldContain($"Cantil Térmico {product.Token}");
    }

    [Fact]
    public async Task ProductPage_AfterARefusedWrite_KeepsTheCachedPage()
    {
        var product = await CreatePublicProductAsync();
        var cached = await GetTextAsync(DetailPath(product));
        await ChangeBehindTheApiAsync(product.Id, $"Cantil Renomeado {product.Token}", 99.90m);

        // The change behind the API moved the row version, so this write is refused.
        var refused = await UpdateNameAsync(product, $"Cantil Recusado {product.Token}");

        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await GetTextAsync(DetailPath(product))).ShouldBe(cached);
    }

    [Fact]
    public async Task CachingDisabled_ReadsTheDatabaseEveryTime()
    {
        var product = await CreatePublicProductAsync();
        using var uncached = api.CreateClient();
        (await uncached.GetAsync(DetailPath(product), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);

        await ChangeBehindTheApiAsync(product.Id, $"Cantil Renomeado {product.Token}", 99.90m);

        var page = await uncached.GetFromJsonAsync<JsonElement>(DetailPath(product), TestContext.Current.CancellationToken);
        page.GetProperty("name").GetString().ShouldBe($"Cantil Renomeado {product.Token}");
    }

    private static string DetailPath(CachedProduct product) => $"{CatalogPath}/products/{product.Slug}";

    private static string SearchPath(CachedProduct product) => $"{CatalogPath}/products?q={product.Token}";

    private static string FacetsPath(CachedProduct product) => $"{CatalogPath}/facets?q={product.Token}";

    private static string SuggestionsPath(string text) => $"{CatalogPath}/suggestions?q={Uri.EscapeDataString(text)}";

    // Letters only, so the search and the suggestions read it as one ordinary word.
    private static string Token() => string.Concat(Guid.NewGuid().ToString("N")[..12].Select(character => (char)('a' + (character % 16))));

    private async Task<string[]> ReadAllAsync(CachedProduct product) =>
    [
        await GetTextAsync(DetailPath(product)),
        await GetTextAsync(SearchPath(product)),
        await GetTextAsync(FacetsPath(product)),
        await GetTextAsync(SuggestionsPath($"Térmico {product.Token}")),
    ];

    private async Task<string> GetTextAsync(string path)
    {
        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private async Task<JsonElement> GetJsonAsync(string path) => JsonDocument.Parse(await GetTextAsync(path)).RootElement;

    private async Task ChangeBehindTheApiAsync(Guid productId, string name, decimal priceAmount)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;

        await db.Database.ExecuteSqlAsync($"UPDATE catalog.products SET name = {name} WHERE id = {productId}", cancellationToken);
        await db.Database.ExecuteSqlAsync($"UPDATE catalog.skus SET price_amount = {priceAmount} WHERE product_id = {productId}", cancellationToken);
    }

    private Task<HttpResponseMessage> UpdateNameAsync(CachedProduct product, string name) =>
        _admin.PutAsJsonAsync(
            $"{CatalogAdminApi.ProductsPath}/{product.Id}",
            new { categoryId = product.CategoryId, name, description = Description, version = product.Version, attributes = new { material = "aco" } },
            TestContext.Current.CancellationToken);

    private async Task<CachedProduct> CreatePublicProductAsync()
    {
        var product = await CreateProductAsync();
        var sku = await CatalogAdminApi.AddSkuAsync(_admin, product.Id, product.Version, "azul");
        return product with { Version = sku.Version, SkuId = sku.Id };
    }

    private async Task<CachedProduct> CreateProductAsync()
    {
        var token = Token();
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(_admin);
        var response = await _admin.PostAsJsonAsync(
            CatalogAdminApi.ProductsPath,
            new { categoryId, name = $"Cantil Térmico {token}", description = Description, attributes = new { material = "aco" } },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await CatalogAdminApi.ReadAsync(response);
        var id = created.GetProperty("id").GetGuid();
        var slug = (await CatalogAdminApi.GetProductAsync(_admin, id)).GetProperty("slug").GetString()!;
        return new CachedProduct(id, categoryId, created.GetProperty("version").GetUInt32(), Guid.Empty, slug, token);
    }

    private sealed record CachedProduct(Guid Id, Guid CategoryId, uint Version, Guid SkuId, string Slug, string Token);
}
