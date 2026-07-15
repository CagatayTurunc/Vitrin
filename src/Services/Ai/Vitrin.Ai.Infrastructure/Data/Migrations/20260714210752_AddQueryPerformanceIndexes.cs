using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Ai.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisResults_ProductId_AnalyzedAt",
                table: "AiAnalysisResults",
                columns: new[] { "ProductId", "AnalyzedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiAnalysisResults_ProductId_AnalyzedAt",
                table: "AiAnalysisResults");
        }
    }
}
