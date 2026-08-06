using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Notification.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferencesAndDigests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    InAppEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DigestFrequency = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductUpdatesEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommentsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MentionsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReactionsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SocialEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModerationEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastDigestSentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");
        }
    }
}
