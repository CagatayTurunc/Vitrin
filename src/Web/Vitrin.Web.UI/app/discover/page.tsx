'use client'

import { FormEvent, useCallback, useEffect, useState } from 'react'
import { Filter, Loader2, RotateCcw, Search, SlidersHorizontal, Sparkles } from 'lucide-react'
import { ProductRow } from '@/components/product-row'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ProductRepository } from '@/core/infrastructure/product.repository'
import type { Product, ProductApiModel, ProductFilters, ProductSort, Topic } from '@/core/domain/product.types'
import { cn } from '@/lib/utils'
import { getErrorMessage } from '@/lib/errors'

const emptyFilters: ProductFilters = { sort: 'newest', topics: [] }

const sortOptions: { value: ProductSort; label: string }[] = [
  { value: 'newest', label: 'En yeni' },
  { value: 'trending', label: 'Trend skoru' },
  { value: 'most_voted', label: 'En çok oy alan' },
  { value: 'most_commented', label: 'En çok konuşulan' },
  { value: 'most_viewed', label: 'En çok görüntülenen' },
]

function mapProduct(product: ProductApiModel, rank: number): Product {
  return {
    id: product.id,
    rank,
    name: product.name,
    slug: product.slug,
    description: product.tagline || product.description,
    publishedAt: product.publishedAt,
    image: product.thumbnailUrl || '/products/notai.png',
    topics: product.topics ?? [],
    votes: product.upvotes ?? 0,
    views: product.viewCount ?? 0,
    comments: product.commentCount ?? 0,
    trendScore: product.trendScore ?? 0,
  }
}

export default function DiscoverPage() {
  const [topics, setTopics] = useState<Topic[]>([])
  const [draft, setDraft] = useState<ProductFilters>(emptyFilters)
  const [applied, setApplied] = useState<ProductFilters>(emptyFilters)
  const [products, setProducts] = useState<Product[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [hasMore, setHasMore] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingMore, setIsLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void ProductRepository.getTopics().then(setTopics).catch(() => setTopics([]))
  }, [])

  const fetchPage = useCallback(async (cursor?: string) => {
    if (cursor) setIsLoadingMore(true)
    else setIsLoading(true)
    setError(null)
    try {
      const page = await ProductRepository.filterProducts(applied, cursor)
      setProducts(previous => cursor
        ? [...previous, ...page.items.map((item, index) => mapProduct(item, previous.length + index + 1))]
        : page.items.map((item, index) => mapProduct(item, index + 1)))
      setNextCursor(page.nextCursor)
      setHasMore(page.hasMore)
    } catch (requestError) {
      setError(getErrorMessage(requestError, 'Ürünler filtrelenirken bir hata oluştu.'))
    } finally {
      setIsLoading(false)
      setIsLoadingMore(false)
    }
  }, [applied])

  useEffect(() => {
    // The applied filter snapshot intentionally starts a new remote page request.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void fetchPage()
  }, [fetchPage])

  const applyFilters = (event: FormEvent) => {
    event.preventDefault()
    commitFilters()
  }

  const commitFilters = () => {
    setApplied({ ...draft, topics: [...(draft.topics ?? [])] })
  }

  const resetFilters = () => {
    setDraft(emptyFilters)
    setApplied(emptyFilters)
  }

  const toggleTopic = (slug: string) => {
    setDraft(current => ({
      ...current,
      topics: current.topics?.includes(slug)
        ? current.topics.filter(topic => topic !== slug)
        : [...(current.topics ?? []), slug],
    }))
  }

  const setMinimum = (key: 'minUpvotes' | 'minComments' | 'minViews', value: string) => {
    setDraft(current => ({ ...current, [key]: value === '' ? undefined : Math.max(0, Number(value)) }))
  }

  const activeFilterCount = [
    applied.q,
    applied.topics?.length,
    applied.minUpvotes,
    applied.minComments,
    applied.minViews,
    applied.publishedFrom,
    applied.publishedTo,
  ].filter(value => value !== undefined && value !== '' && value !== 0).length

  return (
    <main className="mx-auto min-h-screen w-full max-w-7xl px-4 py-8 sm:px-6 sm:py-12">
      <section className="relative mb-8 overflow-hidden rounded-[2rem] border border-border bg-card px-6 py-8 sm:px-10">
        <div className="absolute -right-16 -top-20 h-64 w-64 rounded-full bg-primary/10 blur-3xl" />
        <div className="relative max-w-3xl">
          <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-primary/20 bg-primary/10 px-3 py-1 text-xs font-bold uppercase tracking-[0.18em] text-primary">
            <Sparkles className="h-3.5 w-3.5" /> Akıllı keşif
          </div>
          <h1 className="text-3xl font-black tracking-tight sm:text-5xl">Doğru ürünü daha hızlı bul</h1>
          <p className="mt-3 max-w-2xl text-muted-foreground">
            Kategori, yayın tarihi ve etkileşim eşikleriyle daralt; sonuçları trend, oy, yorum veya görüntülenmeye göre sırala.
          </p>
        </div>
      </section>

      <div className="grid items-start gap-6 lg:grid-cols-[320px_minmax(0,1fr)]">
        <form onSubmit={applyFilters} className="rounded-3xl border border-border bg-card p-5 shadow-sm lg:sticky lg:top-24">
          <div className="mb-5 flex items-center justify-between">
            <div className="flex items-center gap-2 font-bold"><SlidersHorizontal className="h-5 w-5 text-primary" /> Filtreler</div>
            {activeFilterCount > 0 && <span className="rounded-full bg-primary px-2 py-0.5 text-xs font-bold text-primary-foreground">{activeFilterCount}</span>}
          </div>

          <div className="space-y-5">
            <label className="block space-y-2 text-sm font-semibold">
              Ürün ara
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input value={draft.q ?? ''} onChange={event => setDraft(current => ({ ...current, q: event.target.value }))} placeholder="Örn. yapay zeka editörü" className="pl-9" />
              </div>
            </label>

            <label className="block space-y-2 text-sm font-semibold">
              Sıralama
              <select value={draft.sort} onChange={event => setDraft(current => ({ ...current, sort: event.target.value as ProductSort }))} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm">
                {sortOptions.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
            </label>

            <div className="space-y-2">
              <span className="text-sm font-semibold">Kategoriler</span>
              <div className="flex max-h-40 flex-wrap gap-2 overflow-y-auto pr-1">
                {topics.map(topic => (
                  <button key={topic.id} type="button" onClick={() => toggleTopic(topic.slug)} className={cn('rounded-full border px-2.5 py-1 text-xs font-semibold transition-colors', draft.topics?.includes(topic.slug) ? 'border-primary bg-primary text-primary-foreground' : 'border-border hover:border-primary/50')}>
                    {topic.name}
                  </button>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-3 gap-2">
              {([
                ['minUpvotes', 'Min. oy'],
                ['minComments', 'Min. yorum'],
                ['minViews', 'Min. görüntü'],
              ] as const).map(([key, label]) => (
                <label key={key} className="space-y-2 text-xs font-semibold text-muted-foreground">
                  {label}
                  <Input type="number" min={0} value={draft[key] ?? ''} onChange={event => setMinimum(key, event.target.value)} className="px-2" />
                </label>
              ))}
            </div>

            <div className="grid grid-cols-2 gap-2">
              <label className="space-y-2 text-xs font-semibold text-muted-foreground">Başlangıç<Input type="date" value={draft.publishedFrom ?? ''} onChange={event => setDraft(current => ({ ...current, publishedFrom: event.target.value || undefined }))} /></label>
              <label className="space-y-2 text-xs font-semibold text-muted-foreground">Bitiş<Input type="date" value={draft.publishedTo ?? ''} onChange={event => setDraft(current => ({ ...current, publishedTo: event.target.value || undefined }))} /></label>
            </div>

            <div className="flex gap-2">
              <Button type="button" onClick={commitFilters} className="flex-1"><Filter className="mr-2 h-4 w-4" /> Sonuçları göster</Button>
              <Button type="button" variant="outline" size="icon" onClick={resetFilters} aria-label="Filtreleri temizle"><RotateCcw className="h-4 w-4" /></Button>
            </div>
          </div>
        </form>

        <section>
          <div className="mb-4 flex items-end justify-between gap-4 px-1">
            <div><h2 className="text-2xl font-extrabold">Keşif sonuçları</h2><p className="text-sm text-muted-foreground">Cursor ile hızlı ve kararlı sayfalama</p></div>
            {!isLoading && <span className="text-sm font-semibold text-muted-foreground">{products.length} ürün gösteriliyor</span>}
          </div>

          {isLoading ? (
            <div className="flex min-h-80 items-center justify-center rounded-3xl border border-border bg-card"><Loader2 className="h-8 w-8 animate-spin text-primary" /></div>
          ) : error ? (
            <div className="rounded-3xl border border-destructive/30 bg-destructive/5 p-10 text-center text-destructive">{error}</div>
          ) : products.length === 0 ? (
            <div className="rounded-3xl border border-dashed border-border bg-muted/20 p-14 text-center"><Search className="mx-auto mb-4 h-10 w-10 text-muted-foreground" /><h3 className="text-xl font-bold">Bu filtrelerle ürün bulunamadı</h3><p className="mt-2 text-sm text-muted-foreground">Bir eşiği azaltmayı veya daha geniş tarih aralığı seçmeyi deneyin.</p></div>
          ) : (
            <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3">
              <div className="divide-y divide-border/60">{products.map(product => <ProductRow key={product.id} product={product} />)}</div>
              {hasMore && <div className="border-t border-border/60 p-4 text-center"><Button variant="outline" disabled={isLoadingMore} onClick={() => nextCursor && void fetchPage(nextCursor)}>{isLoadingMore ? <><Loader2 className="mr-2 h-4 w-4 animate-spin" />Yükleniyor</> : 'Daha fazla ürün yükle'}</Button></div>}
            </div>
          )}
        </section>
      </div>
    </main>
  )
}
