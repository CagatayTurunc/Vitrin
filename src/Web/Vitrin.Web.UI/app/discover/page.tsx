'use client'

import { FormEvent, useCallback, useEffect, useState } from 'react'
import { Bell, BellOff, BookmarkPlus, Filter, Loader2, Play, RotateCcw, Search, SlidersHorizontal, Sparkles, ThumbsDown, Trash2, X } from 'lucide-react'
import { useSession } from 'next-auth/react'
import { ProductRow } from '@/components/product-row'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ProductRepository } from '@/core/infrastructure/product.repository'
import type { Product, ProductApiModel, ProductFilters, ProductSort, SavedSearch, Topic } from '@/core/domain/product.types'
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

type DiscoveryMode = 'search' | 'for-you' | 'following' | 'undiscovered'
interface Recommendation { product: ProductApiModel; reason: string; score: number }

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
  const { data: session } = useSession()
  const accessToken = session?.accessToken
  const [topics, setTopics] = useState<Topic[]>([])
  const [followedTopicIds, setFollowedTopicIds] = useState<string[]>([])
  const [savedSearches, setSavedSearches] = useState<SavedSearch[]>([])
  const [savedSearchName, setSavedSearchName] = useState('')
  const [notifyOnNewMatches, setNotifyOnNewMatches] = useState(true)
  const [isSavingSearch, setIsSavingSearch] = useState(false)
  const [preferenceError, setPreferenceError] = useState<string | null>(null)
  const [draft, setDraft] = useState<ProductFilters>(emptyFilters)
  const [applied, setApplied] = useState<ProductFilters>(emptyFilters)
  const [products, setProducts] = useState<Product[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [hasMore, setHasMore] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingMore, setIsLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [discoveryMode, setDiscoveryMode] = useState<DiscoveryMode>('search')
  const [recommendations, setRecommendations] = useState<Recommendation[]>([])
  const [isPersonalizedLoading, setIsPersonalizedLoading] = useState(false)

  useEffect(() => {
    void ProductRepository.getTopics().then(setTopics).catch(() => setTopics([]))
  }, [])

  useEffect(() => {
    if (!accessToken) {
      return
    }

    void Promise.all([
      ProductRepository.getFollowedTopics(accessToken),
      ProductRepository.getSavedSearches(accessToken),
    ]).then(([followed, searches]) => {
      setFollowedTopicIds(followed.map(topic => topic.id))
      setSavedSearches(searches)
    }).catch(error => setPreferenceError(getErrorMessage(error, 'Kayıtlı keşif tercihleri alınamadı.')))
  }, [accessToken])

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

  useEffect(() => {
    if (!accessToken || discoveryMode === 'search') return
    const controller = new AbortController()
    // Changing discovery mode intentionally starts a personalized request.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setIsPersonalizedLoading(true)
    void fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/discover/personalized?mode=${discoveryMode}`, {
      headers: { Authorization: `Bearer ${accessToken}` }, signal: controller.signal,
    }).then(async response => response.ok ? await response.json() as Recommendation[] : [])
      .then(setRecommendations).finally(() => setIsPersonalizedLoading(false))
    return () => controller.abort()
  }, [accessToken, discoveryMode])

  const sendDiscoverySignal = async (productId: string, kind: 0 | 1) => {
    if (!accessToken) return
    setRecommendations(current => current.filter(item => item.product.id !== productId))
    await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/discover/products/${productId}/signal`, {
      method: 'PUT', headers: { Authorization: `Bearer ${accessToken}`, 'Content-Type': 'application/json' }, body: JSON.stringify({ kind }),
    })
  }

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

  const toggleTopicFollow = async (topic: Topic) => {
    if (!accessToken) {
      setPreferenceError('Topic takip etmek için giriş yapmalısın.')
      return
    }
    const isFollowing = followedTopicIds.includes(topic.id)
    setFollowedTopicIds(current => isFollowing
      ? current.filter(id => id !== topic.id)
      : [...current, topic.id])
    try {
      await ProductRepository.setTopicFollow(topic.id, !isFollowing, accessToken)
      setPreferenceError(null)
    } catch (requestError) {
      setFollowedTopicIds(current => isFollowing
        ? [...current, topic.id]
        : current.filter(id => id !== topic.id))
      setPreferenceError(getErrorMessage(requestError, 'Topic takibi güncellenemedi.'))
    }
  }

  const saveCurrentSearch = async () => {
    if (!accessToken) {
      setPreferenceError('Arama kaydetmek için giriş yapmalısın.')
      return
    }
    if (savedSearchName.trim().length < 2) {
      setPreferenceError('Kaydedilen arama için en az 2 karakterlik bir ad yaz.')
      return
    }
    setIsSavingSearch(true)
    try {
      const saved = await ProductRepository.saveSearch(savedSearchName.trim(), applied, notifyOnNewMatches, accessToken)
      setSavedSearches(current => [saved, ...current])
      setSavedSearchName('')
      setPreferenceError(null)
    } catch (requestError) {
      setPreferenceError(getErrorMessage(requestError, 'Arama kaydedilemedi.'))
    } finally {
      setIsSavingSearch(false)
    }
  }

  const applySavedSearch = (search: SavedSearch) => {
    const filters: ProductFilters = {
      q: search.query ?? undefined,
      topics: search.topics,
      minUpvotes: search.minUpvotes ?? undefined,
      minComments: search.minComments ?? undefined,
      minViews: search.minViews ?? undefined,
      publishedFrom: search.publishedFrom?.slice(0, 10) ?? undefined,
      publishedTo: search.publishedTo?.slice(0, 10) ?? undefined,
      sort: search.sort,
      city: search.city ?? undefined,
      university: search.university ?? undefined,
      technopark: search.technopark ?? undefined,
    }
    setDraft(filters)
    setApplied(filters)
  }

  const deleteSavedSearch = async (savedSearchId: string) => {
    if (!accessToken) return
    const previous = savedSearches
    setSavedSearches(current => current.filter(search => search.id !== savedSearchId))
    try {
      await ProductRepository.deleteSavedSearch(savedSearchId, accessToken)
    } catch (requestError) {
      setSavedSearches(previous)
      setPreferenceError(getErrorMessage(requestError, 'Kayıtlı arama silinemedi.'))
    }
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
    applied.city,
    applied.university,
    applied.technopark,
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
                {topics.map(topic => {
                  const isFollowing = followedTopicIds.includes(topic.id)
                  return (
                    <div key={topic.id} className="inline-flex overflow-hidden rounded-full border border-border bg-background">
                      <button type="button" onClick={() => toggleTopic(topic.slug)} className={cn('px-2.5 py-1 text-xs font-semibold transition-colors', draft.topics?.includes(topic.slug) ? 'bg-primary text-primary-foreground' : 'hover:bg-muted')}>
                        {topic.name}
                      </button>
                      <button type="button" onClick={() => void toggleTopicFollow(topic)} title={isFollowing ? 'Takibi bırak' : 'Yeni ürünleri takip et'} className={cn('border-l border-border px-2 transition-colors', isFollowing ? 'bg-emerald-500/15 text-emerald-500' : 'text-muted-foreground hover:text-primary')}>
                        {isFollowing ? <Bell className="h-3.5 w-3.5" /> : <BellOff className="h-3.5 w-3.5" />}
                      </button>
                    </div>
                  )
                })}
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

            <div className="space-y-2">
              <span className="text-sm font-semibold">Lokasyon & Ekosistem</span>
              <div className="grid grid-cols-1 gap-2">
                <Input value={draft.city ?? ''} onChange={event => setDraft(current => ({ ...current, city: event.target.value || undefined }))} placeholder="Şehir (Örn: İstanbul)" className="h-9 text-sm" />
                <Input value={draft.university ?? ''} onChange={event => setDraft(current => ({ ...current, university: event.target.value || undefined }))} placeholder="Üniversite (Örn: İTÜ)" className="h-9 text-sm" />
                <Input value={draft.technopark ?? ''} onChange={event => setDraft(current => ({ ...current, technopark: event.target.value || undefined }))} placeholder="Teknopark / Kuluçka Merkezi" className="h-9 text-sm" />
              </div>
            </div>

            <div className="flex gap-2">
              <Button type="button" onClick={commitFilters} className="flex-1"><Filter className="mr-2 h-4 w-4" /> Sonuçları göster</Button>
              <Button type="button" variant="outline" size="icon" onClick={resetFilters} aria-label="Filtreleri temizle"><RotateCcw className="h-4 w-4" /></Button>
            </div>

            <div className="rounded-2xl border border-border bg-muted/20 p-3">
              <div className="mb-3 flex items-center gap-2 text-sm font-bold">
                <BookmarkPlus className="h-4 w-4 text-primary" /> Kayıtlı aramalar
              </div>
              {accessToken ? (
                <>
                  <div className="flex gap-2">
                    <Input value={savedSearchName} onChange={event => setSavedSearchName(event.target.value)} placeholder="Örn. AI araçları" className="h-9" />
                    <Button type="button" size="sm" onClick={() => void saveCurrentSearch()} disabled={isSavingSearch}>
                      {isSavingSearch ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Kaydet'}
                    </Button>
                  </div>
                  <label className="mt-2 flex items-center gap-2 text-xs text-muted-foreground cursor-pointer select-none">
                    <input
                      type="checkbox"
                      checked={notifyOnNewMatches}
                      onChange={event => setNotifyOnNewMatches(event.target.checked)}
                      className="h-3.5 w-3.5 accent-primary"
                    />
                    Yeni eşleşmelerde bildirim al
                  </label>
                  <div className="mt-3 space-y-2">
                    {savedSearches.length === 0 && <p className="text-xs text-muted-foreground">Uyguladığın filtreleri kaydedip tek tıkla yeniden açabilirsin.</p>}
                    {savedSearches.map(search => (
                      <div key={search.id} className="flex items-center gap-1 rounded-xl border border-border bg-background p-1.5">
                        <button type="button" onClick={() => applySavedSearch(search)} className="flex min-w-0 flex-1 items-center gap-2 rounded-lg px-2 py-1.5 text-left text-xs font-semibold hover:bg-muted">
                          <Play className="h-3 w-3 shrink-0 text-primary" /><span className="truncate">{search.name}</span>
                        </button>
                        <button type="button" onClick={() => void deleteSavedSearch(search.id)} aria-label={`${search.name} aramasını sil`} className="rounded-lg p-1.5 text-muted-foreground hover:bg-destructive/10 hover:text-destructive">
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      </div>
                    ))}
                  </div>
                </>
              ) : <p className="text-xs text-muted-foreground">Arama kaydetmek ve topic takip etmek için giriş yap.</p>}
              {preferenceError && <p className="mt-2 text-xs text-destructive">{preferenceError}</p>}
            </div>
          </div>
        </form>

        <section>
          {accessToken && <div className="mb-5 flex gap-2 overflow-x-auto rounded-2xl border bg-card p-1.5">
            {([['search', 'Tüm ürünler'], ['for-you', 'Senin için'], ['following', 'Takip ettiklerin'], ['undiscovered', 'Az keşfedilenler']] as const).map(([value, label]) => <button key={value} onClick={() => setDiscoveryMode(value)} className={cn('min-w-fit flex-1 rounded-xl px-4 py-2.5 text-sm font-bold transition', discoveryMode === value ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted')}>{label}</button>)}
          </div>}
          <div className="mb-4 flex items-end justify-between gap-4 px-1">
            <div><h2 className="text-2xl font-extrabold">{discoveryMode === 'search' ? 'Keşif sonuçları' : discoveryMode === 'for-you' ? 'Senin için seçildi' : discoveryMode === 'following' ? 'Takip ettiklerinden' : 'Yeni ve az keşfedilmiş'}</h2><p className="text-sm text-muted-foreground">{discoveryMode === 'search' ? 'Cursor ile hızlı ve kararlı sayfalama' : 'Etkileşimlerin ve takiplerinle şekillenen açıklanabilir öneriler'}</p></div>
            {discoveryMode === 'search' && !isLoading && <span className="text-sm font-semibold text-muted-foreground">{products.length} ürün gösteriliyor</span>}
          </div>

          {discoveryMode === 'for-you' && (
            <div className="mb-6 rounded-2xl border border-primary/20 bg-primary/5 p-4 text-sm text-primary">
              <span className="font-bold">Onboarding hedefinize uygun:</span> Seçtiğiniz "Yeni ürünler bulmak" ve "İş birlikleri kurmak" hedeflerine göre küratör onaylı tavsiyeler.
            </div>
          )}

          {discoveryMode !== 'search' && !isPersonalizedLoading && (
            <div className="mb-8">
              <h3 className="mb-3 px-1 text-lg font-bold">Takip Edilebilecek Maker ve Küratörler</h3>
              <div className="flex gap-4 overflow-x-auto pb-2">
                {[
                  { name: 'Kaan Demir', role: 'Top Maker', user: 'kaandemir', img: '/products/notai.png' },
                  { name: 'Zeynep Kaya', role: 'Curator', user: 'zeynepk', img: '/products/notai.png' },
                  { name: 'Ozan Yılmaz', role: 'Hunter', user: 'ozany', img: '/products/notai.png' }
                ].map((maker) => (
                  <div key={maker.user} className="flex min-w-[200px] flex-col items-center gap-2 rounded-2xl border bg-card p-4 text-center shadow-sm">
                    <img src={maker.img} alt={maker.name} className="h-12 w-12 rounded-full object-cover" />
                    <div>
                      <div className="font-bold">{maker.name}</div>
                      <div className="text-xs text-muted-foreground">@{maker.user} • {maker.role}</div>
                    </div>
                    <Button variant="outline" size="sm" className="w-full mt-2 h-8 text-xs">Takip Et</Button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {discoveryMode !== 'search' ? (isPersonalizedLoading ? <div className="flex min-h-80 items-center justify-center rounded-3xl border bg-card"><Loader2 className="h-8 w-8 animate-spin text-primary" /></div> : recommendations.length === 0 ? <div className="rounded-3xl border border-dashed p-14 text-center text-muted-foreground">Bu akış için henüz yeterli eşleşme yok. Topic ve ürün takip ettikçe öneriler gelişir.</div> : <div className="rounded-3xl border bg-card p-2 shadow-sm sm:p-3"><div className="divide-y divide-border/60">{recommendations.map((item, index) => <div key={item.product.id}><div className="flex items-center justify-between gap-3 px-4 pt-3 text-xs"><span className="rounded-full bg-primary/10 px-2.5 py-1 font-bold text-primary"><Sparkles className="mr-1 inline h-3 w-3" />{item.reason}</span><div className="flex gap-1"><button onClick={() => void sendDiscoverySignal(item.product.id, 1)} className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted" title="İlgilenmiyorum"><ThumbsDown className="h-3.5 w-3.5" /></button><button onClick={() => void sendDiscoverySignal(item.product.id, 0)} className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted" title="Öneriyi gizle"><X className="h-3.5 w-3.5" /></button></div></div><ProductRow product={mapProduct(item.product, index + 1)} /></div>)}</div></div>) : isLoading ? (
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
