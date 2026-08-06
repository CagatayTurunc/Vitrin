using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Notification.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsletterSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    DailyLaunches = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeeklyRoundup = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProductUpdates = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpcomingLaunches = table.Column<bool>(type: "INTEGER", nullable: false),
                    AiDigest = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeveloperDigest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_NewsletterSubscriptions_EmailAddress",
                table: "NewsletterSubscriptions",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_NewsletterSubscriptions_UserId",
                table: "NewsletterSubscriptions",
                column: "UserId",
                unique: true,
                filter: "UserId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsletterSubscriptions");
        }
    }
}
