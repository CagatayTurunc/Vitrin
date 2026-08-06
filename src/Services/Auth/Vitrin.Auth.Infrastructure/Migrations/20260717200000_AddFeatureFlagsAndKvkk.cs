using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Vitrin.Auth.Infrastructure.Data;

#nullable disable

namespace Vitrin.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AuthDbContext))]
    [Migration("20260717200000_AddFeatureFlagsAndKvkk")]
    public partial class AddFeatureFlagsAndKvkk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // KVKK: soft delete alanları Users tablosuna ekleniyor
            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteRequestedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeleteRequestedAtUtc",
                table: "Users",
                column: "DeleteRequestedAtUtc",
                filter: "\"DeleteRequestedAtUtc\" IS NOT NULL");

            // Feature Flags tablosu
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

            migrationBuilder.CreateIndex(
                name: "UX_FeatureFlags_Key",
                table: "FeatureFlags",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FeatureFlags");
            migrationBuilder.DropIndex(name: "IX_Users_DeleteRequestedAtUtc", table: "Users");
            migrationBuilder.DropColumn(name: "DeleteRequestedAtUtc", table: "Users");
            migrationBuilder.DropColumn(name: "AnonymizedAtUtc", table: "Users");
        }
    }
}
