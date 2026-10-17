using System.Text.Json;

namespace Grit.IntegrationTests;

[Collection(SearchCorpusDefinition.Name)]
public class ProductListingTests(SearchCorpus corpus)
{
    private static readonly string[] ProductsForSale =
    [
        "Tênis de Corrida Veloz", "Tênis de Trilha Serra", "Bota Impermeável Montanha",
        "Camiseta Dry Fit Masculina", "Camisetas Térmicas Kit 2", "Jaqueta Corta-Vento",
        "Barraca Iglu 4 Pessoas", "Caneca Térmica para Café", "Lanterna de Cabeça", "Mochila Cargueira 60 Litros",
    ];

    // PostgreSQL orders uuid byte by byte, which is the ordinal order of the "N" text.
    [Fact]
    public async Task Search_NoFilters_ListsEveryProductForSaleNewestIdFirst()
    {
        var page = await corpus.GetAsync($"{SearchCorpus.ProductsPath}?limit=50");

        var items = page.GetProperty("items").EnumerateArray().ToList();
        items.Select(item => item.GetProperty("name").GetString()).ShouldBe(ProductsForSale, ignoreOrder: true);
        var ids = items.Select(item => item.GetProperty("id").GetGuid().ToString("N")).ToList();
        ids.ShouldBe(ids.OrderDescending(StringComparer.Ordinal).ToList());
        page.GetProperty("nextCursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Search_BlankCategoryAndCursor_ListsEveryProductForSale()
    {
        var names = await corpus.SearchNamesAsync("category=&after=");

        names.ShouldBe(ProductsForSale, ignoreOrder: true);
    }

    [Fact]
    public async Task Search_Item_CarriesIdSlugNameAndCheapestPrice()
    {
        var page = await corpus.GetAsync($"{SearchCorpus.ProductsPath}?q=cafe");

        var item = page.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("id").GetGuid().ShouldBe(corpus.ProductIds["Caneca Térmica para Café"]);
        item.GetProperty("slug").GetString().ShouldBe("caneca-termica-para-cafe");
        item.GetProperty("name").GetString().ShouldBe("Caneca Térmica para Café");
        item.GetProperty("priceAmount").GetDecimal().ShouldBe(89.90m);
        item.GetProperty("priceCurrency").GetString().ShouldBe("BRL");
    }

    [Theory]
    [InlineData("category=camping", "Barraca Iglu 4 Pessoas", "Caneca Térmica para Café", "Lanterna de Cabeça", "Mochila Cargueira 60 Litros")]
    [InlineData("category=nao-existe")]
    [InlineData("minPrice=100&maxPrice=259.90", "Lanterna de Cabeça", "Camiseta Dry Fit Masculina", "Camisetas Térmicas Kit 2", "Jaqueta Corta-Vento")]
    [InlineData("attribute=cor:azul", "Tênis de Corrida Veloz", "Camiseta Dry Fit Masculina", "Jaqueta Corta-Vento")]
    [InlineData("attribute=capacidade:4", "Barraca Iglu 4 Pessoas")]
    [InlineData("attribute=cor:azul&attribute=tamanho:42", "Tênis de Corrida Veloz")]
    [InlineData("attribute=cor:preto&attribute=tamanho:42")]
    [InlineData("q=termica&category=camping", "Caneca Térmica para Café")]
    [InlineData("attribute=cor:azul&minPrice=100&maxPrice=250")]
    [InlineData("attribute=cor:vermelho&minPrice=100&maxPrice=250", "Camiseta Dry Fit Masculina")]
    public async Task Search_Filter_ListsOnlyMatchingProducts(string queryString, params string[] expected)
    {
        var names = await corpus.SearchNamesAsync(queryString);

        names.ShouldBe(expected, ignoreOrder: true);
    }

    [Theory]
    [InlineData("minPrice=380", "Tênis de Corrida Veloz", 399.90)]
    [InlineData("attribute=tamanho:42&q=tenis", "Tênis de Corrida Veloz", 379.90)]
    [InlineData("q=tenis", "Tênis de Corrida Veloz", 379.90)]
    [InlineData("minPrice=100&maxPrice=259.90", "Camiseta Dry Fit Masculina", 119.90)]
    [InlineData("attribute=cor:vermelho&minPrice=100&maxPrice=250", "Camiseta Dry Fit Masculina", 119.90)]
    public async Task Search_Filter_PricesTheProductAtItsCheapestMatchingSku(string queryString, string name, double price)
    {
        var page = await corpus.GetAsync($"{SearchCorpus.ProductsPath}?{queryString}");

        var item = page.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("name").GetString() == name);
        item.GetProperty("priceAmount").GetDecimal().ShouldBe((decimal)price);
    }

    [Theory]
    [InlineData("", 3)]
    [InlineData("category=camping", 2)]
    [InlineData("q=tenis", 1)]
    [InlineData("q=camiseta", 1)]
    [InlineData("q=trilha", 1)]
    public async Task Search_PagesFollowingNextCursor_ReturnTheFullListOnce(string queryString, int limit)
    {
        var full = Ids(await corpus.GetAsync($"{SearchCorpus.ProductsPath}?limit=50&{queryString}"));
        var paged = new List<Guid>();
        string? after = null;
        var pages = 0;

        do
        {
            var cursor = after is null ? string.Empty : $"&after={after}";
            var page = await corpus.GetAsync($"{SearchCorpus.ProductsPath}?limit={limit}&{queryString}{cursor}");
            var ids = Ids(page);
            ids.ShouldNotBeEmpty();
            paged.AddRange(ids);
            after = page.GetProperty("nextCursor").GetString();
            pages++;
        }
        while (after is not null && pages <= full.Count);

        paged.ShouldBe(full);
        pages.ShouldBe((full.Count + limit - 1) / limit);
    }

    private static List<Guid> Ids(JsonElement page) =>
        [.. page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())];
}
