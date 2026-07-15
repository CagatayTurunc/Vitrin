using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Notification.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt_Id",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt_Id",
                table: "Notifications");
        }
    }
}
