using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Product.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddP1CommunityReviewsAndCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Collections",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEditorial",
                table: "Collections",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CollectionFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionFollows_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunityThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Body = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityThreads_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductChangelogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductChangelogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductChangelogEntries_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    UsageStatus = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunityReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentReplyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityReplies_CommunityReplies_ParentReplyId",
                        column: x => x.ParentReplyId,
                        principalTable: "CommunityReplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityReplies_CommunityThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "CommunityThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunityReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityReports_CommunityThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "CommunityThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunityThreadFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityThreadFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityThreadFollows_CommunityThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "CommunityThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductReviewHelpfulVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviewHelpfulVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviewHelpfulVotes_ProductReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "ProductReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunityReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityReactions_CommunityReplies_ReplyId",
                        column: x => x.ReplyId,
                        principalTable: "CommunityReplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunityReactions_CommunityThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "CommunityThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionFollows_CollectionId",
                table: "CollectionFollows",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionFollows_UserId_CollectionId",
                table: "CollectionFollows",
                columns: new[] { "UserId", "CollectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReactions_ReplyId",
                table: "CommunityReactions",
                column: "ReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReactions_ThreadId",
                table: "CommunityReactions",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReactions_UserId_ReplyId",
                table: "CommunityReactions",
                columns: new[] { "UserId", "ReplyId" },
                unique: true,
                filter: "\"ReplyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReactions_UserId_ThreadId",
                table: "CommunityReactions",
                columns: new[] { "UserId", "ThreadId" },
                unique: true,
                filter: "\"ThreadId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReplies_ParentReplyId",
                table: "CommunityReplies",
                column: "ParentReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReplies_ThreadId_CreatedAtUtc",
                table: "CommunityReplies",
                columns: new[] { "ThreadId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_ReporterId_ThreadId",
                table: "CommunityReports",
                columns: new[] { "ReporterId", "ThreadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_ThreadId",
                table: "CommunityReports",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityThreadFollows_ThreadId",
                table: "CommunityThreadFollows",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityThreadFollows_UserId_ThreadId",
                table: "CommunityThreadFollows",
                columns: new[] { "UserId", "ThreadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityThreads_ProductId_CreatedAtUtc",
                table: "CommunityThreads",
                columns: new[] { "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityThreads_Slug",
                table: "CommunityThreads",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductChangelogEntries_ProductId_PublishedAtUtc",
                table: "ProductChangelogEntries",
                columns: new[] { "ProductId", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviewHelpfulVotes_ReviewId",
                table: "ProductReviewHelpfulVotes",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviewHelpfulVotes_UserId_ReviewId",
                table: "ProductReviewHelpfulVotes",
                columns: new[] { "UserId", "ReviewId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ProductId_CreatedAtUtc",
                table: "ProductReviews",
                columns: new[] { "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_UserId_ProductId",
                table: "ProductReviews",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionFollows");

            migrationBuilder.DropTable(
                name: "CommunityReactions");

            migrationBuilder.DropTable(
                name: "CommunityReports");

            migrationBuilder.DropTable(
                name: "CommunityThreadFollows");

            migrationBuilder.DropTable(
                name: "ProductChangelogEntries");

            migrationBuilder.DropTable(
                name: "ProductReviewHelpfulVotes");

            migrationBuilder.DropTable(
                name: "CommunityReplies");

            migrationBuilder.DropTable(
                name: "ProductReviews");

            migrationBuilder.DropTable(
                name: "CommunityThreads");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "IsEditorial",
                table: "Collections");
        }
    }
}
