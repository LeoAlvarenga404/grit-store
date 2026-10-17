using Grit.Modules.Catalog.Domain;

namespace Grit.Modules.Catalog.Tests.Domain;

public class CategoryTests
{
    [Fact]
    public void Create_ValidName_SetsNameAndSlug()
    {
        var result = Category.Create("Camping & Trilha");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Camping & Trilha");
        result.Value.Slug.Value.ShouldBe("camping-trilha");
        result.Value.Id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_NameWithoutLetterOrDigit_ReturnsEmptySlugError()
    {
        var result = Category.Create("???");

        result.Error.ShouldBe(CatalogErrors.EmptySlug);
    }
}
