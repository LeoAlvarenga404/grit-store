using Grit.Modules.Ordering.Domain;
using Grit.SharedKernel;

namespace Grit.Modules.Ordering.Tests.Domain;

public class OrderLineTests
{
    [Fact]
    public void Create_KeepsTheSlugColorAndSizeBought()
    {
        var line = OrderLine.Create(Guid.NewGuid(), "Camiseta Dry Fit", "camiseta-dry-fit", "azul", "M", 2, Money.Create(99.90m, "BRL").Value);

        line.ProductSlug.ShouldBe("camiseta-dry-fit");
        line.Color.ShouldBe("azul");
        line.Size.ShouldBe("M");
    }

    [Fact]
    public void Create_SkuWithoutColorOrSize_KeepsThemNull()
    {
        var line = OrderLine.Create(Guid.NewGuid(), "Garrafa", "garrafa", null, null, 1, Money.Create(39.90m, "BRL").Value);

        line.Color.ShouldBeNull();
        line.Size.ShouldBeNull();
    }
}
