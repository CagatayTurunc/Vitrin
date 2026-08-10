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
  images: {
    unoptimized: true,
  },
  // Browser'dan gelen /api/* isteklerini sunucu üzerinden gateway'e yönlendir.
  // /api/auth/* NextAuth'un kendi route'u olduğu için hariç tutulur.
  async rewrites() {
    return [
      {
        source: '/api/((?!auth/).*)',
        destination: `${internalApiUrl}/api/$1`,
      },
    ]
  },
}

export default nextConfig
