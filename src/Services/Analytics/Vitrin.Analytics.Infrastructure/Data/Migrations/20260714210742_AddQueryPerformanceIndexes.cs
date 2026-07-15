using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Analytics.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EventType_ProductId_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "EventType", "ProductId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalyticsEvents_EventType_ProductId_CreatedAt",
                table: "AnalyticsEvents");
        }
    }
}
