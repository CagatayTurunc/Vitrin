using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Product.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductUpvotes_ProductItemId",
                table: "ProductUpvotes");

            migrationBuilder.RenameIndex(
                name: "IX_Topics_Slug",
                table: "Topics",
                newName: "UX_Topics_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_Products_Slug",
                table: "Products",
                newName: "UX_Products_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_Collections_Slug",
                table: "Collections",
                newName: "UX_Collections_Slug");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            // Product is a read model for Voting. Preserve the oldest copy before enforcing idempotency.
            migrationBuilder.Sql("""
                DELETE FROM "ProductUpvotes" AS duplicate
                USING "ProductUpvotes" AS canonical
                WHERE duplicate."ProductItemId" = canonical."ProductItemId"
                  AND duplicate."UserId" = canonical."UserId"
                  AND (duplicate."CreatedAt", duplicate."Id") > (canonical."CreatedAt", canonical."Id");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Name_Trgm",
                table: "Topics",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "UX_ProductUpvotes_ProductId_UserId",
                table: "ProductUpvotes",
                columns: new[] { "ProductItemId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Description_Trgm",
                table: "Products",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_MakerId",
                table: "Products",
                column: "MakerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name_Trgm",
                table: "Products",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Status_PublishedAt_Id",
                table: "Products",
                columns: new[] { "Status", "PublishedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Tagline_Trgm",
                table: "Products",
                column: "Tagline")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Collections_UserId_CreatedAt",
                table: "Collections",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Topics_Name_Trgm",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "UX_ProductUpvotes_ProductId_UserId",
                table: "ProductUpvotes");

            migrationBuilder.DropIndex(
                name: "IX_Products_Description_Trgm",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_MakerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name_Trgm",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Status_PublishedAt_Id",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Tagline_Trgm",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Collections_UserId_CreatedAt",
                table: "Collections");

            migrationBuilder.RenameIndex(
                name: "UX_Topics_Slug",
                table: "Topics",
                newName: "IX_Topics_Slug");

            migrationBuilder.RenameIndex(
                name: "UX_Products_Slug",
                table: "Products",
                newName: "IX_Products_Slug");

            migrationBuilder.RenameIndex(
                name: "UX_Collections_Slug",
                table: "Collections",
                newName: "IX_Collections_Slug");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpvotes_ProductItemId",
                table: "ProductUpvotes",
                column: "ProductItemId");
        }
    }
}
