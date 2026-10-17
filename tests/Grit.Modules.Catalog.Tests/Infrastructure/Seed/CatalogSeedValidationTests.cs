using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.Modules.Catalog.Tests.Infrastructure.Seed;

public class CatalogSeedValidationTests
{
    [Fact]
    public void Validate_EightWebpImages_Passes()
    {
        var images = Enumerable.Range(1, 8).Select(index => $"mochila-{index}.webp").ToList();

        Should.NotThrow(() => CatalogSeeder.Validate(CatalogWith(images)));
    }

    [Fact]
    public void Validate_NoImages_Passes()
    {
        Should.NotThrow(() => CatalogSeeder.Validate(CatalogWith(null)));
    }

    [Theory]
    [InlineData("../mochila.webp")]
    [InlineData("images/mochila.webp")]
    [InlineData("mochila.jpg")]
    [InlineData(".webp")]
    public void Validate_ImageThatIsNotAWebpFileName_Throws(string image)
    {
        var exception = Should.Throw<InvalidOperationException>(() => CatalogSeeder.Validate(CatalogWith([image])));

        exception.Message.ShouldBe("The seed entry 'Mochila de Trilha' must list at most 8 .webp file names.");
    }

    [Fact]
    public void Validate_NineImages_Throws()
    {
        var images = Enumerable.Range(1, 9).Select(index => $"mochila-{index}.webp").ToList();

        Should.Throw<InvalidOperationException>(() => CatalogSeeder.Validate(CatalogWith(images)));
    }

    private static SeedCatalog CatalogWith(IReadOnlyList<string>? images) =>
        new([new SeedCategory("Camping", [
            new SeedProduct("Mochila de Trilha", "Leve.", new Dictionary<string, string>(), [new SeedSku(new Dictionary<string, string> { ["cor"] = "preto" }, 199.90m)], images),
        ])]);
}
