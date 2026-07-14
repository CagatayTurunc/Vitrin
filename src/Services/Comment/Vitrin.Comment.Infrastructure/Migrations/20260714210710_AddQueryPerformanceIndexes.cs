using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Comment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ProductId_CreatedAt_Id",
                table: "Comments",
                columns: new[] { "ProductId", "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ProductId_CreatedAt_Id",
                table: "Comments");
        }
    }
}
