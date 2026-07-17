using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Product.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionVisibilityAndCollaborators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Collections",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "CollectionCollaborators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionCollaborators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionCollaborators_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collections_Visibility_CreatedAt",
                table: "Collections",
                columns: new[] { "Visibility", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCollaborators_UserId",
                table: "CollectionCollaborators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_CollectionCollaborators_CollectionId_UserId",
                table: "CollectionCollaborators",
                columns: new[] { "CollectionId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionCollaborators");

            migrationBuilder.DropIndex(
                name: "IX_Collections_Visibility_CreatedAt",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Collections");
        }
    }
}
