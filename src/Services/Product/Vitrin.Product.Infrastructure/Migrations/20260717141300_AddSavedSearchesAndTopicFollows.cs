using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Product.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedSearchesAndTopicFollows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Query = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TopicSlugsCsv = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MinUpvotes = table.Column<int>(type: "integer", nullable: true),
                    MinComments = table.Column<int>(type: "integer", nullable: true),
                    MinViews = table.Column<int>(type: "integer", nullable: true),
                    PublishedFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Sort = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NotifyOnNewMatches = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopicFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicFollows_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_UserId_CreatedAtUtc",
                table: "SavedSearches",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SavedSearches_UserId_Name",
                table: "SavedSearches",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopicFollows_TopicId",
                table: "TopicFollows",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "UX_TopicFollows_UserId_TopicId",
                table: "TopicFollows",
                columns: new[] { "UserId", "TopicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedSearches");

            migrationBuilder.DropTable(
                name: "TopicFollows");
        }
    }
}
