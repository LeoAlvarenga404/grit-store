namespace Grit.Modules.Catalog.Features.GetProductBySlug;

internal sealed record GetProductBySlugRow(GetProductBySlugResponse Product, IReadOnlyList<string> ImageKeys);
