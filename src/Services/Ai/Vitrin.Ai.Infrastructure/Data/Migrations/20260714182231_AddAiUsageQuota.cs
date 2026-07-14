using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitrin.Ai.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiUsageQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastRequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageQuotas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageQuotas_UserId_PeriodStartUtc",
                table: "AiUsageQuotas",
                columns: new[] { "UserId", "PeriodStartUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiUsageQuotas");
        }
    }
}
