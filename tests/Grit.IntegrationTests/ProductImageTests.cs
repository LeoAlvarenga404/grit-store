using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Grit.Modules.Catalog.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class ProductImageTests(ProductImageHost images) : IClassFixture<ProductImageHost>
{
    private readonly HttpClient _admin = AdminClient.Create(images.Host);

    [Fact]
    public async Task UploadAndAttach_PngImage_ShowsOnTheAdminAndPublicProduct()
    {
        var (productId, version, slug) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", 100);
        var key = upload.GetProperty("key").GetString()!;
        key.ShouldStartWith($"uploads/{productId}/");
        key.ShouldEndWith(".png");
        (await ProductImageApi.PutAsync(upload, "image/png", 100)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var attached = await ProductImageApi.AttachAsync(_admin, productId, key, version);

        attached.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await CatalogAdminApi.GetProductAsync(_admin, productId);
        var image = product.GetProperty("images").EnumerateArray().ShouldHaveSingleItem();
        image.GetProperty("key").GetString().ShouldBe($"products/{productId}/{ProductImageApi.FileNameOf(key)}");
        image.GetProperty("fileName").GetString().ShouldBe(ProductImageApi.FileNameOf(key));
        image.GetProperty("color").GetString().ShouldBe("azul");
        var page = await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/catalog/products/{slug}", TestContext.Current.CancellationToken);
        var url = page.GetProperty("imageUrls").EnumerateArray().ShouldHaveSingleItem().GetString();
        (await ProductImageApi.Storage.GetByteArrayAsync(url, TestContext.Current.CancellationToken)).Length.ShouldBe(100);
    }

    [Theory]
    [InlineData("image/gif", 100L)]
    [InlineData("image/png", 2_097_153L)]
    public async Task StartImageUpload_TypeOrSizeNotAllowed_Returns400(string contentType, long sizeInBytes)
    {
        var (productId, _, _) = await ProductImageApi.ProductWithSkuAsync(_admin);

        var response = await ProductImageApi.StartUploadResponseAsync(_admin, productId, contentType, sizeInBytes);

        (await CodeOfAsync(response, HttpStatusCode.BadRequest)).ShouldBe("validation.failed");
    }

    [Fact]
    public async Task StartImageUpload_UnknownProduct_Returns404()
    {
        var response = await ProductImageApi.StartUploadResponseAsync(_admin, Guid.NewGuid(), "image/png", 100);

        (await CodeOfAsync(response, HttpStatusCode.NotFound)).ShouldBe("catalog.product_not_found");
    }

    [Fact]
    public async Task AttachImage_NothingUploaded_Returns400()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", 100);

        var response = await ProductImageApi.AttachAsync(_admin, productId, upload.GetProperty("key").GetString()!, version);

        (await CodeOfAsync(response, HttpStatusCode.BadRequest)).ShouldBe("catalog.image_not_uploaded");
    }

    [Fact]
    public async Task AttachImage_StoredFileTooLarge_Returns400AndDeletesIt()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", 100);
        var key = upload.GetProperty("key").GetString()!;
        (await ProductImageApi.PutAsync(upload, "image/png", 2_097_153)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await ProductImageApi.AttachAsync(_admin, productId, key, version);

        (await CodeOfAsync(response, HttpStatusCode.BadRequest)).ShouldBe("catalog.invalid_image");
        (await CodeOfAsync(await ProductImageApi.AttachAsync(_admin, productId, key, version), HttpStatusCode.BadRequest)).ShouldBe("catalog.image_not_uploaded");
    }

    [Fact]
    public async Task AttachImage_KeyOfAnotherProduct_Returns400()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var (otherProductId, _, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var upload = await ProductImageApi.StartUploadAsync(_admin, otherProductId, "image/png", 100);

        var response = await ProductImageApi.AttachAsync(_admin, productId, upload.GetProperty("key").GetString()!, version);

        (await CodeOfAsync(response, HttpStatusCode.BadRequest)).ShouldBe("catalog.invalid_image_key");
    }

    [Fact]
    public async Task AttachImage_UploadUrlReusedAfterAttach_KeepsTheConfirmedImage()
    {
        var (productId, version, slug) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", 100);
        await ProductImageApi.PutAsync(upload, "image/png", 100);
        (await ProductImageApi.AttachAsync(_admin, productId, upload.GetProperty("key").GetString()!, version)).StatusCode.ShouldBe(HttpStatusCode.OK);

        await ProductImageApi.PutAsync(upload, "image/png", 2_097_153);

        var page = await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/catalog/products/{slug}", TestContext.Current.CancellationToken);
        var url = page.GetProperty("imageUrls").EnumerateArray().ShouldHaveSingleItem().GetString();
        (await ProductImageApi.Storage.GetByteArrayAsync(url, TestContext.Current.CancellationToken)).Length.ShouldBe(100);
    }

    [Fact]
    public async Task AttachImage_KeyOutsideTheUploadPrefix_Returns400()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", 100);
        await ProductImageApi.PutAsync(upload, "image/png", 100);

        var response = await ProductImageApi.AttachAsync(
            _admin,
            productId,
            $"products/{productId}/{ProductImageApi.FileNameOf(upload.GetProperty("key").GetString()!)}",
            version);

        (await CodeOfAsync(response, HttpStatusCode.BadRequest)).ShouldBe("catalog.invalid_image_key");
    }

    [Fact]
    public async Task CopyIfUnchanged_UploadReplacedAfterTheCheck_CopiesNothing()
    {
        var (productId, _, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/png", 100);
        await ProductImageApi.PutAsync(upload, "image/png", 100);
        var storage = images.Host.Services.GetRequiredService<ProductImageStorage>();
        var key = upload.GetProperty("key").GetString()!;
        var checkedUpload = (await storage.FindAsync(key, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        await ProductImageApi.PutAsync(upload, "image/png", 200);
        var target = $"products/{productId}/{ProductImageApi.FileNameOf(key)}";

        var isCopied = await storage.CopyIfUnchangedAsync(key, target, checkedUpload.ETag, TestContext.Current.CancellationToken);

        isCopied.ShouldBeFalse();
        (await storage.FindAsync(target, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task RemoveImage_AttachedImage_RemovesIt()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);
        var upload = await ProductImageApi.StartUploadAsync(_admin, productId, "image/webp", 10);
        var key = upload.GetProperty("key").GetString()!;
        await ProductImageApi.PutAsync(upload, "image/webp", 10);
        var attached = await CatalogAdminApi.ReadAsync(await ProductImageApi.AttachAsync(_admin, productId, key, version));
        var fileName = ProductImageApi.FileNameOf(key);

        var response = await _admin.DeleteAsync(
            $"{CatalogAdminApi.ProductsPath}/{productId}/images/{fileName}?version={attached.GetProperty("version").GetUInt32()}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await CatalogAdminApi.GetProductAsync(_admin, productId)).GetProperty("images").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task RemoveImage_UnknownFile_Returns404()
    {
        var (productId, version, _) = await ProductImageApi.ProductWithSkuAsync(_admin);

        var response = await _admin.DeleteAsync(
            $"{CatalogAdminApi.ProductsPath}/{productId}/images/missing.png?version={version}",
            TestContext.Current.CancellationToken);

        (await CodeOfAsync(response, HttpStatusCode.NotFound)).ShouldBe("catalog.image_not_found");
    }

    private static async Task<string?> CodeOfAsync(HttpResponseMessage response, HttpStatusCode status) =>
        (await ProblemResponse.ReadAsync(response, status)).GetProperty("code").GetString();
}
