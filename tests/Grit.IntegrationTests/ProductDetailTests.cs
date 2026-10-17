using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

public class ProductDetailTests(ApiFactory factory)
{
    private const string ProductsPath = "/api/v1/catalog/products";

    private readonly HttpClient _client = AdminClient.Create(factory);

    [Fact]
    public async Task GetProductBySlug_ProductWithSkus_ReturnsCategoryAttributesAndEachSku()
    {
        var categoryName = CatalogAdminApi.UniqueName("Trilha");
        var categoryResponse = await _client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, new { name = categoryName }, TestContext.Current.CancellationToken);
        var category = await CatalogAdminApi.ReadAsync(categoryResponse);
        var categoryId = category.GetProperty("id").GetGuid();
        var productResponse = await _client.PostAsJsonAsync(
            CatalogAdminApi.ProductsPath,
            new { categoryId, name = CatalogAdminApi.UniqueName("Bastão de Caminhada"), description = "Alumínio.", attributes = new { material = "aluminio" } },
            TestContext.Current.CancellationToken);
        var created = await CatalogAdminApi.ReadAsync(productResponse);
        var id = created.GetProperty("id").GetGuid();
        var blue = await AddSkuAsync(id, created.GetProperty("version").GetUInt32(), "azul", 79.90m);
        var black = await AddSkuAsync(id, blue.Version, "preto", 119.90m);
        var slug = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("slug").GetString();

        var product = await _client.GetFromJsonAsync<JsonElement>($"{ProductsPath}/{slug}", TestContext.Current.CancellationToken);

        product.GetProperty("id").GetGuid().ShouldBe(id);
        product.GetProperty("slug").GetString().ShouldBe(slug);
        product.GetProperty("name").GetString().ShouldStartWith("Bastão de Caminhada");
        product.GetProperty("description").GetString().ShouldBe("Alumínio.");
        product.GetProperty("attributes").GetProperty("material").GetString().ShouldBe("aluminio");
        product.GetProperty("categorySlug").GetString().ShouldBe(category.GetProperty("slug").GetString());
        product.GetProperty("categoryName").GetString().ShouldBe(categoryName);
        var skus = product.GetProperty("skus").EnumerateArray().ToDictionary(
            sku => sku.GetProperty("id").GetGuid(),
            sku => (sku.GetProperty("attributes").GetProperty("cor").GetString(), sku.GetProperty("priceAmount").GetDecimal(), sku.GetProperty("priceCurrency").GetString()));
        skus.Count.ShouldBe(2);
        skus[blue.Id].ShouldBe(("azul", 79.90m, "BRL"));
        skus[black.Id].ShouldBe(("preto", 119.90m, "BRL"));
    }

    [Fact]
    public async Task GetProductBySlug_UnknownSlug_Returns404()
    {
        var response = await _client.GetAsync($"{ProductsPath}/nao-existe-{Guid.NewGuid():N}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }

    [Fact]
    public async Task GetProductBySlug_ProductWithoutSkus_Returns404()
    {
        var (id, _) = await CatalogAdminApi.CreateProductAsync(_client);
        var slug = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("slug").GetString();

        var response = await _client.GetAsync($"{ProductsPath}/{slug}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }

    // string.Normalize throws on U+FFFE (UTF-8 EF BF BE): the slug shape check answers before any normalisation.
    [Fact]
    public async Task GetProductBySlug_SlugWithNoncharacter_Returns404()
    {
        var response = await _client.GetAsync($"{ProductsPath}/x%EF%BF%BE", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }

    [Fact]
    public async Task GetProductBySlug_SlugWithDoubleHyphen_Returns404()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        await CatalogAdminApi.AddSkuAsync(_client, id, version, "azul");
        var slug = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("slug").GetString()!;

        var response = await _client.GetAsync($"{ProductsPath}/{slug.Replace("-", "--", StringComparison.Ordinal)}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }

    [Fact]
    public async Task GetProductBySlug_SlugInUppercase_Returns404()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        await CatalogAdminApi.AddSkuAsync(_client, id, version, "azul");
        var slug = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("slug").GetString()!;

        var response = await _client.GetAsync($"{ProductsPath}/{slug.ToUpperInvariant()}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }

    private async Task<(Guid Id, uint Version)> AddSkuAsync(Guid productId, uint version, string color, decimal price)
    {
        var body = new { attributes = new { cor = color }, priceAmount = price, priceCurrency = "BRL", version };
        var created = await CatalogAdminApi.ReadAsync(await _client.PostAsJsonAsync(CatalogAdminApi.SkusPath(productId), body, TestContext.Current.CancellationToken));
        return (created.GetProperty("id").GetGuid(), created.GetProperty("version").GetUInt32());
    }
}
