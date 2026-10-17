namespace Grit.MlImporter.Tests;

public class SeedMapperTests
{
    private static readonly MlItem[] OneListing = [new MlItem(199.90m, "BRL")];

    [Fact]
    public void Map_ProductWithListings_TakesTheLowestBrlPriceRoundedToCents()
    {
        MlItem[] items = [new(10m, "USD"), new(499.9m, "BRL"), new(459.905m, "BRL"), new(0m, "BRL"), new(null, "BRL")];

        var mapped = SeedMapper.Map(Product("Barraca Iglu"), items, []);

        mapped.Value.Skus.ShouldHaveSingleItem().PriceAmount.ShouldBe(459.91m);
    }

    [Fact]
    public void Map_NoBrlListing_FailsWithNoPrice()
    {
        SeedMapper.Map(Product("Barraca Iglu"), [new MlItem(89m, "USD")], []).Error.ShouldBe(SeedMapper.NoPrice);
    }

    [Fact]
    public void Map_NameWithoutLetterOrDigit_FailsWithNoName()
    {
        SeedMapper.Map(Product("--- ***"), OneListing, []).Error.ShouldBe(SeedMapper.NoName);
    }

    [Fact]
    public void Map_ColorAndSize_BecomeTheSkuVariantAndLeaveTheProductAttributes()
    {
        var product = Product("Bota Serra", attributes:
        [
            new("BRAND", "Marca", "Serra Alta"),
            new("COLOR", "Cor", "Azul Marinho"),
            new("SIZE", "Tamanho", "42"),
            new("SOLE_MATERIAL", "Material da sola", "Borracha"),
            new("GTIN", "Código universal de produto", "7898950123456"),
            new("PACKAGE_WEIGHT", "Peso da embalagem", "900 g"),
        ]);

        var mapped = SeedMapper.Map(product, OneListing, ["bota-serra-1.webp"]).Value;

        mapped.Skus.ShouldHaveSingleItem().Attributes.ShouldBe(new Dictionary<string, string> { ["cor"] = "azul marinho", ["tamanho"] = "42" });
        mapped.Attributes.ShouldBe(new Dictionary<string, string> { ["marca"] = "Serra Alta", ["material-da-sola"] = "Borracha" });
        mapped.Images.ShouldBe(["bota-serra-1.webp"]);
    }

    [Fact]
    public void Map_NoShortDescription_UsesTheName()
    {
        SeedMapper.Map(Product("Barraca Iglu"), OneListing, []).Value.Description.ShouldBe("Barraca Iglu");
    }

    [Fact]
    public void Map_LongNameAndDescription_AreCutToTheApiLimits()
    {
        var product = Product(new string('a', 250), new string('b', 5000));

        var mapped = SeedMapper.Map(product, OneListing, []).Value;

        mapped.Name.Length.ShouldBe(SeedMapper.MaxNameLength);
        mapped.Description.Length.ShouldBe(SeedMapper.MaxDescriptionLength);
    }

    [Fact]
    public void ProductAttributes_MoreThanTwenty_KeepsTheFirstTwenty()
    {
        var attributes = Enumerable.Range(1, 25).Select(index => new MlAttribute($"ATTRIBUTE_{index}", $"Atributo {index}", "sim")).ToList();

        var mapped = SeedMapper.ProductAttributes(attributes);

        mapped.Count.ShouldBe(SeedMapper.MaxAttributes);
        mapped.ShouldContainKey("atributo-20");
        mapped.ShouldNotContainKey("atributo-21");
    }

    [Fact]
    public void ProductAttributes_EmptyValueKeyWithoutLettersOrRepeatedKey_AreSkipped()
    {
        MlAttribute[] attributes = [new("A", "Peso", null), new("B", "Altura", "  "), new("C", "***", "x"), new("D", "Marca", "Grit"), new("E", "marca", "Outra")];

        SeedMapper.ProductAttributes(attributes).ShouldBe(new Dictionary<string, string> { ["marca"] = "Grit" });
    }

    [Fact]
    public void AttributeKey_LongName_IsCutTo50CharactersWithoutATrailingHyphen()
    {
        var key = SeedMapper.AttributeKey("Capacidade " + new string('x', 38) + " de carga máxima");

        key.ShouldBe("capacidade-" + new string('x', 38));
    }

    [Fact]
    public void Clean_NulAndNoncharacter_AreRemovedAndTheTextTrimmed()
    {
        SeedMapper.Clean("  Barraca\0 Iglu￾  ", 100).ShouldBe("Barraca Iglu");
    }

    [Fact]
    public void Clean_CutInsideASurrogatePair_DropsTheHalfCharacter()
    {
        SeedMapper.Clean("abc\U0001F3D5", 4).ShouldBe("abc");
    }

    private static MlProduct Product(string name, string? description = null, IReadOnlyList<MlAttribute>? attributes = null) =>
        new("MLB1", name, attributes ?? [], [], description is null ? null : new MlShortDescription(description));
}
