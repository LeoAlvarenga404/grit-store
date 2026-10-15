namespace Grit.Modules.Catalog.Features;

internal static class AttributesRule
{
    public const string Message =
        "Use at most 20 attributes, keys of up to 50 lowercase letters, digits or '-', and values of 1 to 100 characters.";

    private const int MaxCount = 20;

    public static bool IsValid(IReadOnlyDictionary<string, string> attributes) =>
        attributes.Count <= MaxCount
        && attributes.All(pair => IsValidKey(pair.Key) && pair.Value is { Length: > 0 and <= 100 });

    private static bool IsValidKey(string key) =>
        key.Length is > 0 and <= 50
        && key.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
}
