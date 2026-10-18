using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grit.Modules.Catalog.Infrastructure.Migrations
{
    // Hand-written (ADR 0008, exception Q2) to keep the images: every key moves into the jsonb list with a colour. Products
    // with images had one SKU colour in practice, so each image takes the "cor" of the product's first SKU (by id); a
    // product without SKUs keeps its images without colour.
    public partial class ProductImageColors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE catalog.products ADD images jsonb NOT NULL DEFAULT '[]';

                UPDATE catalog.products p
                SET images = (
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'key', k.image_key,
                            'color', (SELECT s.attributes ->> 'cor' FROM catalog.skus s WHERE s.product_id = p.id ORDER BY s.id LIMIT 1))
                        ORDER BY k.position)
                    FROM unnest(p.image_keys) WITH ORDINALITY AS k(image_key, position))
                WHERE cardinality(p.image_keys) > 0;

                ALTER TABLE catalog.products ALTER COLUMN images DROP DEFAULT;
                ALTER TABLE catalog.products DROP COLUMN image_keys;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE catalog.products ADD image_keys text[] NOT NULL DEFAULT '{}';

                UPDATE catalog.products p
                SET image_keys = ARRAY(
                    SELECT e.image ->> 'key'
                    FROM jsonb_array_elements(p.images) WITH ORDINALITY AS e(image, position)
                    ORDER BY e.position);

                ALTER TABLE catalog.products DROP COLUMN images;
                """);
        }
    }
}
