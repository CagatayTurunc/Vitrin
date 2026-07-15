using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseInsensitiveIdentityIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "Users",
                newName: "UX_Users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "Users",
                newName: "UX_Users_Email");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Users"
                        GROUP BY lower("Email")
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Users contain duplicate email addresses that differ only by case.';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM "Users"
                        GROUP BY lower("Username")
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Users contain duplicate usernames that differ only by case.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "citext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_MakerApplications_Status_CreatedAt",
                table: "MakerApplications",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MakerApplications_Status_CreatedAt",
                table: "MakerApplications");

            migrationBuilder.RenameIndex(
                name: "UX_Users_Username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameIndex(
                name: "UX_Users_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);
        }
    }
}
