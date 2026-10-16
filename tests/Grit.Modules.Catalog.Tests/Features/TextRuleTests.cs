using Grit.Modules.Catalog.Features;

namespace Grit.Modules.Catalog.Tests.Features;

public class TextRuleTests
{
    [Theory]
    [InlineData("Barraca Iglu")]
    [InlineData("")]
    [InlineData("Ação \uFFFF")]
    public void HasOnlySupportedCharacters_PlainText_ReturnsTrue(string text)
    {
        TextRule.HasOnlySupportedCharacters(text).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Leve\u0000")]
    [InlineData("\u0000")]
    [InlineData("Trilha\uFFFE")]
    public void HasOnlySupportedCharacters_UnsupportedCharacter_ReturnsFalse(string text)
    {
        TextRule.HasOnlySupportedCharacters(text).ShouldBeFalse();
    }
}
