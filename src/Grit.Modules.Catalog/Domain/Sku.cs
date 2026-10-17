using Grit.SharedKernel;

namespace Grit.Modules.Catalog.Domain;

internal sealed class Sku : Entity<SkuId>
{
    // EF cannot bind a complex property such as Price to a constructor parameter; it sets Price right after.
    private Sku(SkuId id, IReadOnlyDictionary<string, string> attributes) : base(id)
    {
        Attributes = attributes;
        Price = null!;
    }

    public const string ColorAttribute = "cor";

    public const string SizeAttribute = "tamanho";

    public IReadOnlyDictionary<string, string> Attributes { get; }

    public string? Color => Attributes.GetValueOrDefault(ColorAttribute);

    public Money Price { get; private set; }

    public static Sku Create(IReadOnlyDictionary<string, string> attributes, Money price) =>
        new(SkuId.New(), new Dictionary<string, string>(attributes)) { Price = price };

    public void ChangePrice(Money price) => Price = price;
}
