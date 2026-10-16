using Grit.Modules.Catalog.Features;

namespace Grit.Modules.Catalog.Tests.Features;

public class AttributeFilterTests
{
    [Fact]
    public void Parse_Pairs_ReturnsOneEntryPerKey()
    {
        var attributes = AttributeFilter.Parse(["cor:azul", "tamanho:42"]);

        attributes.ShouldBe(new Dictionary<string, string> { ["cor"] = "azul", ["tamanho"] = "42" });
    }

    [Fact]
    public void Parse_ValueWithColon_SplitsAtTheFirstColon()
    {
        var attributes = AttributeFilter.Parse(["hora:10:30"]);

        attributes.ShouldBe(new Dictionary<string, string> { ["hora"] = "10:30" });
    }

    [Fact]
    public void Parse_NoPairs_ReturnsEmpty()
    {
        AttributeFilter.Parse([]).ShouldBeEmpty();
    }

    [Fact]
    public void Parse_PairWithoutColon_ReturnsNull()
    {
        AttributeFilter.Parse(["cor:azul", "tamanho"]).ShouldBeNull();
    }

    [Fact]
    public void Parse_RepeatedKey_ReturnsNull()
    {
        AttributeFilter.Parse(["cor:azul", "cor:preto"]).ShouldBeNull();
    }

    [Fact]
    public void IsValid_TwentyPairs_ReturnsTrue()
    {
        var pairs = Enumerable.Range(1, 20).Select(index => $"chave{index}:valor").ToList();

        AttributeFilter.IsValid(pairs).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_TwentyOnePairs_ReturnsFalse()
    {
        var pairs = Enumerable.Range(1, 21).Select(index => $"chave{index}:valor").ToList();

        AttributeFilter.IsValid(pairs).ShouldBeFalse();
    }

    [Theory]
    [InlineData("cor")]
    [InlineData(":azul")]
    [InlineData("cor:")]
    [InlineData("Cor:azul")]
    [InlineData("cor:azul\u0000")]
    public void IsValid_BadPair_ReturnsFalse(string pair)
    {
        AttributeFilter.IsValid([pair]).ShouldBeFalse();
    }
}
