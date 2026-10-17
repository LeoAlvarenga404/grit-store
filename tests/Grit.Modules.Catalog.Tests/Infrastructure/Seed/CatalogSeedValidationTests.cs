using System.Text.Json;
using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.Modules.Catalog.Tests.Infrastructure.Seed;

public class CatalogSeedValidationTests
{
    private const string Name = "Mochila de Trilha";

    [Fact]
    public void Validate_EightWebpImagesOfOneColor_Passes()
    {
        Should.NotThrow(() => CatalogSeeder.Validate(CatalogWith([new("preto", Files("mochila", 8))], "preto")));
    }

    [Fact]
    public void Validate_NoImages_Passes()
    {
        Should.NotThrow(() => CatalogSeeder.Validate(CatalogWith(null, "preto")));
    }

    [Theory]
    [InlineData("../mochila.webp")]
    [InlineData("images/mochila.webp")]
    [InlineData("mochila.jpg")]
    [InlineData(".webp")]
    public void Validate_ImageThatIsNotAWebpFileName_Throws(string image)
    {
        var exception = Should.Throw<InvalidOperationException>(() => CatalogSeeder.Validate(CatalogWith([new("preto", [image])], "preto")));

        exception.Message.ShouldBe("The seed entry 'Mochila de Trilha' must list .webp file names only.");
    }

    [Fact]
    public void Validate_NineImagesOfOneColor_Throws()
    {
        var exception = Should.Throw<InvalidOperationException>(() => CatalogSeeder.Validate(CatalogWith([new("preto", Files("mochila", 9))], "preto")));

        exception.Message.ShouldBe("The seed entry 'Mochila de Trilha' is invalid: catalog.image_limit.");
    }

    [Fact]
    public void Validate_EightImagesInEachOfTwoColors_Passes()
    {
        var images = new List<SeedColorImages> { new("preto", Files("preta", 8)), new("azul", Files("azul", 8)) };

        Should.NotThrow(() => CatalogSeeder.Validate(CatalogWith(images, "preto", "azul")));
    }

    [Fact]
    public void Validate_ColorNoSkuHas_Throws()
    {
        var exception = Should.Throw<InvalidOperationException>(() => CatalogSeeder.Validate(CatalogWith([new("verde", Files("mochila", 1))], "preto")));

        exception.Message.ShouldBe("The seed entry 'Mochila de Trilha' is invalid: catalog.image_color_not_found.");
    }

    [Fact]
    public void Validate_SameFileInTwoColors_Throws()
    {
        var images = new List<SeedColorImages> { new("preto", ["mochila.webp"]), new("azul", ["mochila.webp"]) };

        var exception = Should.Throw<InvalidOperationException>(() => CatalogSeeder.Validate(CatalogWith(images, "preto", "azul")));

        exception.Message.ShouldBe("The seed entry 'Mochila de Trilha' is invalid: catalog.duplicate_image.");
    }

    [Fact]
    public void Validate_ImagesWithoutColorForASkuWithoutColor_Passes()
    {
        Should.NotThrow(() => CatalogSeeder.Validate(CatalogWith([new(null, Files("mochila", 2))], (string?)null)));
    }

    [Fact]
    public void Validate_GroupWithoutFiles_Throws()
    {
        var exception = Should.Throw<InvalidOperationException>(() => CatalogSeeder.Validate(CatalogWith([new("preto", null!)], "preto")));

        exception.Message.ShouldBe("The seed entry 'Mochila de Trilha' must list .webp file names only.");
    }

    [Fact]
    public void Validate_CommittedSeed_Passes()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "seed", "catalog.json"));
        var catalog = JsonSerializer.Deserialize<SeedCatalog>(File.ReadAllText(path), JsonSerializerOptions.Web)!;

        Should.NotThrow(() => CatalogSeeder.Validate(catalog));
    }

    // One SKU per colour (null = a SKU without "cor"), each with its own size.
    private static SeedCatalog CatalogWith(IReadOnlyList<SeedColorImages>? images, params string?[] skuColors) =>
        new([new SeedCategory("Camping", [
            new SeedProduct(Name, "Leve.", new Dictionary<string, string>(), [.. skuColors.Select(Sku)], images),
        ])]);

    private static SeedSku Sku(string? color, int index)
    {
        var attributes = new Dictionary<string, string> { ["tamanho"] = $"{index + 1}" };
        if (color is not null)
        {
            attributes["cor"] = color;
        }

        return new SeedSku(attributes, 199.90m);
    }

    private static List<string> Files(string prefix, int count) => [.. Enumerable.Range(1, count).Select(index => $"{prefix}-{index}.webp")];
}
