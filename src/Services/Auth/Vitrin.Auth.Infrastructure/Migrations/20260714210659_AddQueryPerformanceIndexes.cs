using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserFollows_FollowingId",
                table: "UserFollows");

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowingId_CreatedAt",
                table: "UserFollows",
                columns: new[] { "FollowingId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserFollows_FollowingId_CreatedAt",
                table: "UserFollows");

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowingId",
                table: "UserFollows",
                column: "FollowingId");
        }
    }
}
