using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnitOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_units_of_measure_sale_unit_id",
                table: "product");

            migrationBuilder.DropTable(
                name: "unit_of_measure");

            migrationBuilder.DropIndex(
                name: "ix_product_sale_unit_id",
                table: "product");

            migrationBuilder.DropColumn(
                name: "sale_unit_id",
                table: "product");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sale_unit_id",
                table: "product",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "unit_of_measure",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    abbreviation = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_of_measure", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_sale_unit_id",
                table: "product",
                column: "sale_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_of_measure_abbreviation",
                table: "unit_of_measure",
                column: "abbreviation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unit_of_measure_label",
                table: "unit_of_measure",
                column: "label",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_units_of_measure_sale_unit_id",
                table: "product",
                column: "sale_unit_id",
                principalTable: "unit_of_measure",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
