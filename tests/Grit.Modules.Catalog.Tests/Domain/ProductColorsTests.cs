using Grit.Modules.Catalog.Domain;

namespace Grit.Modules.Catalog.Tests.Domain;

public class ProductColorsTests
{
    [Fact]
    public void Group_ColorsWithImages_FollowTheOrderOfTheirFirstImage()
    {
        var colors = ProductColors.Group(["azul", "preto", "verde"], [new ProductImage("k1", "preto"), new ProductImage("k2", "azul"), new ProductImage("k3", "preto")]);

        colors.Select(color => color.Name).ShouldBe(["preto", "azul", "verde"]);
        colors[0].ImageKeys.ShouldBe(["k1", "k3"]);
        colors[1].ImageKeys.ShouldBe(["k2"]);
        colors[2].ImageKeys.ShouldBeEmpty();
    }

    [Fact]
    public void Group_ColorsWithoutImages_ComeAfterByName()
    {
        var colors = ProductColors.Group(["verde", "azul", "amarelo"], [new ProductImage("k1", "verde")]);

        colors.Select(color => color.Name).ShouldBe(["verde", "amarelo", "azul"]);
    }

    [Fact]
    public void Group_SkuWithoutColor_IsLeftOut()
    {
        var colors = ProductColors.Group([null, "azul"], [new ProductImage("k1", null), new ProductImage("k2", "azul")]);

        colors.Select(color => color.Name).ShouldBe(["azul"]);
        colors[0].ImageKeys.ShouldBe(["k2"]);
    }

    [Fact]
    public void Group_RepeatedSkuColor_AppearsOnce()
    {
        var colors = ProductColors.Group(["azul", "azul"], []);

        colors.Select(color => color.Name).ShouldBe(["azul"]);
    }

    [Fact]
    public void Group_ImageOfAColorNoSkuHas_IsLeftOut()
    {
        var colors = ProductColors.Group(["azul"], [new ProductImage("k1", "preto")]);

        colors.Select(color => color.Name).ShouldBe(["azul"]);
        colors[0].ImageKeys.ShouldBeEmpty();
    }
}
