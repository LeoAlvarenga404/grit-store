namespace Grit.Modules.Catalog.Features.UpdateProduct;

internal sealed record UpdateProductRequest(
    Guid CategoryId,
    string Name,
    string Description,
    uint Version,
    Dictionary<string, string>? Attributes = null);
