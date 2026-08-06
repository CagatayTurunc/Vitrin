import type { MetadataRoute } from "next";
import type { ProductCategory } from "@/core/domain/product.types";
import { serverApiFetch } from "@/lib/server-api";

type SitemapProduct = { slug: string; publishedAt?: string | null };
const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3001";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [products, categories] = await Promise.all([
    serverApiFetch<SitemapProduct[]>("/products/sitemap", { revalidate: 3600, tags: ["sitemap-products"] }),
    serverApiFetch<ProductCategory[]>("/categories", { revalidate: 3600, tags: ["categories"] }),
  ]);

  const staticRoutes: MetadataRoute.Sitemap = [
    ["", "daily", 1], ["/launches", "hourly", 0.9], ["/launches/upcoming", "hourly", 0.8],
    ["/categories", "daily", 0.8], ["/discover", "hourly", 0.8], ["/search", "weekly", 0.7],
    ["/collections", "daily", 0.7], ["/leaderboard", "daily", 0.6], ["/discussions", "daily", 0.6],
    ["/events", "daily", 0.5], ["/blog", "weekly", 0.5], ["/about", "monthly", 0.3],
  ].map(([path, changeFrequency, priority]) => ({ url: `${siteUrl}${path}`, lastModified: new Date(), changeFrequency: changeFrequency as MetadataRoute.Sitemap[number]["changeFrequency"], priority: priority as number }));

  return [
    ...staticRoutes,
    ...(products ?? []).map(product => ({ url: `${siteUrl}/product/${product.slug}`, lastModified: product.publishedAt ? new Date(product.publishedAt) : new Date(), changeFrequency: "weekly" as const, priority: 0.8 })),
    ...(categories ?? []).map(category => ({ url: `${siteUrl}/category/${category.slug}`, lastModified: new Date(), changeFrequency: "daily" as const, priority: 0.7 })),
  ];
}
