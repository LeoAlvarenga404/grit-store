using Grit.Modules.Catalog.Features;

namespace Grit.Modules.Catalog.Tests.Features;

public class TextRuleTests
{
    [Theory]
    [InlineData("Barraca Iglu")]
    [InlineData("")]
    public void HasNoNullCharacter_PlainText_ReturnsTrue(string text)
    {
        TextRule.HasNoNullCharacter(text).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Leve\u0000")]
    [InlineData("\u0000")]
    public void HasNoNullCharacter_TextWithNul_ReturnsFalse(string text)
    {
        TextRule.HasNoNullCharacter(text).ShouldBeFalse();
    }
}
