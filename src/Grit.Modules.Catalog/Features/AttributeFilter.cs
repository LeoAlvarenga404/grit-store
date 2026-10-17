namespace Grit.Modules.Catalog.Features;

internal static class AttributeFilter
{
    public const string Message =
        "Use key:value pairs, one per key: at most 20, keys of up to 50 lowercase letters, digits or '-', values of 1 to 100 characters without U+0000 or U+FFFE.";

    // Null when a pair has no ':' or repeats a key; the value keeps any later ':' ("hora:10:30").
    public static Dictionary<string, string>? Parse(IReadOnlyList<string> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        var attributes = new Dictionary<string, string>(pairs.Count);
        foreach (var pair in pairs)
        {
            var separator = pair.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0 || !attributes.TryAdd(pair[..separator], pair[(separator + 1)..]))
            {
                return null;
            }
        }

        return attributes;
    }

    public static bool IsValid(IReadOnlyList<string> pairs) => Parse(pairs) is { } attributes && AttributesRule.IsValid(attributes);
}
