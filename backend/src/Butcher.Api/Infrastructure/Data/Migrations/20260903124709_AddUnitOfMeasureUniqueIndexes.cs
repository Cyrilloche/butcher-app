using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitOfMeasureUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_unit_of_measure_abbreviation",
                table: "unit_of_measure");

            migrationBuilder.DropIndex(
                name: "ix_unit_of_measure_label",
                table: "unit_of_measure");
        }
    }
}
