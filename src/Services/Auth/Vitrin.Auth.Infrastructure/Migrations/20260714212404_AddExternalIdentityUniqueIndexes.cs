using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIdentityUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Users_GithubId",
                table: "Users",
                column: "GithubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Users_GithubId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_Users_GoogleId",
                table: "Users");
        }
    }
}
