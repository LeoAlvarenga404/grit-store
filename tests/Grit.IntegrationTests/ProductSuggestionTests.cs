using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

[Collection(SearchCorpusDefinition.Name)]
public class ProductSuggestionTests(SearchCorpus corpus, ApiFactory api)
{
    private const string SuggestionsPath = "/api/v1/catalog/suggestions";

    [Theory]
    [InlineData("cam", "Camiseta Dry Fit Masculina", "Camisetas Térmicas Kit 2")]
    [InlineData("te", "Tênis de Corrida Veloz", "Tênis de Trilha Serra", "Camisetas Térmicas Kit 2", "Caneca Térmica para Café")]
    [InlineData("TÊN", "Tênis de Corrida Veloz", "Tênis de Trilha Serra")]
    [InlineData("cafe", "Caneca Térmica para Café")]
    [InlineData("termica", "Caneca Térmica para Café", "Camisetas Térmicas Kit 2")]
    [InlineData("mochla", "Mochila Cargueira 60 Litros")]
    [InlineData("saco")]
    public async Task Suggestions_TypedText_ListsNamesThatStartWithItFirst(string query, params string[] expected)
    {
        var suggestions = await corpus.GetAsync($"{SuggestionsPath}?q={Uri.EscapeDataString(query)}");

        suggestions.EnumerateArray().Select(suggestion => suggestion.GetProperty("name").GetString()).ShouldBe(expected);
    }

    [Fact]
    public async Task Suggestions_Match_CarriesSlugAndName()
    {
        var suggestions = await corpus.GetAsync($"{SuggestionsPath}?q=lanterna");

        var suggestion = suggestions.EnumerateArray().ShouldHaveSingleItem();
        suggestion.GetProperty("slug").GetString().ShouldBe("lanterna-de-cabeca");
        suggestion.GetProperty("name").GetString().ShouldBe("Lanterna de Cabeça");
    }

    // A database of its own: ten products sharing a word would change every count of the search corpus.
    [Fact]
    public async Task Suggestions_MoreThanEightMatches_ReturnsTheFirstEightByName()
    {
        var connectionString = await api.CreateDatabaseAsync("suggestion_limit");
        await using var host = api.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:grit", connectionString));
        var client = host.CreateClient();
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(client);
        for (var number = 10; number >= 1; number--)
        {
            var body = new { categoryId, name = $"Caiaque {number:00}", description = "" };
            var created = await CatalogAdminApi.ReadAsync(await client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken));
            await CatalogAdminApi.AddSkuAsync(client, created.GetProperty("id").GetGuid(), created.GetProperty("version").GetUInt32(), "azul");
        }

        var suggestions = await client.GetFromJsonAsync<JsonElement>($"{SuggestionsPath}?q=caiaque", TestContext.Current.CancellationToken);

        suggestions.EnumerateArray().Select(suggestion => suggestion.GetProperty("name").GetString()).ShouldBe(
            ["Caiaque 01", "Caiaque 02", "Caiaque 03", "Caiaque 04", "Caiaque 05", "Caiaque 06", "Caiaque 07", "Caiaque 08"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?q=")]
    [InlineData("?q=a")]
    [InlineData("?q=%20a")]
    [InlineData("?q=ab%00")]
    [InlineData("?q=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Suggestions_TextMissingTooShortOrTooLong_Returns400(string queryString)
    {
        var response = await corpus.Client.GetAsync($"{SuggestionsPath}{queryString}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).ShouldBe(["q"]);
    }
}
