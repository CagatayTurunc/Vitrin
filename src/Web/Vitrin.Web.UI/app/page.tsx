import { CalendarDays } from 'lucide-react'
import { SiteHeader } from '@/components/site-header'
import { ProductFeed } from '@/components/product-feed'
import { CategoryMenu } from '@/components/category-menu'

export default function HomePage() {
  return (
    <div className="min-h-screen bg-background">

      <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-12">
        <div className="mb-6 flex flex-col gap-1">
          <div className="flex items-center gap-2 text-sm font-medium text-primary">
            <CalendarDays className="h-4 w-4" aria-hidden="true" />
            <span>8 Temmuz 2026</span>
          </div>
          <h1 className="text-pretty text-3xl font-extrabold tracking-tight text-foreground sm:text-4xl">
            Günün Ürünleri
          </h1>
          <p className="text-balance text-sm text-muted-foreground sm:text-base">
            Topluluğun bugün keşfettiği en yeni ürünler. En sevdiğine oy ver, öne çıkmasına yardım et.
          </p>
        </div>

        <CategoryMenu />

        <div className="mt-8">
          <ProductFeed />
        </div>

        <p className="mt-12 text-center text-sm text-muted-foreground">
          Daha fazlasını mı arıyorsun?{' '}
          <a
            href="#"
            className="font-semibold text-primary underline-offset-4 hover:underline"
          >
            Tüm ürünleri keşfet
          </a>
        </p>
      </main>
    </div>
  )
}
