namespace Grit.Modules.Catalog.Domain;

// Color is the "cor" value of the SKUs the image shows, or null for a product whose SKUs have no colour.
internal sealed record ProductImage(string Key, string? Color);
