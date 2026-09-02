import { PremiumHero } from '@/components/premium-hero'
import { LiveDiscoveryTicker } from '@/components/live-discovery-ticker'
import { FeaturedProducts } from '@/components/featured-products'
import { TrendingSection } from '@/components/trending-section'
import { TopicsSection } from '@/components/topics-section'
import { HomepagePricingSection } from '@/components/homepage-pricing-section'
import { UpgradePromoBanner } from '@/components/upgrade-promo-banner'
import { serverApiFetch } from '@/lib/server-api'
import type { CursorPage, ProductApiModel } from '@/core/domain/product.types'
import { generateSEO } from '@/lib/seo'
import type { Metadata } from 'next'

// API'ye bağımlı server component'ler build sırasında prerender edilemez
export const dynamic = 'force-dynamic'

// Madde 2.5 & 2.6: Ana sayfa için optimize edilmiş SEO
export const metadata: Metadata = generateSEO({
  title: 'Vitrin — Günün Ürünleri',
  description:
    'Türkiye\'nin en yeni ürünlerini keşfet, oy ver ve paylaş. Startup\'lar, SaaS ürünleri, mobil uygulamalar ve teknoloji ürünleri için en büyük keşif platformu.',
  path: '',
  keywords: [
    'ürün keşfi',
    'yeni ürünler',
    'startup',
    'product hunt türkiye',
    'yazılım ürünleri',
    'mobil uygulama',
    'saas',
    'teknoloji',
  ],
})

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

      {/* Upgrade promo — hero variant, FeaturedProducts ile Trending arasında */}
      <div className="mx-auto max-w-6xl px-4 pb-4">
        <UpgradePromoBanner variant="hero" />
      </div>

      {/* Trending Section */}
      <div id="trendler">
        <TrendingSection products={trending?.items ?? []} />
      </div>

      {/* Topics Section */}
      <div id="konular">
        <TopicsSection />
      </div>

      {/* Pricing Section */}
      <HomepagePricingSection />
    </div>
  )
}
