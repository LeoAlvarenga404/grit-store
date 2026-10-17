using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

// Three products in a colour no other test uses: two at the same price, one cheaper. Ties fall back to the id, which
// PostgreSQL orders byte by byte like the ordinal order of the "N" text.
public class ProductSortTests(ApiFactory factory)
{
    private readonly HttpClient _admin = AdminClient.Create(factory);

    [Fact]
    public async Task Search_SortByPriceAscending_OrdersTiesByIdAscending()
    {
        var (color, cheaper, tied) = await CreateProductsAsync();

        var ids = await IdsAsync($"attribute=cor:{color}&sort=price_asc&limit=50");

        ids.ShouldBe([cheaper, .. tied.OrderBy(Text, StringComparer.Ordinal)]);
    }

    [Fact]
    public async Task Search_SortByPriceDescending_OrdersTiesByIdDescending()
    {
        var (color, cheaper, tied) = await CreateProductsAsync();

        var ids = await IdsAsync($"attribute=cor:{color}&sort=price_desc&limit=50");

        ids.ShouldBe([.. tied.OrderByDescending(Text, StringComparer.Ordinal), cheaper]);
    }

    [Theory]
    [InlineData("price_asc")]
    [InlineData("price_desc")]
    public async Task Search_PagesSortedByPriceWithTies_ReturnEachProductOnce(string sort)
    {
        var (color, _, _) = await CreateProductsAsync();
        var full = await IdsAsync($"attribute=cor:{color}&sort={sort}&limit=50");
        var paged = new List<Guid>();
        string? after = null;

        do
        {
            var page = await PageAsync($"attribute=cor:{color}&sort={sort}&limit=1" + (after is null ? "" : $"&after={after}"));
            paged.AddRange(Ids(page));
            after = page.GetProperty("nextCursor").GetString();
        }
        while (after is not null && paged.Count <= full.Count);

        full.Count.ShouldBe(3);
        paged.ShouldBe(full);
    }

    private static string Text(Guid id) => id.ToString("N");

    private async Task<(string Color, Guid Cheaper, Guid[] Tied)> CreateProductsAsync()
    {
        var color = Guid.NewGuid().ToString("N");
        var first = await CreateProductAsync(color, 50.00m);
        var second = await CreateProductAsync(color, 50.00m);
        var cheaper = await CreateProductAsync(color, 40.00m);
        return (color, cheaper, [first, second]);
    }

    private async Task<Guid> CreateProductAsync(string color, decimal price)
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_admin);
        var sku = await _admin.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(id),
            new { attributes = new { cor = color }, priceAmount = price, priceCurrency = "BRL", version },
            TestContext.Current.CancellationToken);
        sku.StatusCode.ShouldBe(HttpStatusCode.Created);
        return id;
    }

    private Task<JsonElement> PageAsync(string queryString) =>
        _admin.GetFromJsonAsync<JsonElement>($"/api/v1/catalog/products?{queryString}", TestContext.Current.CancellationToken);

    private async Task<List<Guid>> IdsAsync(string queryString) => Ids(await PageAsync(queryString));

    private static List<Guid> Ids(JsonElement page) =>
        [.. page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())];
}
