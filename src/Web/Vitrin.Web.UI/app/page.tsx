import { PremiumHero } from '@/components/premium-hero'
import { LiveDiscoveryTicker } from '@/components/live-discovery-ticker'
import { FeaturedProducts } from '@/components/featured-products'
import { TrendingSection } from '@/components/trending-section'
import { TopicsSection } from '@/components/topics-section'
import { serverApiFetch } from '@/lib/server-api'
import type { CursorPage, ProductApiModel } from '@/core/domain/product.types'

// API'ye bağımlı server component'ler build sırasında prerender edilemez
export const dynamic = 'force-dynamic'

export default async function HomePage() {
  const [featured, trending, ticker] = await Promise.all([
    serverApiFetch<CursorPage<ProductApiModel>>('/products?sort=newest&pageSize=6', { cache: 'no-store' }),
    serverApiFetch<CursorPage<ProductApiModel>>('/products?sort=trending&pageSize=3', { cache: 'no-store' }),
    serverApiFetch<CursorPage<ProductApiModel>>('/products?sort=most_voted&pageSize=8', { cache: 'no-store' }),
  ])

  return (
    <div className="min-h-screen bg-background">
      {/* Hero Section */}
      <PremiumHero />

      {/* Live Discovery Ticker */}
      <LiveDiscoveryTicker products={ticker?.items ?? []} />

      {/* Featured Products */}
      <FeaturedProducts products={featured?.items ?? []} />

      {/* Trending Section */}
      <div id="trendler">
        <TrendingSection products={trending?.items ?? []} />
      </div>

      {/* Topics Section */}
      <div id="konular">
        <TopicsSection />
      </div>
    </div>
  )
}
