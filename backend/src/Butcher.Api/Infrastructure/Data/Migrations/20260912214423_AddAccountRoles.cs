using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Comptes nominatifs avec rôle (ADR-011). Les comptes existants — en pratique le compte partagé —
    /// deviennent administrateurs actifs, pour qu'aucun accès ne soit perdu (FR-010).
    /// </summary>
    public partial class AddAccountRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Les colonnes obligatoires arrivent nullables, le temps d'être remplies.
            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "app_user",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "app_user",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "app_user",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_login_at",
                table: "app_user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "app_user",
                type: "timestamp with time zone",
                nullable: true);

            // 2. Reprise : tout compte existant est administrateur et actif. Le nom affiché provisoire
            //    est la partie locale de l'email, même règle que la création en ligne de commande.
            migrationBuilder.Sql(
                """
                UPDATE app_user
                SET role = 'admin',
                    is_active = TRUE,
                    display_name = COALESCE(NULLIF(split_part(email, '@', 1), ''), user_name, 'Administrateur');
                """);

            // 3. Les colonnes deviennent obligatoires, sans valeur par défaut en base : le rôle et l'état
            //    sont toujours écrits explicitement par l'application.
            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "app_user",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "app_user",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "app_user",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_name",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "last_login_at",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "role",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "app_user");
        }
    }
}
