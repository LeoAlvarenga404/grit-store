using Dapper;
using Grit.Modules.Catalog.Contracts;
using Npgsql;

namespace Grit.Modules.Catalog.Infrastructure;

// Opens its own connection: callers use it from queries, or from commands that open no other connection (ADR 0005).
internal sealed class CatalogReader(NpgsqlDataSource dataSource) : ICatalogReader
{
    // Column order matches the SkuSnapshot constructor: Dapper binds record constructors by position and name.
    private const string SkusSql = """
        SELECT s.id AS "SkuId", p.name AS "ProductName", p.slug AS "ProductSlug",
               s.price_amount AS "PriceAmount", s.price_currency AS "PriceCurrency"
        FROM catalog.skus s
        JOIN catalog.products p ON p.id = s.product_id
        WHERE s.id = ANY(@SkuIds)
        """;

    private const string SkuIdsSql = "SELECT id FROM catalog.skus";

    public async Task<IReadOnlyList<SkuSnapshot>> GetSkusAsync(IReadOnlyCollection<Guid> skuIds, CancellationToken cancellationToken)
    {
        if (skuIds.Count == 0)
        {
            return [];
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(SkusSql, new { SkuIds = skuIds.ToArray() }, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<SkuSnapshot>(command)).AsList();
    }

    public async Task<IReadOnlyList<Guid>> GetSkuIdsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<Guid>(new CommandDefinition(SkuIdsSql, cancellationToken: cancellationToken))).AsList();
    }
}
