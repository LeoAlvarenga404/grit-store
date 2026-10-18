using Dapper;
using Grit.Modules.Catalog.Contracts;
using Grit.Modules.Catalog.Domain;
using Npgsql;

namespace Grit.Modules.Catalog.Infrastructure;

// Opens its own connection: callers use it from queries, or from commands that open no other connection (ADR 0005).
internal sealed class CatalogReader(NpgsqlDataSource dataSource, ProductImageStorage storage) : ICatalogReader
{
    // The image is the first one of the SKU's colour; IS NOT DISTINCT FROM also matches a SKU without colour to the images without colour.
    private const string SkusSql = $"""
        SELECT s.id AS "SkuId", p.name AS "ProductName", p.slug AS "ProductSlug",
               s.price_amount AS "PriceAmount", s.price_currency AS "PriceCurrency",
               s.attributes ->> '{Sku.ColorAttribute}' AS "Color", s.attributes ->> '{Sku.SizeAttribute}' AS "Size",
               (SELECT e.image ->> 'key'
                FROM jsonb_array_elements(p.images) WITH ORDINALITY AS e(image, position)
                WHERE e.image ->> 'color' IS NOT DISTINCT FROM s.attributes ->> '{Sku.ColorAttribute}'
                ORDER BY e.position
                LIMIT 1) AS "ImageKey"
        FROM catalog.skus s
        JOIN catalog.products p ON p.id = s.product_id
        WHERE s.id = ANY(@SkuIds)
        """;

    private const string SkuIdsSql = "SELECT id FROM catalog.skus";

    // Signed on every read, like the catalog pages (ADR 0016); basket and checkout reads are never cached.
    public async Task<IReadOnlyList<SkuSnapshot>> GetSkusAsync(IReadOnlyCollection<Guid> skuIds, CancellationToken cancellationToken)
    {
        if (skuIds.Count == 0)
        {
            return [];
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(SkusSql, new { SkuIds = skuIds.ToArray() }, cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<SkuSnapshotRow>(command)).AsList();
        var snapshots = new List<SkuSnapshot>(rows.Count);
        foreach (var row in rows)
        {
            var imageUrl = row.ImageKey is null ? null : await storage.DownloadUrlAsync(row.ImageKey);
            snapshots.Add(new SkuSnapshot(row.SkuId, row.ProductName, row.ProductSlug, row.PriceAmount, row.PriceCurrency, row.Color, row.Size, imageUrl));
        }

        return snapshots;
    }

    public async Task<IReadOnlyList<Guid>> GetSkuIdsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<Guid>(new CommandDefinition(SkuIdsSql, cancellationToken: cancellationToken))).AsList();
    }
}
