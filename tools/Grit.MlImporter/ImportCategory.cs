using System.Text.RegularExpressions;
using Grit.Modules.Catalog.Domain;

namespace Grit.MlImporter;

internal sealed partial record ImportCategory(string StoreName, string Query, string? Domain = null)
{
    // "Camping=barraca camping@MLB-TENTS": the store category, then the Mercado Livre search terms and an optional domain.
    public static ImportCategory? Parse(string value)
    {
        var parts = value.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[1].Length == 0 || parts[0].Length > 100 || Slug.FromName(parts[0]).IsFailure)
        {
            return null;
        }

        var atIndex = parts[1].LastIndexOf('@');
        if (atIndex < 0)
        {
            return new ImportCategory(parts[0], parts[1]);
        }

        var query = parts[1][..atIndex].Trim();
        var domain = parts[1][(atIndex + 1)..].Trim();
        return query.Length == 0 || !DomainPattern().IsMatch(domain) ? null : new ImportCategory(parts[0], query, domain);
    }

    [GeneratedRegex("^MLB-[A-Z0-9_]+$")]
    private static partial Regex DomainPattern();
}
