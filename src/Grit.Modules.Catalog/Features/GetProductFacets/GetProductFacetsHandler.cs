using System.Globalization;
using System.Text.Json;
using Dapper;
using Grit.SharedKernel;
using MediatR;
using Npgsql;

namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal sealed class GetProductFacetsHandler(NpgsqlDataSource dataSource)
    : IRequestHandler<GetProductFacetsQuery, Result<GetProductFacetsResponse>>
{
    private const string CategoryKind = "category";

    private const string PriceKind = "price";

    private const string AttributeKind = "attribute";

    // Every count is the number of products the search returns for that refinement, so a click never changes the
    // number shown. Counting matching SKUs per product means a product with SKUs in two price ranges counts in both.
    private const string Sql = $"""
        WITH {ProductFilterSql.Input},
        matched AS (
            SELECT p.id, p.category_id, s.price_amount, p.attributes || s.attributes AS attributes
            FROM catalog.products p
            JOIN catalog.skus s ON s.product_id = p.id
            CROSS JOIN input
            WHERE {ProductFilterSql.Where}
        )
        SELECT '{CategoryKind}' AS kind, c.slug AS key, c.name AS value, count(DISTINCT m.id) AS count
        FROM matched m
        JOIN catalog.categories c ON c.id = m.category_id
        GROUP BY c.slug, c.name
        UNION ALL
        SELECT '{PriceKind}', width_bucket(m.price_amount, @PriceBounds)::text, NULL, count(DISTINCT m.id)
        FROM matched m
        GROUP BY width_bucket(m.price_amount, @PriceBounds)
        UNION ALL
        SELECT '{AttributeKind}', a.key, a.value, count(DISTINCT m.id)
        FROM matched m
        CROSS JOIN LATERAL jsonb_each_text(m.attributes) AS a
        GROUP BY a.key, a.value
        """;

    // Ranges are [lower, upper): width_bucket answers 0 below the first bound and PriceBounds.Length from the last bound up.
    private static readonly decimal[] PriceBounds = [100m, 250m, 500m, 1000m];

    public async Task<Result<GetProductFacetsResponse>> Handle(GetProductFacetsQuery query, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            query.Query,
            query.Category,
            query.MinPrice,
            query.MaxPrice,
            Attributes = JsonSerializer.Serialize(AttributeFilter.Parse(query.Attributes)),
            PriceBounds,
        };

        await using var connection = dataSource.CreateConnection();
        var rows = (await connection.QueryAsync<FacetRow>(
            new CommandDefinition(Sql, parameters, cancellationToken: cancellationToken))).AsList();

        return new GetProductFacetsResponse(CategoriesOf(rows), PricesOf(rows), AttributesOf(rows));
    }

    private static List<CategoryFacetResponse> CategoriesOf(List<FacetRow> rows) =>
        rows.Where(row => row.Kind == CategoryKind)
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.Value, StringComparer.Ordinal)
            .Select(row => new CategoryFacetResponse(row.Key, row.Value!, row.Count))
            .ToList();

    private static List<PriceFacetResponse> PricesOf(List<FacetRow> rows) =>
        rows.Where(row => row.Kind == PriceKind)
            .Select(row => (Bucket: int.Parse(row.Key, CultureInfo.InvariantCulture), row.Count))
            .OrderBy(price => price.Bucket)
            .Select(price => new PriceFacetResponse(
                price.Bucket == 0 ? 0m : PriceBounds[price.Bucket - 1],
                price.Bucket == PriceBounds.Length ? null : PriceBounds[price.Bucket],
                price.Count))
            .ToList();

    private static List<AttributeFacetResponse> AttributesOf(List<FacetRow> rows) =>
        rows.Where(row => row.Kind == AttributeKind)
            .GroupBy(row => row.Key)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new AttributeFacetResponse(
                group.Key,
                group.OrderByDescending(row => row.Count)
                    .ThenBy(row => row.Value, StringComparer.Ordinal)
                    .Select(row => new AttributeValueFacetResponse(row.Value!, row.Count))
                    .ToList()))
            .ToList();
}
