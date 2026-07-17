'use client'

import Image from 'next/image'
import Link from 'next/link'
import { Suspense, useCallback, useEffect, useMemo, useState } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { Check, Eye, Flame, GitCompareArrows, Loader2, MessageCircle, Plus, Search, Trash2, Trophy } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { ProductApiModel } from '@/core/domain/product.types'
import { cn } from '@/lib/utils'

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

interface CompareResponse {
  items: ProductApiModel[]
  maxProducts: number
}

function CompareContent() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const ids = useMemo(() => (searchParams.get('ids') ?? '').split(',').filter(Boolean).slice(0, 4), [searchParams])
  const [products, setProducts] = useState<ProductApiModel[]>([])
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<ProductApiModel[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isSearching, setIsSearching] = useState(false)

  const updateIds = useCallback((nextIds: string[]) => {
    const params = new URLSearchParams(searchParams.toString())
    if (nextIds.length) params.set('ids', nextIds.join(','))
    else params.delete('ids')
    router.replace(`/compare${params.size ? `?${params.toString()}` : ''}`)
  }, [router, searchParams])

  useEffect(() => {
    if (!ids.length) return

    // The URL selection intentionally starts a new comparison request.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setIsLoading(true)
    fetch(`${apiUrl}/api/products/compare?ids=${encodeURIComponent(ids.join(','))}`)
      .then(response => response.ok ? response.json() as Promise<CompareResponse> : Promise.reject(new Error('Karşılaştırma yüklenemedi')))
      .then(data => setProducts(data.items))
      .catch(() => setProducts([]))
      .finally(() => setIsLoading(false))
  }, [ids])

  useEffect(() => {
    const term = query.trim()
    if (term.length < 2) return

    const timer = window.setTimeout(() => {
      setIsSearching(true)
      fetch(`${apiUrl}/api/products/search?q=${encodeURIComponent(term)}&limit=8`)
        .then(response => response.ok ? response.json() as Promise<ProductApiModel[]> : [])
        .then(data => setResults(data.filter(product => !ids.includes(product.id))))
        .catch(() => setResults([]))
        .finally(() => setIsSearching(false))
    }, 250)
    return () => window.clearTimeout(timer)
  }, [ids, query])

  const visibleProducts = ids.length ? products : []
  const visibleResults = query.trim().length >= 2 ? results : []

  const addProduct = (id: string) => {
    if (ids.includes(id) || ids.length >= 4) return
    updateIds([...ids, id])
    setQuery('')
    setResults([])
  }

  const max = (selector: (product: ProductApiModel) => number) => Math.max(0, ...visibleProducts.map(selector))
  const metrics = [
    { label: 'Topluluk oyu', icon: Trophy, value: (product: ProductApiModel) => product.upvotes ?? 0, suffix: '' },
    { label: 'Görüntülenme', icon: Eye, value: (product: ProductApiModel) => product.viewCount ?? 0, suffix: '' },
    { label: 'Yorum', icon: MessageCircle, value: (product: ProductApiModel) => product.commentCount ?? 0, suffix: '' },
    { label: 'Trend skoru', icon: Flame, value: (product: ProductApiModel) => product.trendScore ?? 0, suffix: '' },
  ]

  return (
    <main className="mx-auto min-h-screen w-full max-w-7xl px-4 py-8 sm:px-6 sm:py-12">
      <section className="mb-8 overflow-hidden rounded-[2rem] border border-border bg-gradient-to-br from-card via-card to-primary/5 p-7 sm:p-10">
        <div className="flex flex-col justify-between gap-6 lg:flex-row lg:items-end">
          <div className="max-w-2xl">
            <div className="mb-3 inline-flex items-center gap-2 rounded-full bg-primary/10 px-3 py-1 text-xs font-bold uppercase tracking-[0.18em] text-primary"><GitCompareArrows className="h-4 w-4" /> Ürün karşılaştırma</div>
            <h1 className="text-3xl font-black tracking-tight sm:text-5xl">Karar vermeden önce yan yana gör</h1>
            <p className="mt-3 text-muted-foreground">En fazla dört ürünü; etkileşim, trend, kategori ve yayın bilgileriyle tek ekranda karşılaştır.</p>
          </div>
          <span className="w-fit rounded-2xl border border-border bg-background/70 px-4 py-2 text-sm font-bold">{visibleProducts.length} / 4 ürün seçildi</span>
        </div>
      </section>

      {ids.length < 4 && (
        <section className="relative z-20 mb-8 rounded-3xl border border-border bg-card p-5 shadow-sm">
          <label className="mb-2 block text-sm font-bold">Karşılaştırmaya ürün ekle</label>
          <div className="relative">
            {isSearching ? <Loader2 className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-primary" /> : <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />}
            <Input value={query} onChange={event => setQuery(event.target.value)} placeholder="Ürün adı veya kısa açıklama ara..." className="h-12 pl-10" />
          </div>
          {visibleResults.length > 0 && (
            <div className="mt-3 grid gap-2 sm:grid-cols-2">
              {visibleResults.map(product => (
                <button key={product.id} type="button" onClick={() => addProduct(product.id)} className="flex items-center gap-3 rounded-2xl border border-border p-3 text-left transition-colors hover:border-primary/40 hover:bg-primary/5">
                  <Image src={product.thumbnailUrl || '/products/notai.png'} alt="" width={44} height={44} className="h-11 w-11 rounded-xl border border-border object-cover" />
                  <span className="min-w-0 flex-1"><span className="block truncate font-bold">{product.name}</span><span className="block truncate text-xs text-muted-foreground">{product.tagline}</span></span>
                  <Plus className="h-5 w-5 text-primary" />
                </button>
              ))}
            </div>
          )}
        </section>
      )}

      {isLoading ? (
        <div className="flex min-h-80 items-center justify-center"><Loader2 className="h-9 w-9 animate-spin text-primary" /></div>
      ) : visibleProducts.length === 0 ? (
        <div className="rounded-[2rem] border border-dashed border-border bg-muted/20 px-6 py-20 text-center"><GitCompareArrows className="mx-auto mb-5 h-14 w-14 text-muted-foreground/40" /><h2 className="text-2xl font-extrabold">İlk ürünü seçerek başla</h2><p className="mx-auto mt-2 max-w-lg text-muted-foreground">Arama kutusundan ürün ekleyebilir veya ürün listesindeki karşılaştır simgesini kullanabilirsin.</p></div>
      ) : (
        <div className="overflow-x-auto rounded-[2rem] border border-border bg-card shadow-sm">
          <div className="min-w-[760px]" style={{ gridTemplateColumns: `180px repeat(${visibleProducts.length}, minmax(180px, 1fr))` }}>
            <div className="grid border-b border-border bg-muted/20" style={{ gridTemplateColumns: `180px repeat(${visibleProducts.length}, minmax(180px, 1fr))` }}>
              <div className="p-5 text-sm font-bold text-muted-foreground">ÜRÜNLER</div>
              {visibleProducts.map(product => (
                <div key={product.id} className="relative border-l border-border p-5 text-center">
                  <Button variant="ghost" size="icon" onClick={() => updateIds(ids.filter(id => id !== product.id))} className="absolute right-2 top-2 h-8 w-8 text-muted-foreground hover:text-destructive" aria-label={`${product.name} ürününü çıkar`}><Trash2 className="h-4 w-4" /></Button>
                  <Image src={product.thumbnailUrl || '/products/notai.png'} alt={`${product.name} logosu`} width={72} height={72} className="mx-auto mb-3 h-18 w-18 rounded-2xl border border-border object-cover shadow-sm" />
                  <Link href={`/product/${product.slug}`} className="font-extrabold hover:text-primary">{product.name}</Link>
                  <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">{product.tagline}</p>
                </div>
              ))}
            </div>

            {metrics.map(metric => {
              const best = max(metric.value)
              return (
                <div key={metric.label} className="grid border-b border-border last:border-b-0" style={{ gridTemplateColumns: `180px repeat(${visibleProducts.length}, minmax(180px, 1fr))` }}>
                  <div className="flex items-center gap-2 p-5 text-sm font-semibold text-muted-foreground"><metric.icon className="h-4 w-4" />{metric.label}</div>
                  {visibleProducts.map(product => {
                    const value = metric.value(product)
                    return <div key={product.id} className={cn('flex items-center justify-center gap-2 border-l border-border p-5 text-xl font-black tabular-nums', visibleProducts.length > 1 && value === best && best > 0 && 'bg-emerald-500/5 text-emerald-600 dark:text-emerald-400')}>{value.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}{visibleProducts.length > 1 && value === best && best > 0 && <Check className="h-4 w-4" />}</div>
                  })}
                </div>
              )
            })}

            <div className="grid border-b border-border" style={{ gridTemplateColumns: `180px repeat(${visibleProducts.length}, minmax(180px, 1fr))` }}>
              <div className="p-5 text-sm font-semibold text-muted-foreground">Kategoriler</div>
              {visibleProducts.map(product => <div key={product.id} className="flex flex-wrap items-center justify-center gap-1.5 border-l border-border p-5">{product.topics?.length ? product.topics.map(topic => <span key={topic.id} className="rounded-full bg-secondary px-2 py-1 text-xs font-semibold">{topic.name}</span>) : <span className="text-sm text-muted-foreground">Etiket yok</span>}</div>)}
            </div>

            <div className="grid" style={{ gridTemplateColumns: `180px repeat(${visibleProducts.length}, minmax(180px, 1fr))` }}>
              <div className="p-5 text-sm font-semibold text-muted-foreground">Yayın tarihi</div>
              {visibleProducts.map(product => <div key={product.id} className="border-l border-border p-5 text-center text-sm font-semibold">{product.publishedAt ? new Date(product.publishedAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' }) : '—'}</div>)}
            </div>
          </div>
        </div>
      )}
    </main>
  )
}

export default function ComparePage() {
  return <Suspense fallback={<div className="flex min-h-[60vh] items-center justify-center"><Loader2 className="h-9 w-9 animate-spin text-primary" /></div>}><CompareContent /></Suspense>
}
