using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Butcher.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Fait descendre le numéro d'étiquette de la fabrication vers l'unité physique.
    /// </summary>
    /// <remarks>
    /// La table de séquences est renommée plutôt que recréée, et les unités existantes sont
    /// renumérotées sur place : une migration destructive détruirait des données sur l'instance
    /// déployée, alors qu'un rétro-remplissage déterministe coûte quelques lignes. Le registre est
    /// ensuite recalé sur le dernier rang attribué, faute de quoi la première unité créée après la
    /// migration porterait un numéro déjà attribué.
    /// </remarks>
    public partial class AddUnitNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Le registre change d'objet compté, pas de structure : on le renomme.
            migrationBuilder.Sql(
                """
                ALTER TABLE batch_number_sequence RENAME TO unit_number_sequence;
                ALTER TABLE unit_number_sequence
                    RENAME CONSTRAINT pk_batch_number_sequence TO pk_unit_number_sequence;
                ALTER TABLE unit_number_sequence
                    RENAME CONSTRAINT fk_batch_number_sequence_products_product_id
                    TO fk_unit_number_sequence_product_product_id;
                """);

            // 2. La colonne arrive nullable, le temps d'être remplie.
            migrationBuilder.AddColumn<string>(
                name: "unit_number",
                table: "stock_unit",
                type: "text",
                nullable: true);

            // 3. Rétro-remplissage : rang par produit et par date de production, dans l'ordre de
            //    création des unités, qui est celui dans lequel elles ont été pesées.
            migrationBuilder.Sql(
                """
                WITH numbered AS (
                    SELECT
                        u.id AS unit_id,
                        p.code AS product_code,
                        b.production_date AS production_date,
                        ROW_NUMBER() OVER (
                            PARTITION BY b.product_id, b.production_date
                            ORDER BY u.id
                        ) AS rank
                    FROM stock_unit u
                    JOIN production_batch b ON b.id = u.batch_id
                    JOIN product p ON p.id = b.product_id
                )
                UPDATE stock_unit u
                SET unit_number = n.product_code || '-'
                    || to_char(n.production_date, 'YYMMDD') || '-' || n.rank
                FROM numbered n
                WHERE u.id = n.unit_id;
                """);

            // 4. Le registre repart du dernier rang attribué. Son ancien contenu comptait des
            //    fabrications : il n'a plus de sens et est remplacé, pas complété.
            migrationBuilder.Sql(
                """
                DELETE FROM unit_number_sequence;

                INSERT INTO unit_number_sequence (product_id, production_date, last_sequence)
                SELECT b.product_id, b.production_date, COUNT(*)
                FROM stock_unit u
                JOIN production_batch b ON b.id = u.batch_id
                GROUP BY b.product_id, b.production_date;
                """);

            // 5. La colonne devient l'identité de l'unité : non nulle et unique.
            migrationBuilder.AlterColumn<string>(
                name: "unit_number",
                table: "stock_unit",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_unit_unit_number",
                table: "stock_unit",
                column: "unit_number",
                unique: true);

            // 6. Le numéro de fabrication n'a plus de lecteur.
            migrationBuilder.DropIndex(
                name: "ix_production_batch_batch_number",
                table: "production_batch");

            migrationBuilder.DropColumn(
                name: "batch_number",
                table: "production_batch");
        }

        /// <summary>
        /// Retour en arrière de développement. Les numéros de fabrication d'origine ne sont pas
        /// reconstitués : ils sont réémis de façon déterministe, pour que la colonne unique et non
        /// nulle puisse exister à nouveau.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "batch_number",
                table: "production_batch",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH numbered AS (
                    SELECT
                        b.id AS batch_id,
                        p.code AS product_code,
                        b.production_date AS production_date,
                        ROW_NUMBER() OVER (
                            PARTITION BY b.product_id, b.production_date
                            ORDER BY b.id
                        ) AS rank
                    FROM production_batch b
                    JOIN product p ON p.id = b.product_id
                )
                UPDATE production_batch b
                SET batch_number = n.product_code || '-'
                    || to_char(n.production_date, 'YYMMDD') || '-' || n.rank
                FROM numbered n
                WHERE b.id = n.batch_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "batch_number",
                table: "production_batch",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_production_batch_batch_number",
                table: "production_batch",
                column: "batch_number",
                unique: true);

            migrationBuilder.DropIndex(
                name: "ix_stock_unit_unit_number",
                table: "stock_unit");

            migrationBuilder.DropColumn(
                name: "unit_number",
                table: "stock_unit");

            migrationBuilder.Sql(
                """
                DELETE FROM unit_number_sequence;

                INSERT INTO unit_number_sequence (product_id, production_date, last_sequence)
                SELECT product_id, production_date, COUNT(*)
                FROM production_batch
                GROUP BY product_id, production_date;

                ALTER TABLE unit_number_sequence
                    RENAME CONSTRAINT fk_unit_number_sequence_product_product_id
                    TO fk_batch_number_sequence_products_product_id;
                ALTER TABLE unit_number_sequence
                    RENAME CONSTRAINT pk_unit_number_sequence TO pk_batch_number_sequence;
                ALTER TABLE unit_number_sequence RENAME TO batch_number_sequence;
                """);
        }
    }
}
