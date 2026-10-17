using Grit.Modules.Catalog.Domain;

namespace Grit.Modules.Catalog.Tests.Domain;

public class ProductImageTests
{
    [Fact]
    public void AddImage_KeysOfThisProduct_KeepsTheUploadOrder()
    {
        var product = NewProduct();

        product.AddImage(KeyOf(product, "a.png")).IsSuccess.ShouldBeTrue();
        product.AddImage(KeyOf(product, "b.webp")).IsSuccess.ShouldBeTrue();

        product.ImageKeys.ShouldBe([KeyOf(product, "a.png"), KeyOf(product, "b.webp")]);
    }

    [Fact]
    public void AddImage_KeyOfAnotherProduct_ReturnsInvalidImageKey()
    {
        var product = NewProduct();

        product.AddImage(KeyOf(NewProduct(), "a.png")).Error.ShouldBe(CatalogErrors.InvalidImageKey);

        product.ImageKeys.ShouldBeEmpty();
    }

    [Fact]
    public void AddImage_PrefixWithoutFileName_ReturnsInvalidImageKey()
    {
        var product = NewProduct();

        product.AddImage(Product.ImageKeyPrefix(product.Id)).Error.ShouldBe(CatalogErrors.InvalidImageKey);
    }

    [Fact]
    public void AddImage_SameKeyTwice_KeepsOneEntry()
    {
        var product = NewProduct();
        product.AddImage(KeyOf(product, "a.png"));

        product.AddImage(KeyOf(product, "a.png")).IsSuccess.ShouldBeTrue();

        product.ImageKeys.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddImage_PastTheLimit_ReturnsImageLimit()
    {
        var product = NewProduct();
        for (var index = 0; index < Product.MaxImages; index++)
        {
            product.AddImage(KeyOf(product, $"{index}.png")).IsSuccess.ShouldBeTrue();
        }

        product.AddImage(KeyOf(product, "extra.png")).Error.ShouldBe(CatalogErrors.ImageLimit);

        product.ImageKeys.Count.ShouldBe(8);
    }

    [Fact]
    public void RemoveImage_AttachedKey_RemovesItAndKeepsTheOrder()
    {
        var product = NewProduct();
        product.AddImage(KeyOf(product, "a.png"));
        product.AddImage(KeyOf(product, "b.png"));
        product.AddImage(KeyOf(product, "c.png"));

        product.RemoveImage(KeyOf(product, "b.png")).IsSuccess.ShouldBeTrue();

        product.ImageKeys.ShouldBe([KeyOf(product, "a.png"), KeyOf(product, "c.png")]);
    }

    [Fact]
    public void RemoveImage_UnknownKey_ReturnsImageNotFound()
    {
        var product = NewProduct();

        product.RemoveImage(KeyOf(product, "a.png")).Error.ShouldBe(CatalogErrors.ImageNotFound);
    }

    private static Product NewProduct() => Product.Create(CategoryId.New(), "Mochila de Ataque 30 Litros", "", new Dictionary<string, string>()).Value;

    private static string KeyOf(Product product, string fileName) => $"{Product.ImageKeyPrefix(product.Id)}{fileName}";
}
