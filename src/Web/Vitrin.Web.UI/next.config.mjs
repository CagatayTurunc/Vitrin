import { fileURLToPath } from 'node:url'

const projectRoot = fileURLToPath(new URL('.', import.meta.url))

// Runtime'da okunur (build'e gömülmez) — Docker environment'dan gelir
const internalApiUrl = process.env.INTERNAL_API_URL ?? 'http://localhost:5000'

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
    unoptimized: true,
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
}

export default nextConfig
