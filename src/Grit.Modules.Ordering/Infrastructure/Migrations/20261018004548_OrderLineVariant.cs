using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grit.Modules.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderLineVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                schema: "ordering",
                table: "order_lines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_slug",
                schema: "ordering",
                table: "order_lines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "size",
                schema: "ordering",
                table: "order_lines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "product_slug",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "size",
                schema: "ordering",
                table: "order_lines");
        }
    }
}
