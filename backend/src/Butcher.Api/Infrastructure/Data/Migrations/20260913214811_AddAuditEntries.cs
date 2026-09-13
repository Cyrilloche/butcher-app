using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    entity_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    deleted_content = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entry", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_entry_app_user_account_id",
                        column: x => x.account_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entry_account_id_occurred_at",
                table: "audit_entry",
                columns: new[] { "account_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entry_occurred_at",
                table: "audit_entry",
                column: "occurred_at",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entry");
        }
    }
}
