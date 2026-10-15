using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;

namespace Grit.Modules.Catalog.Tests.Domain;

public class ProductTests
{
    private static readonly CategoryId Camping = CategoryId.New();

    [Fact]
    public void Create_ValidInput_SetsFieldsAndSlugWithoutSkus()
    {
        var result = Product.Create(Camping, "Barraca Iglu 2 Pessoas", "Leve.", Attributes(("capacidade", "2")));

        result.IsSuccess.ShouldBeTrue();
        var product = result.Value;
        product.Id.Value.ShouldNotBe(Guid.Empty);
        product.CategoryId.ShouldBe(Camping);
        product.Name.ShouldBe("Barraca Iglu 2 Pessoas");
        product.Slug.Value.ShouldBe("barraca-iglu-2-pessoas");
        product.Description.ShouldBe("Leve.");
        product.Attributes["capacidade"].ShouldBe("2");
        product.Skus.ShouldBeEmpty();
    }

    [Fact]
    public void Create_NameWithoutLetterOrDigit_ReturnsEmptySlugError()
    {
        var result = Product.Create(Camping, "!!!", "", Attributes());

        result.Error.ShouldBe(CatalogErrors.EmptySlug);
    }

    [Fact]
    public void Create_CallerChangesAttributesAfterwards_ProductKeepsItsCopy()
    {
        var attributes = Attributes(("capacidade", "2"));
        var product = Product.Create(Camping, "Barraca", "", attributes).Value;

        attributes["capacidade"] = "4";

        product.Attributes["capacidade"].ShouldBe("2");
    }

    [Fact]
    public void Update_NewValues_ChangesFieldsAndKeepsSlug()
    {
        var product = Product.Create(Camping, "Barraca", "", Attributes()).Value;
        var trekking = CategoryId.New();

        product.Update(trekking, "Barraca Ultraleve", "Nova.", Attributes(("peso", "1.2 kg")));

        product.CategoryId.ShouldBe(trekking);
        product.Name.ShouldBe("Barraca Ultraleve");
        product.Description.ShouldBe("Nova.");
        product.Attributes["peso"].ShouldBe("1.2 kg");
        product.Slug.Value.ShouldBe("barraca");
    }

    [Fact]
    public void AddSku_NewVariant_AddsSkuWithAttributesAndPrice()
    {
        var product = NewProduct();

        var result = product.AddSku(Attributes(("cor", "azul"), ("tamanho", "M")), Reais(99.90m));

        result.IsSuccess.ShouldBeTrue();
        product.Skus.ShouldHaveSingleItem().ShouldBe(result.Value);
        result.Value.Id.Value.ShouldNotBe(Guid.Empty);
        result.Value.Attributes["cor"].ShouldBe("azul");
        result.Value.Price.ShouldBe(Reais(99.90m));
    }

    [Fact]
    public void AddSku_SameAttributesInAnotherOrder_ReturnsDuplicateVariant()
    {
        var product = NewProduct();
        product.AddSku(Attributes(("cor", "azul"), ("tamanho", "M")), Reais(99.90m));

        var result = product.AddSku(Attributes(("tamanho", "M"), ("cor", "azul")), Reais(89.90m));

        result.Error.ShouldBe(CatalogErrors.DuplicateVariant);
        product.Skus.Count.ShouldBe(1);
    }

    [Fact]
    public void AddSku_DifferentValue_AddsSecondSku()
    {
        var product = NewProduct();
        product.AddSku(Attributes(("cor", "azul"), ("tamanho", "M")), Reais(99.90m));

        var result = product.AddSku(Attributes(("cor", "azul"), ("tamanho", "G")), Reais(99.90m));

        result.IsSuccess.ShouldBeTrue();
        product.Skus.Count.ShouldBe(2);
    }

    [Fact]
    public void AddSku_SupersetOfExistingAttributes_AddsSecondSku()
    {
        var product = NewProduct();
        product.AddSku(Attributes(("cor", "azul")), Reais(99.90m));

        var result = product.AddSku(Attributes(("cor", "azul"), ("tamanho", "M")), Reais(99.90m));

        result.IsSuccess.ShouldBeTrue();
        product.Skus.Count.ShouldBe(2);
    }

    [Fact]
    public void AddSku_PriceNotInReais_ReturnsUnsupportedCurrency()
    {
        var product = NewProduct();

        var result = product.AddSku(Attributes(("cor", "azul")), Money.Create(10m, "USD").Value);

        result.Error.ShouldBe(CatalogErrors.UnsupportedCurrency);
        product.Skus.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeSkuPrice_ExistingSku_ChangesOnlyThatPrice()
    {
        var product = NewProduct();
        var blue = product.AddSku(Attributes(("cor", "azul")), Reais(99.90m)).Value;
        var green = product.AddSku(Attributes(("cor", "verde")), Reais(99.90m)).Value;

        var result = product.ChangeSkuPrice(blue.Id, Reais(79.90m));

        result.IsSuccess.ShouldBeTrue();
        blue.Price.ShouldBe(Reais(79.90m));
        green.Price.ShouldBe(Reais(99.90m));
    }

    [Fact]
    public void ChangeSkuPrice_UnknownSku_ReturnsSkuNotFound()
    {
        var product = NewProduct();

        var result = product.ChangeSkuPrice(SkuId.New(), Reais(79.90m));

        result.Error.ShouldBe(CatalogErrors.SkuNotFound);
    }

    [Fact]
    public void ChangeSkuPrice_PriceNotInReais_ReturnsUnsupportedCurrencyAndKeepsPrice()
    {
        var product = NewProduct();
        var sku = product.AddSku(Attributes(("cor", "azul")), Reais(99.90m)).Value;

        var result = product.ChangeSkuPrice(sku.Id, Money.Create(10m, "USD").Value);

        result.Error.ShouldBe(CatalogErrors.UnsupportedCurrency);
        sku.Price.ShouldBe(Reais(99.90m));
    }

    private static Product NewProduct() => Product.Create(Camping, "Camiseta Dry Fit", "", Attributes()).Value;

    private static Money Reais(decimal amount) => Money.Create(amount, "BRL").Value;

    private static Dictionary<string, string> Attributes(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
}
