import type { MetadataRoute } from "next";

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3001";

/**
 * Madde 2.1 & 2.4: Robots.txt konfigürasyonu
 * 
 * Arama motorlarına hangi sayfaların taranabileceğini bildirir.
 */
export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: "*",
        allow: "/",
        disallow: [
          "/admin/",           // Admin paneli
          "/dashboard",        // Kullanıcı dashboard
          "/my-products",      // Kullanıcıya özel sayfalar
          "/settings",         // Ayarlar sayfası
          "/api/",             // API endpoint'leri
          "/auth/",            // Auth sayfaları
          "/*?*sort=",         // Sort parametreli URL'ler (duplicate content)
          "/*?*page=",         // Pagination (sadece ilk sayfa index'lensin)
          "/search?",          // Arama sonuçları (parametre ile)
        ],
      },
      // Google bot için özel kurallar
      {
        userAgent: "Googlebot",
        allow: "/",
        disallow: [
          "/admin/",
          "/dashboard",
          "/my-products",
          "/settings",
          "/api/",
        ],
        crawlDelay: 0, // Google gecikme istemiyor
      },
      // Bing bot için özel kurallar
      {
        userAgent: "Bingbot",
        allow: "/",
        disallow: [
          "/admin/",
          "/dashboard",
          "/my-products",
          "/settings",
          "/api/",
        ],
        crawlDelay: 1, // Bing biraz yavaşlatalım
      },
    ],
    sitemap: `${siteUrl}/sitemap.xml`,
    // Madde 2.1: Host tanımla (www vs non-www)
    host: siteUrl,
  };
}
