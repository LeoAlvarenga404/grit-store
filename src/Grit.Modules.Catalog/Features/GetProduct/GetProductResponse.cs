namespace Grit.Modules.Catalog.Features.GetProduct;

internal sealed record GetProductResponse(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Slug,
    string Description,
    IReadOnlyDictionary<string, string> Attributes,
    uint Version,
    IReadOnlyList<GetProductSkuResponse> Skus);
