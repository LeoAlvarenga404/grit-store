using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grit.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "catalog",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "catalog",
                table: "categories",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "catalog",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "catalog",
                table: "categories");
        }
    }
}
