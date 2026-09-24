using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "assistant_enabled",
                table: "app_user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "voice_request",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    input_mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    heard_text = table.Column<string>(type: "text", nullable: true),
                    outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reply_speech = table.Column<string>(type: "text", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voice_request", x => x.id);
                    table.CheckConstraint("ck_voice_request_duration_ms", "duration_ms >= 0");
                    table.ForeignKey(
                        name: "fk_voice_request_app_user_account_id",
                        column: x => x.account_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_voice_request_account_id_occurred_at",
                table: "voice_request",
                columns: new[] { "account_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "voice_request");

            migrationBuilder.DropColumn(
                name: "assistant_enabled",
                table: "app_user");
        }
    }
}
