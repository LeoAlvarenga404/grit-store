using Dapper;
using Grit.SharedKernel;
using MediatR;
using Npgsql;

namespace Grit.Modules.Catalog.Features.GetProductSuggestions;

internal sealed class GetProductSuggestionsHandler(NpgsqlDataSource dataSource)
    : IRequestHandler<GetProductSuggestionsQuery, Result<IReadOnlyList<ProductSuggestionResponse>>>
{
    private const int Limit = 8;

    // <% (word similarity) matches a prefix of any name word; <<% (strict word similarity) matches a whole word
    // with a typo. Names that start with the text come first.
    private const string Sql = """
        WITH input AS (SELECT catalog.immutable_unaccent(@Query) AS text)
        SELECT p.slug, p.name
        FROM catalog.products p
        CROSS JOIN input
        WHERE (input.text <% catalog.immutable_unaccent(p.name) OR input.text <<% catalog.immutable_unaccent(p.name))
            AND EXISTS (SELECT 1 FROM catalog.skus s WHERE s.product_id = p.id)
        ORDER BY starts_with(lower(catalog.immutable_unaccent(p.name)), lower(input.text)) DESC,
            word_similarity(input.text, catalog.immutable_unaccent(p.name)) DESC,
            p.name,
            p.id
        LIMIT @Limit
        """;

    public async Task<Result<IReadOnlyList<ProductSuggestionResponse>>> Handle(
        GetProductSuggestionsQuery query,
        CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();
        var suggestions = await connection.QueryAsync<ProductSuggestionResponse>(
            new CommandDefinition(Sql, new { query.Query, Limit }, cancellationToken: cancellationToken));

        return suggestions.AsList();
    }
}
