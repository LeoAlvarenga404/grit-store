namespace Grit.Modules.Catalog.Features;

// PostgreSQL text and jsonb reject U+0000: without this rule it surfaces as a 500 on save instead of a 400.
internal static class TextRule
{
    public const string Message = "Must not contain the NUL character (U+0000).";

    public static bool HasNoNullCharacter(string? text) => text is null || !text.Contains('\0');
}
