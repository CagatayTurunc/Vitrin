using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vitrin.Product.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductLaunchesAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategories_ProductCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductLaunches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    VersionLabel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    GalleryUrls = table.Column<List<string>>(type: "text[]", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    FinalRank = table.Column<int>(type: "integer", nullable: true),
                    FinalScore = table.Column<double>(type: "double precision", precision: 12, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductLaunches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductLaunches_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategoryAssignments",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategoryAssignments", x => new { x.ProductId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_ProductCategoryAssignments_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductCategoryAssignments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Id", "Description", "IsActive", "Name", "ParentId", "Slug", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Yazılım geliştirme, altyapı, API, test ve DevOps ürünleri.", true, "Mühendislik ve Geliştirme", null, "muhendislik-gelistirme", 10 },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Bireysel ve ekip üretkenliğini artıran araçlar.", true, "Üretkenlik", null, "uretkenlik", 20 },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Müşteri kazanımı, satış ve büyüme ürünleri.", true, "Pazarlama ve Satış", null, "pazarlama-satis", 30 },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Tasarım, içerik ve yaratıcı üretim araçları.", true, "Tasarım ve Yaratıcılık", null, "tasarim-yaraticilik", 40 },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Finans, muhasebe ve işletme operasyonu ürünleri.", true, "Finans ve İşletme", null, "finans-isletme", 50 },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Topluluk, iletişim ve profesyonel ağ ürünleri.", true, "Sosyal ve Topluluk", null, "sosyal-topluluk", 60 },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Yapay zekâ tabanlı ürünler, modeller ve ajanlar.", true, "Yapay Zekâ", null, "yapay-zeka", 70 },
                    { new Guid("20000000-0000-0000-0000-000000000001"), "Kodlama ve geliştirici deneyimi araçları.", true, "Geliştirici Araçları", new Guid("10000000-0000-0000-0000-000000000001"), "gelistirici-araclari", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "API geliştirme, test ve entegrasyon araçları.", true, "API Araçları", new Guid("10000000-0000-0000-0000-000000000001"), "api-araclari", 20 },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "Dağıtım, gözlemlenebilirlik ve altyapı araçları.", true, "DevOps", new Guid("10000000-0000-0000-0000-000000000001"), "devops", 30 },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "Takımlar için iletişim ve iş birliği ürünleri.", true, "Ekip İş Birliği", new Guid("10000000-0000-0000-0000-000000000002"), "ekip-is-birligi", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "Planlama, görev ve proje yönetimi ürünleri.", true, "Proje Yönetimi", new Guid("10000000-0000-0000-0000-000000000002"), "proje-yonetimi", 20 },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "Müşteri ilişkileri ve satış süreçleri araçları.", true, "CRM", new Guid("10000000-0000-0000-0000-000000000003"), "crm", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000007"), "Kampanya ve büyüme otomasyonu araçları.", true, "Pazarlama Otomasyonu", new Guid("10000000-0000-0000-0000-000000000003"), "pazarlama-otomasyonu", 20 },
                    { new Guid("20000000-0000-0000-0000-000000000008"), "Grafik ve görsel tasarım ürünleri.", true, "Grafik Tasarım", new Guid("10000000-0000-0000-0000-000000000004"), "grafik-tasarim", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000009"), "Video, ses ve medya üretim araçları.", true, "Video ve Ses", new Guid("10000000-0000-0000-0000-000000000004"), "video-ses", 20 },
                    { new Guid("20000000-0000-0000-0000-000000000010"), "Fatura, gider ve ön muhasebe ürünleri.", true, "Ön Muhasebe", new Guid("10000000-0000-0000-0000-000000000005"), "on-muhasebe", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000011"), "Ödeme, bankacılık ve finansal teknoloji ürünleri.", true, "Fintech", new Guid("10000000-0000-0000-0000-000000000005"), "fintech", 20 },
                    { new Guid("20000000-0000-0000-0000-000000000012"), "Topluluk kurma ve yönetme araçları.", true, "Topluluk Yönetimi", new Guid("10000000-0000-0000-0000-000000000006"), "topluluk-yonetimi", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000013"), "Görevleri otonom veya yarı otonom yürüten ajanlar.", true, "AI Ajanları", new Guid("10000000-0000-0000-0000-000000000007"), "ai-ajanlari", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000014"), "Metin, görsel, video ve kod üreten yapay zekâ ürünleri.", true, "Üretken AI", new Guid("10000000-0000-0000-0000-000000000007"), "uretken-ai", 20 }
                });

            // Backfill one launch snapshot per existing product. ProductStatus values are
            // mapped explicitly because ProductLaunchStatus has a launch-centric ordering.
            migrationBuilder.Sql(
                """
                INSERT INTO "ProductLaunches" (
                    "Id", "ProductId", "SequenceNumber", "VersionLabel", "Tagline", "Description",
                    "ThumbnailUrl", "GalleryUrls", "Status", "CreatedAtUtc", "ScheduledAtUtc",
                    "PublishedAtUtc", "ArchivedAtUtc", "IsFeatured", "FinalRank", "FinalScore")
                SELECT
                    md5(p."Id"::text || ':launch:1')::uuid,
                    p."Id",
                    1,
                    'İlk Lansman',
                    p."Tagline",
                    p."Description",
                    COALESCE(p."ThumbnailUrl", ''),
                    COALESCE(p."GalleryUrls", ARRAY[]::text[]),
                    CASE p."Status"
                        WHEN 0 THEN 0 -- Draft
                        WHEN 1 THEN 1 -- UnderReview
                        WHEN 2 THEN 3 -- Published
                        WHEN 3 THEN 4 -- Rejected
                        WHEN 4 THEN 5 -- Archived
                        WHEN 5 THEN 2 -- Scheduled
                        ELSE 0
                    END,
                    p."CreatedAt",
                    p."ScheduledLaunchAt",
                    p."PublishedAt",
                    p."ArchivedAt",
                    FALSE,
                    NULL,
                    NULL
                FROM "Products" p
                WHERE NOT EXISTS (
                    SELECT 1 FROM "ProductLaunches" launch WHERE launch."ProductId" = p."Id");
                """);

            // Preserve discovery quality for existing records by mapping well-known topics
            // into the controlled taxonomy. Makers can refine these assignments later.
            migrationBuilder.Sql(
                """
                INSERT INTO "ProductCategoryAssignments" ("ProductId", "CategoryId")
                SELECT DISTINCT product_topic."ProductItemId",
                    CASE
                        WHEN topic."Slug" IN ('yapay-zeka', 'ai', 'artificial-intelligence') THEN '10000000-0000-0000-0000-000000000007'::uuid
                        WHEN topic."Slug" IN ('gelistirici-araclari', 'developer-tools', 'acik-kaynak') THEN '20000000-0000-0000-0000-000000000001'::uuid
                        WHEN topic."Slug" IN ('fintech', 'finans') THEN '20000000-0000-0000-0000-000000000011'::uuid
                        WHEN topic."Slug" IN ('tasarim', 'design') THEN '10000000-0000-0000-0000-000000000004'::uuid
                        WHEN topic."Slug" IN ('verimlilik', 'productivity') THEN '10000000-0000-0000-0000-000000000002'::uuid
                        WHEN topic."Slug" IN ('saas', 'web', 'mobil') THEN '10000000-0000-0000-0000-000000000001'::uuid
                        ELSE NULL
                    END
                FROM "ProductItemTopic" product_topic
                JOIN "Topics" topic ON topic."Id" = product_topic."TopicsId"
                WHERE topic."Slug" IN (
                    'yapay-zeka', 'ai', 'artificial-intelligence', 'gelistirici-araclari',
                    'developer-tools', 'acik-kaynak', 'fintech', 'finans', 'tasarim',
                    'design', 'verimlilik', 'productivity', 'saas', 'web', 'mobil')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ParentId_SortOrder",
                table: "ProductCategories",
                columns: new[] { "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_ProductCategories_Slug",
                table: "ProductCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategoryAssignments_CategoryId",
                table: "ProductCategoryAssignments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLaunches_Status_PublishedAtUtc_Id",
                table: "ProductLaunches",
                columns: new[] { "Status", "PublishedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductLaunches_Status_ScheduledAtUtc_Id",
                table: "ProductLaunches",
                columns: new[] { "Status", "ScheduledAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_ProductLaunches_ProductId_SequenceNumber",
                table: "ProductLaunches",
                columns: new[] { "ProductId", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductCategoryAssignments");

            migrationBuilder.DropTable(
                name: "ProductLaunches");

            migrationBuilder.DropTable(
                name: "ProductCategories");
        }
    }
}
