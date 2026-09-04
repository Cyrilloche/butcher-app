using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowPartialSaleToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_partial_sale",
                table: "product",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_partial_sale",
                table: "product");
        }
    }
}
