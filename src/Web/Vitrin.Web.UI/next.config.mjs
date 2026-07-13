/** @type {import('next').NextConfig} */
const nextConfig = {
  // Docker imaj boyutunu ~150-200MB'a düşüren standalone output modu
  output: 'standalone',
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
}

export default nextConfig
