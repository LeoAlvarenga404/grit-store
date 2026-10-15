namespace Grit.Modules.Catalog.Features.CreateProduct;

internal sealed record CreateProductRequest(
    Guid CategoryId,
    string Name,
    string Description,
    Dictionary<string, string>? Attributes);
