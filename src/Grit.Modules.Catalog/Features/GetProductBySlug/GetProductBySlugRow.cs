using Grit.Modules.Catalog.Domain;

namespace Grit.Modules.Catalog.Features.GetProductBySlug;

internal sealed record GetProductBySlugRow(GetProductBySlugResponse Product, IReadOnlyList<ProductImage> Images);
