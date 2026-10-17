using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Grit.IntegrationTests;

// Exact counts need a database no other test writes to: this one holds only the products below, seeded once
// through the admin API. "Saco de Dormir Polar" has no SKU, so no public read may ever return it.
public sealed class SearchCorpus(ApiFactory api) : IAsyncLifetime
{
    public const string ProductsPath = "/api/v1/catalog/products";

    private readonly Dictionary<string, Guid> _productIds = [];

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public IReadOnlyDictionary<string, Guid> ProductIds => _productIds;

    public async ValueTask InitializeAsync()
    {
        var connectionString = await api.CreateDatabaseAsync("search_corpus");
        _factory = api.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:grit", connectionString));
        Client = AdminClient.Create(_factory);

        var calcados = await CreateCategoryAsync("Calçados");
        var vestuario = await CreateCategoryAsync("Vestuário");
        var camping = await CreateCategoryAsync("Camping");

        await CreateProductAsync(calcados, "Tênis de Corrida Veloz", "Tênis com amortecimento para asfalto.", [], (399.90m, "cor:azul tamanho:40"), (399.90m, "cor:preto tamanho:41"), (379.90m, "cor:azul tamanho:42"));
        await CreateProductAsync(calcados, "Tênis de Trilha Serra", "Solado com cravos para terra.", [], (549.90m, "cor:preto tamanho:40"));
        await CreateProductAsync(calcados, "Bota Impermeável Montanha", "Couro e membrana impermeável.", [], (899.00m, "cor:marrom tamanho:42"));
        await CreateProductAsync(vestuario, "Camiseta Dry Fit Masculina", "Tecido leve que seca rápido.", [], (79.90m, "cor:azul tamanho:M"), (119.90m, "cor:vermelho tamanho:G"));
        await CreateProductAsync(vestuario, "Camisetas Térmicas Kit 2", "Duas camisetas de segunda pele.", [], (149.90m, "cor:preto tamanho:M"));
        await CreateProductAsync(vestuario, "Jaqueta Corta-Vento", "Proteção contra vento e garoa.", [], (259.90m, "cor:azul tamanho:G"));
        await CreateProductAsync(camping, "Barraca Iglu 4 Pessoas", "Montagem rápida.", ["capacidade:4"], (699.00m, "cor:verde"));
        await CreateProductAsync(camping, "Caneca Térmica para Café", "Mantém o café quente por 6 horas.", [], (89.90m, "cor:preto"));
        await CreateProductAsync(camping, "Lanterna de Cabeça", "Ilumina a trilha à noite.", [], (100.00m, "cor:preto"));
        await CreateProductAsync(camping, "Mochila Cargueira 60 Litros", "Costas ventiladas.", ["litros:60"], (1199.00m, "cor:verde"));
        await CreateProductAsync(camping, "Saco de Dormir Polar", "Conforto até 5 graus.", []);
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    public async Task<JsonElement> GetAsync(string pathAndQuery)
    {
        var response = await Client.GetAsync(pathAndQuery, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    public async Task<List<string>> SearchNamesAsync(string queryString)
    {
        var page = await GetAsync($"{ProductsPath}?limit=50&{queryString}");
        return [.. page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()!)];
    }

    private async Task<Guid> CreateCategoryAsync(string name)
    {
        var response = await Client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, new { name }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await CatalogAdminApi.ReadAsync(response)).GetProperty("id").GetGuid();
    }

    // Attributes are written "key:value key:value" to keep the corpus table above readable.
    private async Task CreateProductAsync(
        Guid categoryId,
        string name,
        string description,
        string[] attributes,
        params (decimal Price, string Attributes)[] skus)
    {
        var body = new { categoryId, name, description, attributes = ToDictionary(attributes) };
        var response = await Client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await CatalogAdminApi.ReadAsync(response);
        var id = created.GetProperty("id").GetGuid();
        var version = created.GetProperty("version").GetUInt32();

        foreach (var sku in skus)
        {
            var skuBody = new { attributes = ToDictionary(sku.Attributes.Split(' ')), priceAmount = sku.Price, priceCurrency = "BRL", version };
            var skuResponse = await Client.PostAsJsonAsync(CatalogAdminApi.SkusPath(id), skuBody, TestContext.Current.CancellationToken);
            skuResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
            version = (await CatalogAdminApi.ReadAsync(skuResponse)).GetProperty("version").GetUInt32();
        }

        _productIds[name] = id;
    }

    private static Dictionary<string, string> ToDictionary(string[] pairs) =>
        pairs.Select(pair => pair.Split(':')).ToDictionary(pair => pair[0], pair => pair[1]);
}
