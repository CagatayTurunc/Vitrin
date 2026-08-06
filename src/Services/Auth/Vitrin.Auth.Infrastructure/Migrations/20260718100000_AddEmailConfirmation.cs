using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Vitrin.Auth.Infrastructure.Data;

#nullable disable

namespace Vitrin.Auth.Infrastructure.Migrations;

[DbContext(typeof(AuthDbContext))]
[Migration("20260718100000_AddEmailConfirmation")]
public partial class AddEmailConfirmation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "EmailConfirmedAtUtc",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        // Existing accounts predate email verification and must remain usable.
        migrationBuilder.Sql("UPDATE \"Users\" SET \"EmailConfirmedAtUtc\" = \"CreatedAt\"");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EmailConfirmedAtUtc",
            table: "Users");
    }
}
