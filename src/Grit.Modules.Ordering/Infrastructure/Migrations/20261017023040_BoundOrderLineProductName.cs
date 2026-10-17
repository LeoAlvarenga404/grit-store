using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grit.Modules.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BoundOrderLineProductName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "product_name",
                schema: "ordering",
                table: "order_lines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "product_name",
                schema: "ordering",
                table: "order_lines",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
