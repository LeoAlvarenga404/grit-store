using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Grit.IntegrationTests;

[Collection(SearchCorpusDefinition.Name)]
public class ProductFacetTests(SearchCorpus corpus)
{
    private const string FacetsPath = "/api/v1/catalog/facets";

    [Fact]
    public async Task Facets_NoFilters_CountEveryProductForSale()
    {
        var facets = await corpus.GetAsync(FacetsPath);

        Categories(facets).ShouldBe(["camping Camping 4", "calcados Calçados 3", "vestuario Vestuário 3"]);
        Prices(facets).ShouldBe(["0-100 2", "100-250 3", "250-500 2", "500-1000 3", "1000- 1"]);
        Attributes(facets).ShouldBe(
        [
            "capacidade=4 1",
            "cor=preto 5", "cor=azul 3", "cor=verde 2", "cor=marrom 1", "cor=vermelho 1",
            "litros=60 1",
            "tamanho=40 2", "tamanho=42 2", "tamanho=G 2", "tamanho=M 2", "tamanho=41 1",
        ]);
    }

    [Fact]
    public async Task Facets_Query_CountOnlyTheProductsFound()
    {
        var facets = await corpus.GetAsync($"{FacetsPath}?q=tenis");

        Categories(facets).ShouldBe(["calcados Calçados 2"]);
        Prices(facets).ShouldBe(["250-500 1", "500-1000 1"]);
        Attributes(facets).ShouldBe(["cor=preto 2", "cor=azul 1", "tamanho=40 2", "tamanho=41 1", "tamanho=42 1"]);
    }

    [Fact]
    public async Task Facets_Attribute_CountOnlyTheMatchingSkus()
    {
        var facets = await corpus.GetAsync($"{FacetsPath}?attribute=cor:azul");

        Categories(facets).ShouldBe(["vestuario Vestuário 2", "calcados Calçados 1"]);
        Prices(facets).ShouldBe(["0-100 1", "250-500 2"]);
        Attributes(facets).ShouldBe(["cor=azul 3", "tamanho=40 1", "tamanho=42 1", "tamanho=G 1", "tamanho=M 1"]);
    }

    [Fact]
    public async Task Facets_CategoryAndPrice_CountOnlyThatSlice()
    {
        var facets = await corpus.GetAsync($"{FacetsPath}?category=camping&maxPrice=100");

        Categories(facets).ShouldBe(["camping Camping 2"]);
        Prices(facets).ShouldBe(["0-100 1", "100-250 1"]);
        Attributes(facets).ShouldBe(["cor=preto 2"]);
    }

    [Fact]
    public async Task Facets_PriceAndAttribute_CountOnlySkusThatMatchBoth()
    {
        var blueInRange = await corpus.GetAsync($"{FacetsPath}?attribute=cor:azul&minPrice=100&maxPrice=250");
        var redInRange = await corpus.GetAsync($"{FacetsPath}?attribute=cor:vermelho&minPrice=100&maxPrice=250");

        Categories(blueInRange).ShouldBeEmpty();
        Prices(blueInRange).ShouldBeEmpty();
        Attributes(blueInRange).ShouldBeEmpty();
        Categories(redInRange).ShouldBe(["vestuario Vestuário 1"]);
        Prices(redInRange).ShouldBe(["100-250 1"]);
        Attributes(redInRange).ShouldBe(["cor=vermelho 1", "tamanho=G 1"]);
    }

    // Each range count is what the search lists for that range: Camiseta Dry Fit (79.90 and 119.90) is in 0–100 and 100–250.
    [Theory]
    [InlineData("0-100 2", "minPrice=0&maxPrice=99.99")]
    [InlineData("100-250 3", "minPrice=100&maxPrice=249.99")]
    [InlineData("250-500 2", "minPrice=250&maxPrice=499.99")]
    public async Task Facets_PriceRange_CountsWhatTheSearchListsForIt(string range, string refinement)
    {
        var facets = await corpus.GetAsync(FacetsPath);
        var names = await corpus.SearchNamesAsync(refinement);

        Prices(facets).ShouldContain(range);
        names.Count.ShouldBe(int.Parse(range.Split(' ')[1], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Facets_BlankParameters_CountEveryProductForSale()
    {
        var facets = await corpus.GetAsync($"{FacetsPath}?q=%20%20&category=");

        Categories(facets).ShouldBe(["camping Camping 4", "calcados Calçados 3", "vestuario Vestuário 3"]);
    }

    // Binding fails before validation, so the framework answers without a code (ADR 0006).
    [Fact]
    public async Task Facets_PriceThatIsNotANumber_Returns400WithoutCode()
    {
        var response = await corpus.Client.GetAsync($"{FacetsPath}?minPrice=abc", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.TryGetProperty("code", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Facets_NothingFound_ReturnsEmptyLists()
    {
        var facets = await corpus.GetAsync($"{FacetsPath}?q=xyzw");

        Categories(facets).ShouldBeEmpty();
        Prices(facets).ShouldBeEmpty();
        Attributes(facets).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("minPrice=-1", "minPrice")]
    [InlineData("attribute=cor", "attribute")]
    [InlineData("q=a%00", "q")]
    [InlineData("category=a%00", "category")]
    public async Task Facets_InvalidParameter_Returns400WithAFieldError(string queryString, string field)
    {
        var response = await corpus.Client.GetAsync($"{FacetsPath}?{queryString}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).ShouldBe([field]);
    }

    private static List<string> Categories(JsonElement facets) =>
        [.. facets.GetProperty("categories").EnumerateArray().Select(category =>
            $"{category.GetProperty("slug").GetString()} {category.GetProperty("name").GetString()} {category.GetProperty("count").GetInt64()}")];

    private static List<string> Prices(JsonElement facets) =>
        [.. facets.GetProperty("prices").EnumerateArray().Select(price => string.Create(
            CultureInfo.InvariantCulture,
            $"{price.GetProperty("minAmount").GetDecimal()}-{(price.GetProperty("maxAmount").ValueKind == JsonValueKind.Null ? "" : price.GetProperty("maxAmount").GetDecimal())} {price.GetProperty("count").GetInt64()}"))];

    private static List<string> Attributes(JsonElement facets) =>
        [.. facets.GetProperty("attributes").EnumerateArray().SelectMany(attribute => attribute.GetProperty("values").EnumerateArray().Select(value =>
            $"{attribute.GetProperty("key").GetString()}={value.GetProperty("value").GetString()} {value.GetProperty("count").GetInt64()}"))];
}
