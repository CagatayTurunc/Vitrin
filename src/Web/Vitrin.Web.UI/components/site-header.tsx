import { Search, Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export function SiteHeader() {
  return (
    <header className="sticky top-0 z-50 w-full border-b border-border bg-background/80 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-6xl items-center gap-4 px-4 sm:px-6">
        {/* Logo */}
        <a href="/" className="flex shrink-0 items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-sm shadow-primary/40 ring-1 ring-primary/30">
            <Sparkles className="h-5 w-5" aria-hidden="true" />
          </span>
          <span className="font-sans text-xl font-extrabold tracking-tight text-foreground">
            Vitrin
          </span>
        </a>

        {/* Search */}
        <div className="relative mx-auto hidden w-full max-w-md md:block">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden="true"
          />
          <Input
            type="search"
            placeholder="Ürün, kategori veya koleksiyon ara..."
            aria-label="Ara"
            className="h-10 rounded-full border-border bg-muted/60 pl-9 text-sm shadow-none focus-visible:bg-background"
          />
        </div>

        {/* Actions */}
        <div className="ml-auto flex shrink-0 items-center gap-2">
          <Button
            variant="ghost"
            size="icon"
            aria-label="Ara"
            className="md:hidden"
          >
            <Search className="h-5 w-5" />
          </Button>
          <Button variant="outline" className="rounded-full font-medium">
            Giriş Yap
          </Button>
          <Button className="rounded-full font-semibold shadow-sm shadow-primary/30">
            Ürün Ekle
          </Button>
        </div>
      </div>
    </header>
  )
}
