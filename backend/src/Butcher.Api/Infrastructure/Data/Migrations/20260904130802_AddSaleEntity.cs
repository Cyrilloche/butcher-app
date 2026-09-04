using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_customer_customer_id",
                table: "stock_movement");

            // EF génère par défaut un RENAME de customer_id en sale_id : ce serait réinterpréter des
            // identifiants clients comme des identifiants de vente. On supprime la colonne et on en
            // crée une neuve — le client est désormais porté par "sale", plus par le mouvement.
            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "stock_movement");

            migrationBuilder.AddColumn<int>(
                name: "sale_id",
                table: "stock_movement",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_sale_id",
                table: "stock_movement",
                column: "sale_id");

            migrationBuilder.CreateTable(
                name: "sale",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_number = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    paid = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_app_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_sale_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sale_created_by_id",
                table: "sale",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_customer_id",
                table: "sale",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_date",
                table: "sale",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_sale_sale_number",
                table: "sale",
                column: "sale_number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_sale_sale_id",
                table: "stock_movement",
                column: "sale_id",
                principalTable: "sale",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_sale_sale_id",
                table: "stock_movement");

            migrationBuilder.DropTable(
                name: "sale");

            migrationBuilder.DropColumn(
                name: "sale_id",
                table: "stock_movement");

            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "stock_movement",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_customer_id",
                table: "stock_movement",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_customer_customer_id",
                table: "stock_movement",
                column: "customer_id",
                principalTable: "customer",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
