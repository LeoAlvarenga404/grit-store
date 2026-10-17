using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;

namespace Grit.Modules.Catalog.Tests.Domain;

public class ProductImageTests
{
    // One SKU per colour (null = a SKU without "cor"), each with its own size so no two SKUs repeat a variant.
    private static Product ProductWithColors(params string?[] colors)
    {
        var product = Product.Create(CategoryId.New(), "Camiseta Dry Fit", "", new Dictionary<string, string>()).Value;
        for (var index = 0; index < colors.Length; index++)
        {
            var attributes = new Dictionary<string, string> { ["tamanho"] = $"{index + 1}" };
            if (colors[index] is { } color)
            {
                attributes["cor"] = color;
            }

            product.AddSku(attributes, Money.Create(99.90m, "BRL").Value).IsSuccess.ShouldBeTrue();
        }

        return product;
    }

    private static string KeyOf(Product product, string fileName) => $"{Product.ImageKeyPrefix(product.Id)}{fileName}";

    [Fact]
    public void AddImage_KeysOfThisProduct_KeepsTheUploadOrderAndColors()
    {
        var product = ProductWithColors("azul", "preto");

        product.AddImage(KeyOf(product, "a.png"), "azul").IsSuccess.ShouldBeTrue();
        product.AddImage(KeyOf(product, "b.webp"), "preto").IsSuccess.ShouldBeTrue();

        product.Images.ShouldBe([new ProductImage(KeyOf(product, "a.png"), "azul"), new ProductImage(KeyOf(product, "b.webp"), "preto")]);
    }

    [Fact]
    public void AddImage_KeyOfAnotherProduct_ReturnsInvalidImageKey()
    {
        var product = ProductWithColors("azul");

        product.AddImage(KeyOf(ProductWithColors("azul"), "a.png"), "azul").Error.ShouldBe(CatalogErrors.InvalidImageKey);

        product.Images.ShouldBeEmpty();
    }

    [Fact]
    public void AddImage_PrefixWithoutFileName_ReturnsInvalidImageKey()
    {
        var product = ProductWithColors("azul");

        product.AddImage(Product.ImageKeyPrefix(product.Id), "azul").Error.ShouldBe(CatalogErrors.InvalidImageKey);
    }

    [Fact]
    public void AddImage_SameKeyTwice_ReturnsDuplicateImage()
    {
        var product = ProductWithColors("azul", "preto");
        product.AddImage(KeyOf(product, "a.png"), "azul").IsSuccess.ShouldBeTrue();

        product.AddImage(KeyOf(product, "a.png"), "preto").Error.ShouldBe(CatalogErrors.DuplicateImage);

        product.Images.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddImage_ColorNoSkuHas_ReturnsImageColorNotFound()
    {
        var product = ProductWithColors("azul");

        product.AddImage(KeyOf(product, "a.png"), "verde").Error.ShouldBe(CatalogErrors.ImageColorNotFound);

        product.Images.ShouldBeEmpty();
    }

    [Fact]
    public void AddImage_NoColorWhenEverySkuHasOne_ReturnsImageColorNotFound()
    {
        var product = ProductWithColors("azul");

        product.AddImage(KeyOf(product, "a.png"), null).Error.ShouldBe(CatalogErrors.ImageColorNotFound);
    }

    [Fact]
    public void AddImage_NoColorWhenASkuHasNone_AddsIt()
    {
        var product = ProductWithColors([null]);

        product.AddImage(KeyOf(product, "a.png"), null).IsSuccess.ShouldBeTrue();

        product.Images.ShouldBe([new ProductImage(KeyOf(product, "a.png"), null)]);
    }

    [Fact]
    public void AddImage_ProductWithoutSkus_ReturnsImageColorNotFound()
    {
        var product = ProductWithColors();

        product.AddImage(KeyOf(product, "a.png"), "azul").Error.ShouldBe(CatalogErrors.ImageColorNotFound);
    }

    [Fact]
    public void AddImage_ColorInAnotherCase_ReturnsImageColorNotFound()
    {
        var product = ProductWithColors("azul");

        product.AddImage(KeyOf(product, "a.png"), "Azul").Error.ShouldBe(CatalogErrors.ImageColorNotFound);
    }

    [Fact]
    public void AddImage_PastTheLimitOfOneColor_ReturnsImageLimit()
    {
        var product = ProductWithColors("azul");
        for (var index = 0; index < Product.MaxImagesPerColor; index++)
        {
            product.AddImage(KeyOf(product, $"{index}.png"), "azul").IsSuccess.ShouldBeTrue();
        }

        product.AddImage(KeyOf(product, "extra.png"), "azul").Error.ShouldBe(CatalogErrors.ImageLimit);

        product.Images.Count.ShouldBe(8);
    }

    [Fact]
    public void AddImage_EightImagesOfAnotherColor_StillAddsThisOne()
    {
        var product = ProductWithColors("azul", "preto");
        for (var index = 0; index < Product.MaxImagesPerColor; index++)
        {
            product.AddImage(KeyOf(product, $"{index}.png"), "azul").IsSuccess.ShouldBeTrue();
        }

        product.AddImage(KeyOf(product, "extra.png"), "preto").IsSuccess.ShouldBeTrue();

        product.Images.Count.ShouldBe(9);
    }

    [Fact]
    public void RemoveImage_AttachedKey_RemovesItAndKeepsTheOrder()
    {
        var product = ProductWithColors("azul");
        product.AddImage(KeyOf(product, "a.png"), "azul");
        product.AddImage(KeyOf(product, "b.png"), "azul");
        product.AddImage(KeyOf(product, "c.png"), "azul");

        product.RemoveImage(KeyOf(product, "b.png")).IsSuccess.ShouldBeTrue();

        product.Images.Select(image => image.Key).ShouldBe([KeyOf(product, "a.png"), KeyOf(product, "c.png")]);
    }

    [Fact]
    public void RemoveImage_UnknownKey_ReturnsImageNotFound()
    {
        var product = ProductWithColors("azul");

        product.RemoveImage(KeyOf(product, "a.png")).Error.ShouldBe(CatalogErrors.ImageNotFound);
    }
}
