import { fileURLToPath } from 'node:url'

const projectRoot = fileURLToPath(new URL('.', import.meta.url))

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
}

export default nextConfig
