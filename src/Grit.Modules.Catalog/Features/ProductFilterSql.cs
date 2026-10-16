namespace Grit.Modules.Catalog.Features;

// One filter for the search and the facets. A product is listed when it matches the text and the category and
// has at least one SKU that, with the product's attributes, holds every requested attribute and fits the price range.
// The text matches the stemmed, unaccented words (plurals, accents) or, for typos, a name word within pg_trgm's
// strict word similarity threshold (0.5).
internal static class ProductFilterSql
{
    public const string Input = """
        input AS (
            SELECT plainto_tsquery('catalog.portuguese_unaccent', @Query) AS query,
                catalog.immutable_unaccent(@Query) AS text
        )
        """;

    public const string Where = """
        (@Query IS NULL OR p.search_vector @@ input.query OR input.text <<% catalog.immutable_unaccent(p.name))
            AND (@Category IS NULL OR p.category_id = (SELECT c.id FROM catalog.categories c WHERE c.slug = @Category))
            AND (@MinPrice IS NULL OR s.price_amount >= @MinPrice)
            AND (@MaxPrice IS NULL OR s.price_amount <= @MaxPrice)
            AND (p.attributes || s.attributes) @> CAST(@Attributes AS jsonb)
        """;
}
