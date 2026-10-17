using System.Text.Json;
using Dapper;
using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Npgsql;

namespace Grit.Modules.Catalog.Features.SearchProducts;

internal sealed class SearchProductsHandler(NpgsqlDataSource dataSource, ProductImageStorage storage, CatalogCache cache)
    : IRequestHandler<SearchProductsQuery, Result<SearchProductsResponse>>
{
    // Without text every rank is 0, so the order falls back to the id: UUIDv7, newest first.
    private const string Sql = $"""
        WITH {ProductFilterSql.Input},
        matched AS (
            SELECT p.id, p.slug, p.name, p.image_keys[1] AS image_key, s.price_amount,
                CASE WHEN @Query IS NULL THEN 0
                    ELSE round((ts_rank(p.search_vector, input.query)
                        + strict_word_similarity(input.text, catalog.immutable_unaccent(p.name)))::numeric, 6)
                END AS rank
            FROM catalog.products p
            JOIN catalog.skus s ON s.product_id = p.id
            CROSS JOIN input
            WHERE {ProductFilterSql.Where}
        )
        SELECT id, slug, name, min(price_amount) AS priceAmount, rank, image_key AS imageKey
        FROM matched
        GROUP BY id, slug, name, rank, image_key
        HAVING @AfterId IS NULL OR (rank, id) < (@AfterRank, @AfterId)
        ORDER BY rank DESC, id DESC
        LIMIT @Limit
        """;

    public async Task<Result<SearchProductsResponse>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        var page = await cache.GetOrCreateAsync(CatalogCache.KeyOf("search-page", query), token => SearchAsync(query, token), cancellationToken);
        return await SignAsync(page);
    }

    private async ValueTask<SearchProductsPage> SearchAsync(SearchProductsQuery query, CancellationToken cancellationToken)
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

        var taken = rows.Take(query.Limit).ToList();
        var items = taken
            .Select(row => new ProductSummaryResponse(row.Id, row.Slug, row.Name, row.PriceAmount, Product.StoreCurrency, ImageUrl: null))
            .ToList();
        var imageKeys = taken.Select(row => row.ImageKey).ToList();
        var nextCursor = rows.Count > query.Limit ? new ProductCursor(rows[query.Limit - 1].Rank, rows[query.Limit - 1].Id).Encode() : null;
        return new SearchProductsPage(new SearchProductsResponse(items, nextCursor), imageKeys);
    }

    private async Task<SearchProductsResponse> SignAsync(SearchProductsPage page)
    {
        var items = new List<ProductSummaryResponse>(page.Response.Items.Count);
        for (var index = 0; index < page.Response.Items.Count; index++)
        {
            var item = page.Response.Items[index];
            var imageKey = page.ImageKeys[index];
            items.Add(imageKey is null ? item : item with { ImageUrl = await storage.DownloadUrlAsync(imageKey) });
        }

        return page.Response with { Items = items };
    }
}
