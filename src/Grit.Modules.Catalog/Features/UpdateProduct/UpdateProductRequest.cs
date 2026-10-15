namespace Grit.Modules.Catalog.Features.UpdateProduct;

internal sealed record UpdateProductRequest(
    Guid CategoryId,
    string Name,
    string Description,
    Dictionary<string, string>? Attributes,
    uint Version);
