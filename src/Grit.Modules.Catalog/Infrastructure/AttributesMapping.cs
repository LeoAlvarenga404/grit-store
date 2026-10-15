using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Catalog.Infrastructure;

internal static class AttributesMapping
{
    // A converter to text keeps jsonb working without Npgsql's dynamic JSON switch on the host's shared data source.
    public static PropertyBuilder<IReadOnlyDictionary<string, string>> IsJsonAttributes(
        this PropertyBuilder<IReadOnlyDictionary<string, string>> property) =>
        property
            .HasColumnType("jsonb")
            .HasConversion(
                attributes => JsonSerializer.Serialize(attributes, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)!,
                new ValueComparer<IReadOnlyDictionary<string, string>>(
                    (left, right) => left!.Count == right!.Count && !left.Except(right).Any(),
                    attributes => attributes.Aggregate(0, (hash, pair) => hash ^ HashCode.Combine(pair.Key, pair.Value)),
                    attributes => new Dictionary<string, string>(attributes)));
}
