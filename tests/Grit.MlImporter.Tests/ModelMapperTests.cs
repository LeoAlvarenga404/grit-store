using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.MlImporter.Tests;

public class ModelMapperTests
{
    [Fact]
    public void Map_TwoColorsAndTwoSizes_KeepsOnlyTheObservedVariants()
    {
        PricedLeaf[] leaves = [Leaf("Preto", "40", 100m), Leaf("Preto", "41", 100m), Leaf("Azul", "40", 120m)];

        var skus = ModelMapper.Map(leaves, []).Value.Skus;

        skus.Select(sku => (sku.Attributes["cor"], sku.Attributes["tamanho"], sku.PriceAmount))
            .ShouldBe([("preto", "40", 100m), ("preto", "41", 100m), ("azul", "40", 120m)]);
    }

    [Fact]
    public void Map_SameVariantTwice_KeepsOneSkuAtTheLowerPrice()
    {
        PricedLeaf[] leaves = [Leaf("Preto", "40", 110m), Leaf("Azul", "40", 90m), Leaf("Preto", "40", 100m)];

        var skus = ModelMapper.Map(leaves, []).Value.Skus;

        skus.Select(sku => (sku.Attributes["cor"], sku.PriceAmount)).ShouldBe([("preto", 100m), ("azul", 90m)]);
    }

    [Fact]
    public void Map_OneColor_FailsWithTooFewColors()
    {
        ModelMapper.Map([Leaf("Preto", "40"), Leaf("Preto", "41")], []).Error.ShouldBe(ModelMapper.TooFewColors);
    }

    [Fact]
    public void Map_LeafWithoutColor_IsLeftOut()
    {
        PricedLeaf[] leaves = [Leaf(null, "40"), Leaf("Preto", "40"), Leaf("Azul", "41")];

        var skus = ModelMapper.Map(leaves, []).Value.Skus;

        skus.Select(sku => sku.Attributes["cor"]).ShouldBe(["preto", "azul"]);
    }

    [Fact]
    public void Map_OneColorAndALeafWithoutColor_FailsWithTooFewColors()
    {
        ModelMapper.Map([Leaf(null, "40"), Leaf("Preto", "41")], []).Error.ShouldBe(ModelMapper.TooFewColors);
    }

    [Fact]
    public void Map_FamilyName_NamesTheProduct()
    {
        PricedLeaf[] leaves = [Leaf("Preto", "40", name: "Tênis Veloz Preto 40", familyName: "Tênis Veloz"), Leaf("Azul", "40")];

        ModelMapper.Map(leaves, []).Value.Name.ShouldBe("Tênis Veloz");
    }

    [Fact]
    public void Map_BlankFamilyName_UsesTheFirstLeafName()
    {
        PricedLeaf[] leaves = [Leaf("Preto", "40", name: "Tênis Veloz Preto 40", familyName: " "), Leaf("Azul", "40")];

        ModelMapper.Map(leaves, []).Value.Name.ShouldBe("Tênis Veloz Preto 40");
    }

    [Fact]
    public void Map_NameWithoutLetterOrDigit_FailsWithNoName()
    {
        PricedLeaf[] leaves = [Leaf("Preto", "40", familyName: "--- ***"), Leaf("Azul", "40")];

        ModelMapper.Map(leaves, []).Error.ShouldBe(SeedMapper.NoName);
    }

    [Fact]
    public void Map_Attributes_KeepOnlyWhatEveryLeafShares()
    {
        PricedLeaf[] leaves =
        [
            Leaf("Preto", "40", extra: [new("BRAND", "Marca", "Grit"), new("MAIN_COLOR", "Cor principal", "Preto"), new("GENDER", "Gênero", "Masculino")]),
            Leaf("Azul", "40", extra: [new("BRAND", "Marca", "Grit"), new("MAIN_COLOR", "Cor principal", "Azul"), new("GENDER", "Gênero", "Masculino")]),
            Leaf("Azul", "41", extra: [new("BRAND", "Marca", "Grit"), new("GENDER", "Gênero", "Masculino")]),
        ];

        ModelMapper.Map(leaves, []).Value.Attributes.ShouldBe(new Dictionary<string, string> { ["marca"] = "Grit", ["genero"] = "Masculino" });
    }

    [Fact]
    public void Map_Description_IsTheFirstOneThatIsNotEmpty()
    {
        PricedLeaf[] leaves = [Leaf("Preto", "40", description: " "), Leaf("Azul", "40", description: "Leve e respirável.")];

        ModelMapper.Map(leaves, []).Value.Description.ShouldBe("Leve e respirável.");
    }

    [Fact]
    public void Map_NoDescription_UsesTheName()
    {
        PricedLeaf[] leaves = [Leaf("Preto", "40", familyName: "Tênis Veloz"), Leaf("Azul", "40")];

        ModelMapper.Map(leaves, []).Value.Description.ShouldBe("Tênis Veloz");
    }

    [Fact]
    public void Map_LongNameAndDescription_AreCutToTheApiLimits()
    {
        PricedLeaf[] leaves = [Leaf("Preto", "40", familyName: new string('a', 250), description: new string('b', 5000)), Leaf("Azul", "40")];

        var mapped = ModelMapper.Map(leaves, []).Value;

        mapped.Name.Length.ShouldBe(SeedMapper.MaxNameLength);
        mapped.Description.Length.ShouldBe(SeedMapper.MaxDescriptionLength);
    }

    [Fact]
    public void Map_Images_AreKeptAsGiven()
    {
        SeedColorImages[] images = [new("preto", ["veloz-1-1.webp"]), new("azul", ["veloz-2-1.webp"])];

        var mapped = ModelMapper.Map([Leaf("Preto", "40"), Leaf("Azul", "40")], images).Value;

        mapped.Images.ShouldBeSameAs(images);
    }

    [Fact]
    public void ColorsOf_Leaves_ListsEachColorOnceInTheOrderSeen()
    {
        ModelMapper.ColorsOf([Leaf("Azul", "40"), Leaf(null, "40"), Leaf("Preto", "40"), Leaf("Azul", "41")]).ShouldBe(["azul", "preto"]);
    }

    [Fact]
    public void PicturesByColor_RepeatedPicture_IsListedOncePerColorUpToTheLimit()
    {
        PricedLeaf[] leaves =
        [
            Leaf("Preto", "40", pictures: [new("https://p/a.jpg"), new("https://p/b.jpg")]),
            Leaf("Azul", "40", pictures: [new("https://p/f.jpg")]),
            Leaf("Preto", "41", pictures: [new("https://p/b.jpg"), new("https://p/c.jpg"), new("https://p/d.jpg")]),
        ];

        var pictures = ModelMapper.PicturesByColor(leaves, maxPerColor: 3);

        pictures.Select(color => color.Color).ShouldBe(["preto", "azul"]);
        pictures[0].Urls.ShouldBe(["https://p/a.jpg", "https://p/b.jpg", "https://p/c.jpg"]);
        pictures[1].Urls.ShouldBe(["https://p/f.jpg"]);
    }

    private static PricedLeaf Leaf(
        string? color,
        string size,
        decimal price = 99.90m,
        string name = "Tênis Veloz",
        string? familyName = null,
        string? description = null,
        MlAttribute[]? extra = null,
        MlPicture[]? pictures = null)
    {
        var attributes = new List<MlAttribute>(extra ?? []) { new("SIZE", "Tamanho", size) };
        if (color is not null)
        {
            attributes.Add(new MlAttribute("COLOR", "Cor", color));
        }

        var product = new MlProduct(
            $"MLB{Guid.NewGuid():N}",
            name,
            attributes,
            pictures ?? [],
            description is null ? null : new MlShortDescription(description),
            FamilyName: familyName);
        return new PricedLeaf(product, price);
    }
}
