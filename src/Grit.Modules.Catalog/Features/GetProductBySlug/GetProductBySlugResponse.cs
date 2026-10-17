namespace Grit.Modules.Catalog.Features.GetProductBySlug;

internal sealed record GetProductBySlugResponse(
    Guid Id,
    string Slug,
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Attributes,
    string CategorySlug,
    string CategoryName,
    IReadOnlyList<GetProductBySlugSkuResponse> Skus);
