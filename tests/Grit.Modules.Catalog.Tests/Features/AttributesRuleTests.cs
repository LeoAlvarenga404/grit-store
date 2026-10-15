using Grit.Modules.Catalog.Features;

namespace Grit.Modules.Catalog.Tests.Features;

public class AttributesRuleTests
{
    [Fact]
    public void IsValid_LowercaseKeysWithDigitsAndHyphens_ReturnsTrue()
    {
        var attributes = new Dictionary<string, string> { ["cor"] = "Azul Royal", ["tamanho-pe2"] = "42" };

        AttributesRule.IsValid(attributes).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_TwentyAttributesWithLongestKeyAndValue_ReturnsTrue()
    {
        var attributes = Enumerable.Range(1, 18).ToDictionary(index => $"chave{index}", _ => "valor");
        attributes[new string('k', 50)] = "valor";
        attributes["descricao"] = new string('v', 100);

        AttributesRule.IsValid(attributes).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Cor", "azul")]
    [InlineData("co r", "azul")]
    [InlineData("", "azul")]
    [InlineData("cor", "")]
    [InlineData("cor", "azul\u0000")]
    public void IsValid_BadKeyOrValue_ReturnsFalse(string key, string value)
    {
        AttributesRule.IsValid(new Dictionary<string, string> { [key] = value }).ShouldBeFalse();
    }

    [Theory]
    [InlineData(51, 1)]
    [InlineData(1, 101)]
    public void IsValid_KeyOrValueTooLong_ReturnsFalse(int keyLength, int valueLength)
    {
        var attributes = new Dictionary<string, string> { [new string('k', keyLength)] = new string('v', valueLength) };

        AttributesRule.IsValid(attributes).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_NullValue_ReturnsFalse()
    {
        var attributes = new Dictionary<string, string> { ["cor"] = null! };

        AttributesRule.IsValid(attributes).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_MoreThanTwentyAttributes_ReturnsFalse()
    {
        var attributes = Enumerable.Range(1, 21).ToDictionary(index => $"chave{index}", _ => "valor");

        AttributesRule.IsValid(attributes).ShouldBeFalse();
    }
}
