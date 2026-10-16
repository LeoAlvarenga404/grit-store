namespace Grit.IntegrationTests;

[Collection(SearchCorpusDefinition.Name)]
public class ProductSearchTests(SearchCorpus corpus)
{
    [Theory]
    [InlineData("tenis", "Tênis de Corrida Veloz", "Tênis de Trilha Serra")]
    [InlineData("TÊNIS", "Tênis de Corrida Veloz", "Tênis de Trilha Serra")]
    [InlineData("cafe", "Caneca Térmica para Café")]
    [InlineData("café", "Caneca Térmica para Café")]
    [InlineData("termica", "Caneca Térmica para Café", "Camisetas Térmicas Kit 2")]
    [InlineData("impermeavel", "Bota Impermeável Montanha")]
    public async Task Search_WordWithOrWithoutAccent_FindsTheSameProducts(string query, params string[] expected)
    {
        var names = await corpus.SearchNamesAsync($"q={Uri.EscapeDataString(query)}");

        names.ShouldBe(expected, ignoreOrder: true);
    }

    [Theory]
    [InlineData("camiseta", "Camiseta Dry Fit Masculina", "Camisetas Térmicas Kit 2")]
    [InlineData("camisetas", "Camiseta Dry Fit Masculina", "Camisetas Térmicas Kit 2")]
    [InlineData("barracas", "Barraca Iglu 4 Pessoas")]
    [InlineData("botas", "Bota Impermeável Montanha")]
    public async Task Search_PluralOrSingular_FindsTheSameProducts(string query, params string[] expected)
    {
        var names = await corpus.SearchNamesAsync($"q={query}");

        names.ShouldBe(expected, ignoreOrder: true);
    }

    [Theory]
    [InlineData("camizeta", "Camiseta Dry Fit Masculina")]
    [InlineData("mochla", "Mochila Cargueira 60 Litros")]
    [InlineData("térmca", "Caneca Térmica para Café")]
    [InlineData("jaquta", "Jaqueta Corta-Vento")]
    public async Task Search_OneWrongLetter_FindsTheProduct(string query, string expected)
    {
        var names = await corpus.SearchNamesAsync($"q={Uri.EscapeDataString(query)}");

        names.ShouldBe([expected]);
    }

    [Theory]
    [InlineData("camisa")]
    [InlineData("xyzw")]
    [InlineData("!!!")]
    public async Task Search_WordInNoProduct_FindsNothing(string query)
    {
        var names = await corpus.SearchNamesAsync($"q={Uri.EscapeDataString(query)}");

        names.ShouldBeEmpty();
    }

    [Fact]
    public async Task Search_WordOnlyInTheDescription_FindsTheProduct()
    {
        var names = await corpus.SearchNamesAsync("q=garoa");

        names.ShouldBe(["Jaqueta Corta-Vento"]);
    }

    [Fact]
    public async Task Search_WordInOneNameAndAnotherDescription_RanksTheNameFirst()
    {
        var names = await corpus.SearchNamesAsync("q=trilha");

        names.ShouldBe(["Tênis de Trilha Serra", "Lanterna de Cabeça"]);
    }

    // Both names match "tenis" equally; only the full-text rank of the word in the older product's description orders them.
    [Fact]
    public async Task Search_WordAlsoInTheDescription_RanksThatProductFirst()
    {
        var names = await corpus.SearchNamesAsync("q=tenis");

        names.ShouldBe(["Tênis de Corrida Veloz", "Tênis de Trilha Serra"]);
    }

    [Theory]
    [InlineData("camiseta", "Camiseta Dry Fit Masculina", "Camisetas Térmicas Kit 2")]
    [InlineData("camisetas", "Camisetas Térmicas Kit 2", "Camiseta Dry Fit Masculina")]
    public async Task Search_SameStem_RanksTheExactWordFirst(string query, params string[] expected)
    {
        var names = await corpus.SearchNamesAsync($"q={query}");

        names.ShouldBe(expected);
    }

    [Fact]
    public async Task Search_ProductWithoutSkus_IsNeverFound()
    {
        var names = await corpus.SearchNamesAsync("q=saco%20de%20dormir");

        names.ShouldBeEmpty();
    }

    [Fact]
    public async Task Search_BlankQuery_ListsEveryProductForSale()
    {
        var names = await corpus.SearchNamesAsync("q=%20%20");

        names.Count.ShouldBe(10);
    }
}
