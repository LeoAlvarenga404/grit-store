namespace Grit.Modules.Catalog.Domain;

internal readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.CreateVersion7());
}
