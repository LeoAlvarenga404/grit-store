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
    private static readonly string RelevanceSql = SqlOrderedBy("rank", "<", "DESC");

    private static readonly string PriceAscendingSql = SqlOrderedBy("price", ">", "ASC");

    private static readonly string PriceDescendingSql = SqlOrderedBy("price", "<", "DESC");

    public async Task<Result<SearchProductsResponse>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        // v2: the cached page changed shape in 13a; an entry left by the previous build in Redis must not be read back.
        var page = await cache.GetOrCreateAsync(CatalogCache.KeyOf("search-page-v2", query), token => SearchAsync(query, token), cancellationToken);
        return await SignAsync(page);
    }

    // Keyset on (sort value, id): the rank, or the price of the cheapest matching SKU. Without text every rank is 0, so the
    // relevance order falls back to the id: UUIDv7, newest first. The colours come from every SKU of the product, read for
    // the page's rows only. The column and the comparison are constants, never request text.
    private static string SqlOrderedBy(string column, string comparison, string direction) => $"""
        WITH {ProductFilterSql.Input},
        matched AS (
            SELECT p.id, s.price_amount,
                CASE WHEN @Query IS NULL THEN 0
                    ELSE round((ts_rank(p.search_vector, input.query)
                        + strict_word_similarity(input.text, catalog.immutable_unaccent(p.name)))::numeric, 6)
                END AS rank
            FROM catalog.products p
            JOIN catalog.skus s ON s.product_id = p.id
            CROSS JOIN input
            WHERE {ProductFilterSql.Where}
        ),
        listed AS (
            SELECT id, rank, min(price_amount) AS price, max(price_amount) AS highest_price
            FROM matched
            GROUP BY id, rank
        ),
        page AS (
            SELECT id, price, highest_price, {column} AS sort_value
            FROM listed
            WHERE @AfterId IS NULL OR ({column}, id) {comparison} (@AfterValue, @AfterId)
            ORDER BY {column} {direction}, id {direction}
            LIMIT @Limit
        )
        SELECT page.id, p.slug, p.name, page.price AS priceAmount, page.price <> page.highest_price AS hasPriceRange,
            page.sort_value AS sortValue, p.images::text AS images,
            coalesce((SELECT jsonb_agg(DISTINCT s.attributes ->> '{Sku.ColorAttribute}')
                FROM catalog.skus s
                WHERE s.product_id = page.id AND s.attributes ->> '{Sku.ColorAttribute}' IS NOT NULL), '[]')::text AS colors
        FROM page
        JOIN catalog.products p ON p.id = page.id
        ORDER BY page.sort_value {direction}, page.id {direction}
        """;

    private static string SqlFor(string sort) => sort switch
    {
        ProductSort.PriceAscending => PriceAscendingSql,
        ProductSort.PriceDescending => PriceDescendingSql,
        _ => RelevanceSql,
    };

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
            AfterValue = after?.SortValue,
            AfterId = after?.Id,
            Limit = query.Limit + 1,
        };

        await using var connection = dataSource.CreateConnection();
        var rows = (await connection.QueryAsync<SearchProductsRow>(
            new CommandDefinition(SqlFor(query.Sort), parameters, cancellationToken: cancellationToken))).AsList();

        var taken = rows.Take(query.Limit).Select(Summarize).ToList();
        var nextCursor = rows.Count > query.Limit ? new ProductCursor(rows[query.Limit - 1].SortValue, rows[query.Limit - 1].Id).Encode() : null;
        return new SearchProductsPage(
            new SearchProductsResponse([.. taken.Select(summary => summary.Item)], nextCursor),
            [.. taken.Select(summary => summary.ImageKeys)]);
    }

    private static (ProductSummaryResponse Item, SearchItemImageKeys ImageKeys) Summarize(SearchProductsRow row)
    {
        var images = JsonSerializer.Deserialize<List<ProductImage>>(row.Images, JsonSerializerOptions.Web)!;
        var colors = ProductColors.Group(JsonSerializer.Deserialize<List<string>>(row.Colors)!, images);
        var item = new ProductSummaryResponse(
            row.Id,
            row.Slug,
            row.Name,
            row.PriceAmount,
            Product.StoreCurrency,
            row.HasPriceRange,
            ImageUrl: null,
            [.. colors.Select(color => new ProductSummaryColorResponse(color.Name, ImageUrl: null))]);
        var imageKeys = new SearchItemImageKeys(
            images is [var cover, ..] ? cover.Key : null,
            [.. colors.Select(color => color.ImageKeys is [var first, ..] ? first : null)]);
        return (item, imageKeys);
    }

    private async Task<SearchProductsResponse> SignAsync(SearchProductsPage page)
    {
        var items = new List<ProductSummaryResponse>(page.Response.Items.Count);
        for (var index = 0; index < page.Response.Items.Count; index++)
        {
            var item = page.Response.Items[index];
            var keys = page.ImageKeys[index];
            var colors = new List<ProductSummaryColorResponse>(item.Colors.Count);
            for (var colorIndex = 0; colorIndex < item.Colors.Count; colorIndex++)
            {
                colors.Add(item.Colors[colorIndex] with { ImageUrl = await UrlOfAsync(keys.Colors[colorIndex]) });
            }

            items.Add(item with { ImageUrl = await UrlOfAsync(keys.Cover), Colors = colors });
        }

        return page.Response with { Items = items };
    }

    private async Task<string?> UrlOfAsync(string? key) => key is null ? null : await storage.DownloadUrlAsync(key);
}
