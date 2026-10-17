namespace Grit.Modules.Catalog.Features.AttachImage;

// Color defaults to null so it is optional in the contract (ADR 0011): only a product whose SKUs have no colour leaves it out.
internal sealed record AttachImageRequest(string Key, uint Version, string? Color = null);
