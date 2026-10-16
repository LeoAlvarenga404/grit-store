namespace Grit.Modules.Catalog.Features;

// PostgreSQL text and jsonb reject U+0000, and string.Normalize (slugs) throws on U+FFFE, a noncharacter that JSON bodies,
// routes and query strings still decode: without this rule either one surfaces as a 500 instead of a 400.
internal static class TextRule
{
    public const string Message = "Must not contain the characters U+0000 (NUL) or U+FFFE.";

    public static bool HasOnlySupportedCharacters(string? text) => text is null || text.AsSpan().IndexOfAny('\0', '\uFFFE') < 0;
}
