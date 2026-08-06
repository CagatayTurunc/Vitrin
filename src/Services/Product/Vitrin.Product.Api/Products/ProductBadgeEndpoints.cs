using System.Security;
using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;

namespace Vitrin.Product.Api.Products;

public static class ProductBadgeEndpoints
{
    public static void MapProductBadgeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products/{slug}/badge", GetBadgeInfo);
        app.MapGet("/api/products/{slug}/badge.svg", GetBadgeSvg);
    }

    private static async Task<IResult> GetBadgeInfo(string slug, ProductDbContext db, HttpContext context)
    {
        var item = await LoadBadge(slug, db, context.RequestAborted);
        if (item is null) return Results.NotFound();
        return Results.Ok(new
        {
            item.ProductName, item.ProductSlug, item.Rank,
            SvgUrl = $"/api/products/{item.ProductSlug}/badge.svg",
            ProductUrl = $"/product/{item.ProductSlug}"
        });
    }

    private static async Task<IResult> GetBadgeSvg(string slug, string? theme, ProductDbContext db, HttpContext context)
    {
        var item = await LoadBadge(slug, db, context.RequestAborted);
        if (item is null) return Results.NotFound();
        var dark = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase);
        var background = dark ? "#111827" : "#ffffff";
        var foreground = dark ? "#f9fafb" : "#111827";
        var accent = "#10b981";
        var name = SecurityElement.Escape(item.ProductName) ?? "Vitrin ürünü";
        var rankText = item.Rank is { } rank ? $"Günün #{rank} Ürünü" : "Vitrin'de keşfet";
        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="240" height="64" role="img" aria-label="{name} — {rankText}">
              <rect width="240" height="64" rx="14" fill="{background}" stroke="{accent}" stroke-width="2"/>
              <rect x="10" y="10" width="44" height="44" rx="11" fill="{accent}"/>
              <path d="M23 21h18l-2.5 17h-13z M27 18h10v5H27z" fill="#fff" opacity=".95"/>
              <text x="66" y="27" font-family="Arial, sans-serif" font-size="12" font-weight="700" fill="{foreground}">{name}</text>
              <text x="66" y="45" font-family="Arial, sans-serif" font-size="12" fill="{accent}">{SecurityElement.Escape(rankText)}</text>
              <text x="218" y="43" text-anchor="end" font-family="Arial, sans-serif" font-size="9" fill="{foreground}" opacity=".55">VİTRİN</text>
            </svg>
            """;
        context.Response.Headers.CacheControl = "public, max-age=300";
        return Results.Text(svg, "image/svg+xml; charset=utf-8");
    }

    private static async Task<BadgeData?> LoadBadge(string slug, ProductDbContext db, CancellationToken cancellationToken)
    {
        return await db.Products.AsNoTracking().Where(product => product.Slug == slug && product.Status == ProductStatus.Published)
            .Select(product => new BadgeData(
                product.Name,
                product.Slug,
                product.Launches.OrderByDescending(launch => launch.SequenceNumber).Select(launch => launch.FinalRank).FirstOrDefault()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record BadgeData(string ProductName, string ProductSlug, int? Rank);
}
