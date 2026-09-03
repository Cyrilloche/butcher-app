using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAndRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_app_user_email",
                table: "app_user");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "app_user",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "access_failed_count",
                table: "app_user",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "app_user",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "email_confirmed",
                table: "app_user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lockout_enabled",
                table: "app_user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end",
                table: "app_user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "app_user",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_user_name",
                table: "app_user",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "app_user",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "app_user",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "phone_number_confirmed",
                table: "app_user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "security_stamp",
                table: "app_user",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "two_factor_enabled",
                table: "app_user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "user_name",
                table: "app_user",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "app_user_claim",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_claim", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_user_claim_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_login",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_login", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_app_user_login_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_token",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_token", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_app_user_token_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "app_user",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "app_user",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_user_claim_user_id",
                table: "app_user_claim",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_user_login_user_id",
                table: "app_user_login",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_token_hash",
                table: "refresh_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_user_id",
                table: "refresh_token",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_user_claim");

            migrationBuilder.DropTable(
                name: "app_user_login");

            migrationBuilder.DropTable(
                name: "app_user_token");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "app_user");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "access_failed_count",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "email_confirmed",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "lockout_enabled",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "lockout_end",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "normalized_user_name",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "phone_number_confirmed",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "user_name",
                table: "app_user");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "app_user",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_user_email",
                table: "app_user",
                column: "email",
                unique: true);
        }
    }
}
