namespace Grit.MlImporter.Tests;

public class SeedMapperTests
{
    [Fact]
    public void LowestPriceInBrl_Listings_TakesTheLowestPositiveBrlPriceRoundedToCents()
    {
        MlItem[] items = [new(10m, "USD"), new(499.9m, "BRL"), new(459.905m, "BRL"), new(0m, "BRL"), new(null, "BRL")];

        SeedMapper.LowestPriceInBrl(items).ShouldBe(459.91m);
    }

    [Fact]
    public void LowestPriceInBrl_NoBrlListing_ReturnsNull()
    {
        SeedMapper.LowestPriceInBrl([new MlItem(89m, "USD")]).ShouldBeNull();
    }

    [Fact]
    public void SkuAttributes_ColorAndSize_BecomeCorInLowerCaseAndTamanho()
    {
        MlAttribute[] attributes = [new("COLOR", "Cor", "Azul Marinho"), new("SIZE", "Tamanho", "42"), new("BRAND", "Marca", "Serra Alta")];

        SeedMapper.SkuAttributes(attributes).ShouldBe(new Dictionary<string, string> { ["cor"] = "azul marinho", ["tamanho"] = "42" });
    }

    [Fact]
    public void ProductAttributes_ColorSizeGtinAndPackage_AreLeftOut()
    {
        MlAttribute[] attributes =
        [
            new("BRAND", "Marca", "Serra Alta"),
            new("COLOR", "Cor", "Azul Marinho"),
            new("SIZE", "Tamanho", "42"),
            new("SOLE_MATERIAL", "Material da sola", "Borracha"),
            new("GTIN", "Código universal de produto", "7898950123456"),
            new("PACKAGE_WEIGHT", "Peso da embalagem", "900 g"),
        ];

        SeedMapper.ProductAttributes(attributes).ShouldBe(new Dictionary<string, string> { ["marca"] = "Serra Alta", ["material-da-sola"] = "Borracha" });
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

    [Theory]
    [InlineData(79.905, "BRL", 79.91)]
    [InlineData(-1.0, "BRL", null)]
    [InlineData(79.9, "USD", null)]
    [InlineData(0.0, "BRL", null)]
    [InlineData(null, "BRL", null)]
    public void PriceOf_Winner_IsAPositiveBrlPriceRoundedToCents(double? price, string currency, double? expected)
    {
        SeedMapper.PriceOf(new MlBuyBoxWinner((decimal?)price, currency)).ShouldBe((decimal?)expected);
    }

    [Fact]
    public void PriceOf_NoWinner_ReturnsNull()
    {
        SeedMapper.PriceOf(null).ShouldBeNull();
    }

    [Theory]
    [InlineData("Sem gênero", "Unissex")]
    [InlineData("Masculino", "Masculino")]
    public void ProductAttributes_Gender_UsesTheStoreNames(string value, string expected)
    {
        SeedMapper.ProductAttributes([new MlAttribute("GENDER", "Gênero", value)]).ShouldBe(new Dictionary<string, string> { ["genero"] = expected });
    }
}
