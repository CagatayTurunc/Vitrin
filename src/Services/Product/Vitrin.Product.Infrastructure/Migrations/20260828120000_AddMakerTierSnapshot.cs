using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Product.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMakerTierSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MakerTierSnapshot",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Free");

            // Index for efficient badge queries
            migrationBuilder.CreateIndex(
                name: "IX_Products_MakerTierSnapshot",
                table: "Products",
                column: "MakerTierSnapshot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_MakerTierSnapshot",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MakerTierSnapshot",
                table: "Products");
        }
    }
}
