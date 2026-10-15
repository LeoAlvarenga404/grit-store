namespace Grit.Modules.Catalog.Domain;

internal readonly record struct SkuId(Guid Value)
{
    public static SkuId New() => new(Guid.CreateVersion7());
}
