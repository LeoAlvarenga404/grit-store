using System.Net;
using System.Text.Json;

namespace Grit.IntegrationTests;

// A product with azul and preto SKUs per test: the admin gallery keeps each image's own colour, the limit counts per
// colour, and a new image needs the current version.
public class AdminImageGalleryTests(ProductImageHost images) : IClassFixture<ProductImageHost>
{
    private readonly HttpClient _admin = AdminClient.Create(images.Host);

    [Fact]
    public async Task GetProduct_ImagesOfTwoColors_ListsEachWithItsOwnColorAndFile()
    {
        var (productId, version) = await ProductWithTwoColorsAsync();
        var blueKey = await UploadAsync(productId, 100);
        version = await VersionOfAsync(await ProductImageApi.AttachAsync(_admin, productId, blueKey, version, "azul"));
        var blackKey = await UploadAsync(productId, 200);
        await VersionOfAsync(await ProductImageApi.AttachAsync(_admin, productId, blackKey, version, "preto"));

        var gallery = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("images").EnumerateArray().ToList();

        gallery.Select(image => (image.GetProperty("fileName").GetString(), image.GetProperty("color").GetString()))
            .ShouldBe([(ProductImageApi.FileNameOf(blueKey), "azul"), (ProductImageApi.FileNameOf(blackKey), "preto")]);
        (await SizeOfAsync(gallery[0])).ShouldBe(100);
        (await SizeOfAsync(gallery[1])).ShouldBe(200);
    }

    [Fact]
    public async Task AttachImage_NewKeyWithAStaleVersion_Returns409AndKeepsTheFirstImage()
    {
        var (productId, version) = await ProductWithTwoColorsAsync();
        var firstKey = await UploadAsync(productId, 100);
        var secondKey = await UploadAsync(productId, 100);
        await VersionOfAsync(await ProductImageApi.AttachAsync(_admin, productId, firstKey, version, "azul"));

        var response = await ProductImageApi.AttachAsync(_admin, productId, secondKey, version, "preto");

        (await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict)).GetProperty("code").GetString().ShouldBe("catalog.product_changed");
        var image = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("images").EnumerateArray().ShouldHaveSingleItem();
        image.GetProperty("fileName").GetString().ShouldBe(ProductImageApi.FileNameOf(firstKey));
        image.GetProperty("color").GetString().ShouldBe("azul");
        (await ProductImageApi.AttachAsync(_admin, productId, firstKey, version, "azul")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AttachImage_NinthImageOfAColor_Returns409WhileAnotherColorStillAttaches()
    {
        var (productId, version) = await ProductWithTwoColorsAsync();
        for (var index = 0; index < 8; index++)
        {
            version = await ProductImageApi.AttachImageAsync(_admin, productId, version, 100, "azul");
        }

        version = await ProductImageApi.AttachImageAsync(_admin, productId, version, 100, "preto");
        var response = await ProductImageApi.AttachAsync(_admin, productId, await UploadAsync(productId, 100), version, "azul");

        (await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict)).GetProperty("code").GetString().ShouldBe("catalog.image_limit");
        var colors = (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("images").EnumerateArray()
            .Select(image => image.GetProperty("color").GetString()).ToList();
        colors.Count(color => color == "azul").ShouldBe(8);
        colors.Count(color => color == "preto").ShouldBe(1);
    }

    private async Task<(Guid Id, uint Version)> ProductWithTwoColorsAsync()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var black = await CatalogAdminApi.AddSkuAsync(_admin, productId, version, "preto");
        return (productId, black.Version);
    }

    private async Task<string> UploadAsync(Guid productId, int sizeInBytes)
    {
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", sizeInBytes);
        await ProductImageApi.PutAsync(upload, "image/png", sizeInBytes);
        return upload.GetProperty("key").GetString()!;
    }

    private static async Task<uint> VersionOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await CatalogAdminApi.ReadAsync(response)).GetProperty("version").GetUInt32();
    }

    private static async Task<int> SizeOfAsync(JsonElement image) =>
        (await ProductImageApi.Storage.GetByteArrayAsync(image.GetProperty("url").GetString(), TestContext.Current.CancellationToken)).Length;
}
