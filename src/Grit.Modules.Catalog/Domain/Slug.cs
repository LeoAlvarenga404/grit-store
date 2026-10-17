using System.Globalization;
using System.Text;
using Grit.SharedKernel;

namespace Grit.Modules.Catalog.Domain;

internal sealed record Slug
{
    public const int MaxLength = 100;

    private Slug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // Idempotent: a stored slug comes back unchanged, which is how EF restores it from its column.
    public static Result<Slug> FromName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var slug = new StringBuilder(name.Length);
        foreach (var character in name.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                slug.Append(character);
                continue;
            }

            var isAccent = CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark;
            if (!isAccent && slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        var value = slug.ToString(0, Math.Min(slug.Length, MaxLength)).TrimEnd('-');
        if (value.Length == 0)
        {
            return CatalogErrors.EmptySlug;
        }

        return new Slug(value);
    }
}
