using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grit.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "image_keys",
                schema: "catalog",
                table: "products",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_keys",
                schema: "catalog",
                table: "products");
        }
    }
}
