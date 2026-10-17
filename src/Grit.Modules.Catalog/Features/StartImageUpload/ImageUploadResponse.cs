namespace Grit.Modules.Catalog.Features.StartImageUpload;

internal sealed record ImageUploadResponse(string Key, string UploadUrl, DateTimeOffset ExpiresAt);
