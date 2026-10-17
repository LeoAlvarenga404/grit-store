namespace Grit.Modules.Catalog.Domain;

internal readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.CreateVersion7());
}
