import { CalendarDays } from 'lucide-react'
import { ProductFeed } from '@/components/product-feed'
import { CategoryMenu } from '@/components/category-menu'
import { LeaderboardWidget } from '@/components/leaderboard-widget'
import { TrendingProducts } from '@/components/trending-products'
import Link from 'next/link'

export default function HomePage() {
  return (
    <div className="min-h-screen bg-background">

      <main className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 sm:py-12">
        <div className="mb-6 flex flex-col gap-1">
          <div className="flex items-center gap-2 text-sm font-medium text-primary">
            <CalendarDays className="h-4 w-4" aria-hidden="true" />
            <span>Bugün</span>
          </div>
          <h1 className="text-pretty text-3xl font-extrabold tracking-tight text-foreground sm:text-4xl">
            Günün Ürünleri
          </h1>
          <p className="text-balance text-sm text-muted-foreground sm:text-base">
            Topluluğun bugün keşfettiği en yeni ürünler. En sevdiğine oy ver, öne çıkmasına yardım et.
          </p>
        </div>

        <TrendingProducts />

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 mt-8">
          <div className="lg:col-span-3 space-y-8">
            <CategoryMenu />
            <ProductFeed />
          </div>
          
          <div className="lg:col-span-1 hidden lg:block">
            <div>
              <LeaderboardWidget />
            </div>
          </div>
        </div>

        <p className="mt-12 text-center text-sm text-muted-foreground">
          Daha fazlasını mı arıyorsun?{' '}
          <Link
            href="/discover"
            className="font-semibold text-primary underline-offset-4 hover:underline"
          >
            Tüm ürünleri keşfet
          </Link>
        </p>
      </main>
    </div>
  )
}
