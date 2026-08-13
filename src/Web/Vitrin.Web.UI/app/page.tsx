import { PremiumHero } from '@/components/premium-hero'
import { LiveDiscoveryTicker } from '@/components/live-discovery-ticker'
import { FeaturedProducts } from '@/components/featured-products'
import { TrendingSection } from '@/components/trending-section'
import { TopicsSection } from '@/components/topics-section'

// API'ye bağımlı server component'ler build sırasında prerender edilemez
export const dynamic = 'force-dynamic'

export default function HomePage() {
  return (
    <div className="min-h-screen bg-background">
      {/* Hero Section */}
      <PremiumHero />

      {/* Live Discovery Ticker */}
      <LiveDiscoveryTicker />

      {/* Featured Products */}
      <FeaturedProducts />

      {/* Trending Section */}
      <div id="trendler">
        <TrendingSection />
      </div>

      {/* Topics Section */}
      <div id="konular">
        <TopicsSection />
      </div>
    </div>
  )
}
