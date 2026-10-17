using Grit.Modules.Catalog.Domain;

namespace Grit.MlImporter;

internal sealed record ImportCategory(string StoreName, string Query)
{
    // "Camping=barraca camping": the store category, then the Mercado Livre search terms.
    public static ImportCategory? Parse(string value)
    {
        var parts = value.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[1].Length == 0 || parts[0].Length > 100 || Slug.FromName(parts[0]).IsFailure)
        {
            return null;
        }

        return new ImportCategory(parts[0], parts[1]);
    }
}
