using Grit.Modules.Catalog.Domain;

namespace Grit.Modules.Catalog.Tests.Domain;

public class SlugTests
{
    [Theory]
    [InlineData("Tênis Café", "tenis-cafe")]
    [InlineData("Camping & Trilha", "camping-trilha")]
    [InlineData("  Bike  Aro 29!! ", "bike-aro-29")]
    [InlineData("Ação--Rápida", "acao-rapida")]
    [InlineData("GARRAFA 750ML", "garrafa-750ml")]
    public void FromName_Name_ReturnsLowercaseAsciiWordsJoinedByHyphens(string name, string expected)
    {
        var result = Slug.FromName(name);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!")]
    [InlineData(" - ")]
    public void FromName_NoLetterOrDigit_ReturnsEmptySlugError(string name)
    {
        var result = Slug.FromName(name);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(CatalogErrors.EmptySlug);
    }

    [Fact]
    public void FromName_LongName_CutsAtMaxLengthWithoutTrailingHyphen()
    {
        var result = Slug.FromName(new string('a', 99) + " bbbb");

        result.Value.Value.ShouldBe(new string('a', 99));
    }

    [Fact]
    public void FromName_ExistingSlug_ReturnsItUnchanged()
    {
        var result = Slug.FromName("tenis-cafe-2026");

        result.Value.Value.ShouldBe("tenis-cafe-2026");
    }
}
