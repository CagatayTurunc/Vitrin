import { fileURLToPath } from 'node:url'

const projectRoot = fileURLToPath(new URL('.', import.meta.url))

// Runtime'da okunur (build'e gömülmez) — Docker environment'dan gelir
const internalApiUrl = process.env.INTERNAL_API_URL ?? 'http://localhost:5000'

// Bundle analyzer (pnpm analyze komutu ile)
const withBundleAnalyzer = (await import('@next/bundle-analyzer')).default({
  enabled: process.env.ANALYZE === 'true',
})

/** @type {import('next').NextConfig} */
const nextConfig = {
  // Docker imaj boyutunu ~150-200MB'a düşüren standalone output modu
  output: 'standalone',
  turbopack: {
    root: projectRoot,
  },
  // Madde 15 — Kodu karart: Production build'de source map kapalı.
  // Source map açık olursa saldırganlar minify edilmiş kodu orijinal haline çevirebilir.
  // Development'ta hata ayıklamak için bu satırı kaldırabilirsiniz.
  productionBrowserSourceMaps: false,
  images: {
    // unoptimized: true satırı kaldırıldı.
    // Docker standalone modunda Next.js image optimization çalışır —
    // WebP dönüşümü, responsive srcset, lazy loading hepsi aktif.
    // Cloudinary URL'leri (res.cloudinary.com) ve GitHub/Google avatar'ları için
    // remote pattern whitelist eklendi.
    remotePatterns: [
      { protocol: "https", hostname: "res.cloudinary.com" },
      { protocol: "https", hostname: "avatars.githubusercontent.com" },
      { protocol: "https", hostname: "lh3.googleusercontent.com" },
    ],
  },
  // Browser'dan gelen /api/* isteklerini sunucu üzerinden gateway'e yönlendir.
  // Next.js, rewrite kurallarından önce kendi route handler'larını kontrol eder;
  // bu sayede /api/auth/[...nextauth] NextAuth tarafından handle edilirken,
  // /api/auth/leaderboard gibi diğer /api/auth/* rotaları gateway'e proxy edilir.
  async rewrites() {
    // Tüm /api/* istekleri nginx tarafından doğrudan gateway'e yönlendirilir.
    // Bu rewrite fallback, sadece SSR (server-side) fetch'ler için gereklidir
    // (örn. getServerSideProps, Server Components, server actions).
    return {
      beforeFiles: [],
      afterFiles: [],
      fallback: [
        {
          source: '/api/:path*',
          destination: `${internalApiUrl}/api/:path*`,
        },
      ],
    }
  },
  // Madde 1.2: Performans optimizasyonları
  compress: true, // Gzip compression
  poweredByHeader: false, // X-Powered-By header'ı kaldır (güvenlik)
  reactStrictMode: true, // React strict mode
  swcMinify: true, // SWC ile minification
}

export default withBundleAnalyzer(nextConfig)
