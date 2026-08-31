using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Auth.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ApplicableTiers = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    MaxUsesPerUser = table.Column<int>(type: "integer", nullable: false),
                    DurationMonths = table.Column<int>(type: "integer", nullable: true),
                    CurrentUseCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByAdminId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RolloutPercentage = table.Column<int>(type: "integer", nullable: false),
                    AllowedRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VariantPayload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MakerApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioUrl = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MakerApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModerationAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Topic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeadLetteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IyzicoPaymentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IyzicoConversationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IyzicoCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IyzicoSubscriptionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    CanceledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrandfatherUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "citext", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "citext", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Headline = table.Column<string>(type: "text", nullable: true),
                    About = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    GithubUrl = table.Column<string>(type: "text", nullable: true),
                    LinkedInUrl = table.Column<string>(type: "text", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    GoogleId = table.Column<string>(type: "text", nullable: true),
                    GithubId = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false),
                    LastVoteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmailConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActiveBanId = table.Column<Guid>(type: "uuid", nullable: true),
                    SuspendedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspensionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeleteRequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AnonymizedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscountCodeUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentHistoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscountApplied = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodeUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountCodeUsages_DiscountCodes_DiscountCodeId",
                        column: x => x.DiscountCodeId,
                        principalTable: "DiscountCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModerationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetOwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationReports_Users_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBadges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserFollows",
                columns: table => new
                {
                    FollowerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFollows", x => new { x.FollowerId, x.FollowingId });
                    table.ForeignKey(
                        name: "FK_UserFollows_Users_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserFollows_Users_FollowingId",
                        column: x => x.FollowingId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModerationAppeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BanId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Statement = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationAppeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationAppeals_UserBans_BanId",
                        column: x => x.BanId,
                        principalTable: "UserBans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModerationAppeals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_IsActive_ExpiresAt",
                table: "DiscountCodes",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "UX_DiscountCodes_Code",
                table: "DiscountCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodeUsages_CodeId_UserId",
                table: "DiscountCodeUsages",
                columns: new[] { "DiscountCodeId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodeUsages_UserId",
                table: "DiscountCodeUsages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_FeatureFlags_Key",
                table: "FeatureFlags",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MakerApplications_Status_CreatedAt",
                table: "MakerApplications",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAppeals_BanId_UserId",
                table: "ModerationAppeals",
                columns: new[] { "BanId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAppeals_Status_CreatedAtUtc",
                table: "ModerationAppeals",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAppeals_UserId",
                table: "ModerationAppeals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAuditEntries_OccurredAtUtc_Id",
                table: "ModerationAuditEntries",
                columns: new[] { "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAuditEntries_ResourceType_ResourceId",
                table: "ModerationAuditEntries",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationReports_ReporterUserId_TargetType_TargetId",
                table: "ModerationReports",
                columns: new[] { "ReporterUserId", "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationReports_Status_CreatedAtUtc",
                table: "ModerationReports",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_DeadLetteredAtUtc_NextAttempt~",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "DeadLetteredAtUtc", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentHistories_IyzicoPaymentId",
                table: "PaymentHistories",
                column: "IyzicoPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentHistories_SubscriptionId_CreatedAt",
                table: "PaymentHistories",
                columns: new[] { "SubscriptionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status_CurrentPeriodEnd",
                table: "Subscriptions",
                columns: new[] { "Status", "CurrentPeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "UX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId",
                table: "UserBadges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_RevokedAtUtc_ExpiresAtUtc",
                table: "UserBans",
                columns: new[] { "RevokedAtUtc", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UserId_CreatedAtUtc",
                table: "UserBans",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowingId_CreatedAt",
                table: "UserFollows",
                columns: new[] { "FollowingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeleteRequestedAtUtc",
                table: "Users",
                column: "DeleteRequestedAtUtc",
                filter: "\"DeleteRequestedAtUtc\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_GithubId",
                table: "Users",
                column: "GithubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountCodeUsages");

            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "MakerApplications");

            migrationBuilder.DropTable(
                name: "ModerationAppeals");

            migrationBuilder.DropTable(
                name: "ModerationAuditEntries");

            migrationBuilder.DropTable(
                name: "ModerationReports");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PaymentHistories");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "UserBadges");

            migrationBuilder.DropTable(
                name: "UserFollows");

            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropTable(
                name: "UserBans");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
