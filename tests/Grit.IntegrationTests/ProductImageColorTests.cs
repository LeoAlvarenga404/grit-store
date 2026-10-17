using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

public class ProductImageColorTests(ProductImageHost images) : IClassFixture<ProductImageHost>
{
    private readonly HttpClient _admin = AdminClient.Create(images.Host);

    [Fact]
    public async Task AttachImage_ColorNoSkuHas_Returns400()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var key = await UploadAsync(productId);

        var response = await ProductImageApi.AttachAsync(_admin, productId, key, version, "verde");

        (await CodeOfAsync(response, HttpStatusCode.BadRequest)).ShouldBe("catalog.image_color_not_found");
    }

    // A retry of the very same request carries the version read before the first attach.
    [Fact]
    public async Task AttachImage_SameUploadAgain_ReturnsTheCurrentVersionAndKeepsOneImage()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var key = await UploadAsync(productId);
        var attached = await VersionOfAsync(await ProductImageApi.AttachAsync(_admin, productId, key, version));

        var again = await ProductImageApi.AttachAsync(_admin, productId, key, version);

        (await VersionOfAsync(again)).ShouldBe(attached);
        var image = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("images").EnumerateArray().ShouldHaveSingleItem();
        image.GetProperty("key").GetString().ShouldBe($"products/{productId}/{ProductImageApi.FileNameOf(key)}");
        image.GetProperty("color").GetString().ShouldBe("azul");
    }

    [Fact]
    public async Task AttachImage_SameUploadWithAnotherColor_Returns409()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var black = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "preto");
        var key = await UploadAsync(productId);
        var attached = await VersionOfAsync(await ProductImageApi.AttachAsync(_admin, productId, key, black.Version));

        var response = await ProductImageApi.AttachAsync(_admin, productId, key, attached, "preto");

        (await CodeOfAsync(response, HttpStatusCode.Conflict)).ShouldBe("catalog.duplicate_image");
    }

    [Fact]
    public async Task AttachImage_ColorWithNul_Returns400WithTheField()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var key = await UploadAsync(productId);

        var response = await ProductImageApi.AttachAsync(_admin, productId, key, version, "azul\0");

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).ShouldBe(["color"]);
    }

    [Fact]
    public async Task AttachImage_ColorAbove100Characters_Returns400WithTheField()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var key = await UploadAsync(productId);

        var response = await ProductImageApi.AttachAsync(_admin, productId, key, version, new string('c', 101));

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).ShouldBe(["color"]);
    }

    [Fact]
    public async Task AttachImage_ColorOf100Characters_AttachesIt()
    {
        var color = new string('c', 100);
        var (productId, productVersion) = await CatalogAdminApi.CreateProductAsync(_admin);
        var sku = await CatalogAdminApi.AddSkuAsync(_admin, productId, productVersion, color);
        var key = await UploadAsync(productId);

        var response = await ProductImageApi.AttachAsync(_admin, productId, key, sku.Version, color);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var image = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("images").EnumerateArray().ShouldHaveSingleItem();
        image.GetProperty("color").GetString().ShouldBe(color);
    }

    [Fact]
    public async Task AttachImage_SkusWithoutColor_AttachesTheImageWithoutColor()
    {
        var (productId, productVersion) = await CatalogAdminApi.CreateProductAsync(_admin);
        var sku = await _admin.PostAsJsonAsync(
            CatalogAdminApi.SkusPath(productId),
            new { attributes = new { tamanho = "U" }, priceAmount = 59.90m, priceCurrency = "BRL", version = productVersion },
            TestContext.Current.CancellationToken);
        sku.StatusCode.ShouldBe(HttpStatusCode.Created);
        var version = (await CatalogAdminApi.ReadAsync(sku)).GetProperty("version").GetUInt32();
        var key = await UploadAsync(productId);

        var response = await ProductImageApi.AttachAsync(_admin, productId, key, version, color: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var image = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("images").EnumerateArray().ShouldHaveSingleItem();
        image.GetProperty("color").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    private async Task<string> UploadAsync(Guid productId)
    {
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", 100);
        await ProductImageApi.PutAsync(upload, "image/png", 100);
        return upload.GetProperty("key").GetString()!;
    }

    private static async Task<uint> VersionOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await CatalogAdminApi.ReadAsync(response)).GetProperty("version").GetUInt32();
    }

    private static async Task<string?> CodeOfAsync(HttpResponseMessage response, HttpStatusCode status) =>
        (await ProblemResponse.ReadAsync(response, status)).GetProperty("code").GetString();
}
