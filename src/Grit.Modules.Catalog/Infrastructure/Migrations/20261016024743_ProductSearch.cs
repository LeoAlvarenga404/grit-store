using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grit.Modules.Catalog.Infrastructure.Migrations
{
    // Search lives only in SQL: EF never maps search_vector, and the Dapper queries read it directly.
    public partial class ProductSearch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS unaccent SCHEMA public;
                CREATE EXTENSION IF NOT EXISTS pg_trgm SCHEMA public;

                CREATE TEXT SEARCH CONFIGURATION catalog.portuguese_unaccent (COPY = pg_catalog.portuguese);
                ALTER TEXT SEARCH CONFIGURATION catalog.portuguese_unaccent
                    ALTER MAPPING FOR hword, hword_part, word WITH public.unaccent, pg_catalog.portuguese_stem;

                CREATE FUNCTION catalog.immutable_unaccent(text) RETURNS text
                    LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
                    RETURN public.unaccent('public.unaccent'::regdictionary, $1);

                ALTER TABLE catalog.products ADD COLUMN search_vector tsvector GENERATED ALWAYS AS (
                    setweight(to_tsvector('catalog.portuguese_unaccent'::regconfig, name), 'A')
                    || setweight(to_tsvector('catalog.portuguese_unaccent'::regconfig, description), 'C')) STORED;

                CREATE INDEX ix_products_search_vector ON catalog.products USING gin (search_vector);
                CREATE INDEX ix_products_name_trigram ON catalog.products USING gin (catalog.immutable_unaccent(name) gin_trgm_ops);
                """);
        }

        // The extensions stay: they belong to the database, and another module may use them.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX catalog.ix_products_name_trigram;
                DROP INDEX catalog.ix_products_search_vector;
                ALTER TABLE catalog.products DROP COLUMN search_vector;
                DROP FUNCTION catalog.immutable_unaccent(text);
                DROP TEXT SEARCH CONFIGURATION catalog.portuguese_unaccent;
                """);
        }
    }
}
