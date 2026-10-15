using System.Net;
using System.Net.Http.Json;

namespace Grit.IntegrationTests;

public class CatalogProductTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateCategory_NewName_Returns201WithSlug()
    {
        var name = CatalogAdminApi.UniqueName("Camping & Trilha");

        var response = await _client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, new { name }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await CatalogAdminApi.ReadAsync(response);
        created.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        created.GetProperty("slug").GetString()!.ShouldStartWith("camping-trilha-");
    }

    [Fact]
    public async Task CreateCategory_NameWithTakenSlug_Returns409()
    {
        var name = CatalogAdminApi.UniqueName("Ciclismo");
        await _client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, new { name }, TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, new { name = name.ToUpperInvariant() }, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("catalog.category_slug_taken");
    }

    [Fact]
    public async Task CreateProduct_ValidInput_Returns201AndTheProductCanBeRead()
    {
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(_client);
        var name = CatalogAdminApi.UniqueName("Barraca Iglu");
        var body = new { categoryId, name, description = "Leve.", attributes = new { capacidade = "2" } };

        var response = await _client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await CatalogAdminApi.ReadAsync(response);
        var id = created.GetProperty("id").GetGuid();
        response.Headers.Location!.ToString().ShouldBe($"{CatalogAdminApi.ProductsPath}/{id}");
        var product = await CatalogAdminApi.GetProductAsync(_client, id);
        product.GetProperty("categoryId").GetGuid().ShouldBe(categoryId);
        product.GetProperty("name").GetString().ShouldBe(name);
        product.GetProperty("slug").GetString().ShouldBe(created.GetProperty("slug").GetString());
        product.GetProperty("slug").GetString()!.ShouldStartWith("barraca-iglu-");
        product.GetProperty("attributes").GetProperty("capacidade").GetString().ShouldBe("2");
        product.GetProperty("version").GetUInt32().ShouldBe(created.GetProperty("version").GetUInt32());
        product.GetProperty("skus").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task CreateProduct_UnknownCategory_Returns400()
    {
        var body = new { categoryId = Guid.NewGuid(), name = CatalogAdminApi.UniqueName("Bike"), description = "" };

        var response = await _client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("catalog.category_not_found");
    }

    [Fact]
    public async Task CreateProduct_UppercaseAttributeKey_Returns400WithFieldError()
    {
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(_client);
        var body = new { categoryId, name = CatalogAdminApi.UniqueName("Bike"), description = "", attributes = new Dictionary<string, string> { ["Aro"] = "29" } };

        var response = await _client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").GetProperty("attributes").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task CreateProduct_NameWithTakenSlug_Returns409()
    {
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(_client);
        var name = CatalogAdminApi.UniqueName("Garrafa");
        await _client.PostAsJsonAsync(CatalogAdminApi.ProductsPath, new { categoryId, name, description = "" }, TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync(
            CatalogAdminApi.ProductsPath, new { categoryId, name = name.ToUpperInvariant(), description = "" }, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_slug_taken");
    }

    [Fact]
    public async Task UpdateProduct_CurrentVersion_Returns200AndKeepsTheSlug()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var slug = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("slug").GetString();
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(_client);
        var body = new { categoryId, name = "Camiseta Nova", description = "Nova.", attributes = new { material = "algodao" }, version };

        var response = await _client.PutAsJsonAsync($"{CatalogAdminApi.ProductsPath}/{id}", body, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newVersion = (await CatalogAdminApi.ReadAsync(response)).GetProperty("version").GetUInt32();
        newVersion.ShouldNotBe(version);
        var product = await CatalogAdminApi.GetProductAsync(_client, id);
        product.GetProperty("categoryId").GetGuid().ShouldBe(categoryId);
        product.GetProperty("name").GetString().ShouldBe("Camiseta Nova");
        product.GetProperty("description").GetString().ShouldBe("Nova.");
        product.GetProperty("attributes").GetProperty("material").GetString().ShouldBe("algodao");
        product.GetProperty("slug").GetString().ShouldBe(slug);
        product.GetProperty("version").GetUInt32().ShouldBe(newVersion);
    }

    [Fact]
    public async Task UpdateProduct_StaleVersion_Returns409ProductChanged()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var categoryId = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("categoryId").GetGuid();
        var path = $"{CatalogAdminApi.ProductsPath}/{id}";
        await _client.PutAsJsonAsync(path, new { categoryId, name = "Primeira", description = "", version }, TestContext.Current.CancellationToken);

        var response = await _client.PutAsJsonAsync(path, new { categoryId, name = "Segunda", description = "", version }, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_changed");
        (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("name").GetString().ShouldBe("Primeira");
    }

    [Fact]
    public async Task UpdateProduct_UnknownCategory_Returns400AndKeepsTheProduct()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var body = new { categoryId = Guid.NewGuid(), name = "Outro Nome", description = "", version };

        var response = await _client.PutAsJsonAsync($"{CatalogAdminApi.ProductsPath}/{id}", body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("catalog.category_not_found");
        var product = await CatalogAdminApi.GetProductAsync(_client, id);
        product.GetProperty("name").GetString().ShouldNotBe("Outro Nome");
        product.GetProperty("version").GetUInt32().ShouldBe(version);
    }

    [Fact]
    public async Task UpdateProduct_InvalidAttributes_Returns400AndKeepsTheProduct()
    {
        var (id, version) = await CatalogAdminApi.CreateProductAsync(_client);
        var categoryId = (await CatalogAdminApi.GetProductAsync(_client, id)).GetProperty("categoryId").GetGuid();
        var attributes = new Dictionary<string, string> { ["material"] = "" };
        var body = new { categoryId, name = "Outro Nome", description = "", attributes, version };

        var response = await _client.PutAsJsonAsync($"{CatalogAdminApi.ProductsPath}/{id}", body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").GetProperty("attributes").GetArrayLength().ShouldBe(1);
        var product = await CatalogAdminApi.GetProductAsync(_client, id);
        product.GetProperty("attributes").GetProperty("material").GetString().ShouldBe("poliester");
    }

    [Fact]
    public async Task UpdateProduct_UnknownId_Returns404()
    {
        var categoryId = await CatalogAdminApi.CreateCategoryAsync(_client);
        var body = new { categoryId, name = "Qualquer", description = "", version = 1 };

        var response = await _client.PutAsJsonAsync($"{CatalogAdminApi.ProductsPath}/{Guid.NewGuid()}", body, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }

    [Fact]
    public async Task GetProduct_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"{CatalogAdminApi.ProductsPath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("catalog.product_not_found");
    }
}
