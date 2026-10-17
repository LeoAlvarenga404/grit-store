namespace Grit.Modules.Catalog.Features.ListProducts;

internal sealed record ProductListItemResponse(Guid Id, string Name, string Slug, int SkuCount);
