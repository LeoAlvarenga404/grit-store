namespace Grit.Modules.Catalog.Features;

// Checked twice: before signing an upload (the declared type and size) and on confirm (what the storage really holds).
internal static class ImageRule
{
    public const long MaxSizeInBytes = 2 * 1024 * 1024;

    public const string Message = "Images must be JPEG, PNG or WebP files of at most 2 MB.";

    public static readonly IReadOnlyDictionary<string, string> ExtensionByContentType = new Dictionary<string, string>
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    public static bool IsAllowed(string? contentType, long sizeInBytes) =>
        contentType is not null && ExtensionByContentType.ContainsKey(contentType) && sizeInBytes is > 0 and <= MaxSizeInBytes;
}
