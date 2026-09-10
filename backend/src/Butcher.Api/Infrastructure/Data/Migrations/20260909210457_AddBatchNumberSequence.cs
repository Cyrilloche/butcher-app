using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "batch_number_sequence",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    production_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_batch_number_sequence", x => new { x.product_id, x.production_date });
                    table.ForeignKey(
                        name: "fk_batch_number_sequence_products_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Initialisation depuis les lots déjà en base. Sans elle, le registre repartirait de zéro
            // et le premier lot créé après le déploiement réémettrait un numéro déjà porté par une
            // étiquette existante. Le comptage est fidèle tant qu'aucun lot n'a été supprimé, ce qui
            // est le cas : la suppression n'existait pas avant cette migration.
            migrationBuilder.Sql(
                """
                INSERT INTO batch_number_sequence (product_id, production_date, last_sequence)
                SELECT product_id, production_date, COUNT(*)
                FROM production_batch
                GROUP BY product_id, production_date
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "batch_number_sequence");
        }
    }
}
