using System.Text.Json;
using Dapper;
using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;
using MediatR;
using Npgsql;

namespace Grit.Modules.Catalog.Features.SearchProducts;

internal sealed class SearchProductsHandler(NpgsqlDataSource dataSource)
    : IRequestHandler<SearchProductsQuery, Result<SearchProductsResponse>>
{
    // Without text every rank is 0, so the order falls back to the id: UUIDv7, newest first.
    private const string Sql = $"""
        WITH {ProductFilterSql.Input},
        matched AS (
            SELECT p.id, p.slug, p.name, s.price_amount,
                CASE WHEN @Query IS NULL THEN 0
                    ELSE round((ts_rank(p.search_vector, input.query)
                        + strict_word_similarity(input.text, catalog.immutable_unaccent(p.name)))::numeric, 6)
                END AS rank
            FROM catalog.products p
            JOIN catalog.skus s ON s.product_id = p.id
            CROSS JOIN input
            WHERE {ProductFilterSql.Where}
        )
        SELECT id, slug, name, min(price_amount) AS priceAmount, rank
        FROM matched
        GROUP BY id, slug, name, rank
        HAVING @AfterId IS NULL OR (rank, id) < (@AfterRank, @AfterId)
        ORDER BY rank DESC, id DESC
        LIMIT @Limit
        """;

    public async Task<Result<SearchProductsResponse>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        var after = ProductCursor.TryParse(query.After, out var cursor) ? cursor : null;
        var parameters = new
        {
            query.Query,
            query.Category,
            query.MinPrice,
            query.MaxPrice,
            Attributes = JsonSerializer.Serialize(AttributeFilter.Parse(query.Attributes)),
            AfterRank = after?.Rank,
            AfterId = after?.Id,
            Limit = query.Limit + 1,
        };

        await using var connection = dataSource.CreateConnection();
        var rows = (await connection.QueryAsync<SearchProductsRow>(
            new CommandDefinition(Sql, parameters, cancellationToken: cancellationToken))).AsList();

        var items = rows
            .Take(query.Limit)
            .Select(row => new ProductSummaryResponse(row.Id, row.Slug, row.Name, row.PriceAmount, Product.StoreCurrency))
            .ToList();
        var nextCursor = rows.Count > query.Limit ? new ProductCursor(rows[query.Limit - 1].Rank, rows[query.Limit - 1].Id).Encode() : null;
        return new SearchProductsResponse(items, nextCursor);
    }
}
