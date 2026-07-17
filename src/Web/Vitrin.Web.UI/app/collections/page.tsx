'use client'

import Link from 'next/link'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSession } from 'next-auth/react'
import { Bookmark, Globe2, LayoutGrid, Loader2, Lock, Plus, Users } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { useToast } from '@/components/ui/use-toast'
import { cn } from '@/lib/utils'
import { CollectionVisibility, type CollectionSummary } from '@/core/domain/collection.types'

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

const visibilityMeta = {
  [CollectionVisibility.Private]: { label: 'Özel', icon: Lock, className: 'bg-slate-500/10 text-slate-600 dark:text-slate-300' },
  [CollectionVisibility.Public]: { label: 'Herkese açık', icon: Globe2, className: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' },
  [CollectionVisibility.Shared]: { label: 'Ortak', icon: Users, className: 'bg-violet-500/10 text-violet-600 dark:text-violet-400' },
}

type CollectionTab = 'public' | 'mine' | 'shared'

export default function CollectionsPage() {
  const { data: session } = useSession()
  const { toast } = useToast()
  const [collections, setCollections] = useState<CollectionSummary[]>([])
  const [activeTab, setActiveTab] = useState<CollectionTab>('public')
  const [isLoading, setIsLoading] = useState(true)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [visibility, setVisibility] = useState(CollectionVisibility.Private)
  const accessToken = session?.accessToken as string | undefined

  const fetchCollections = useCallback(async () => {
    setIsLoading(true)
    try {
      const response = await fetch(`${apiUrl}/api/collections`, {
        headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined,
      })
      setCollections(response.ok ? await response.json() as CollectionSummary[] : [])
    } finally {
      setIsLoading(false)
    }
  }, [accessToken])

  useEffect(() => {
    // Session changes intentionally refresh the set of accessible collections.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void fetchCollections()
  }, [fetchCollections])

  const visibleCollections = useMemo(() => collections.filter(collection => {
    if (activeTab === 'mine') return collection.isOwner
    if (activeTab === 'shared') return !collection.isOwner && collection.visibility === CollectionVisibility.Shared
    return collection.visibility === CollectionVisibility.Public
  }), [activeTab, collections])

  const counts = {
    public: collections.filter(item => item.visibility === CollectionVisibility.Public).length,
    mine: collections.filter(item => item.isOwner).length,
    shared: collections.filter(item => !item.isOwner && item.visibility === CollectionVisibility.Shared).length,
  }

  const createCollection = async () => {
    if (!accessToken || !name.trim()) return
    setIsCreating(true)
    try {
      const response = await fetch(`${apiUrl}/api/collections`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${accessToken}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: name.trim(), description: description.trim(), visibility }),
      })
      if (!response.ok) throw new Error('Koleksiyon oluşturulamadı')
      setName('')
      setDescription('')
      setVisibility(CollectionVisibility.Private)
      setIsCreateOpen(false)
      setActiveTab('mine')
      await fetchCollections()
      toast({ title: 'Koleksiyon hazır', description: 'Görünürlük ayarınla birlikte oluşturuldu.' })
    } catch {
      toast({ title: 'Koleksiyon oluşturulamadı', variant: 'destructive' })
    } finally {
      setIsCreating(false)
    }
  }

  return (
    <main className="mx-auto min-h-screen w-full max-w-6xl px-4 py-8 sm:px-6 sm:py-12">
      <section className="relative mb-8 overflow-hidden rounded-[2rem] border border-border bg-card p-7 sm:p-10">
        <div className="absolute -right-20 -top-24 h-72 w-72 rounded-full bg-primary/10 blur-3xl" />
        <div className="relative flex flex-col justify-between gap-6 md:flex-row md:items-end">
          <div className="max-w-2xl">
            <div className="mb-4 inline-flex items-center justify-center rounded-2xl bg-primary/10 p-3"><LayoutGrid className="h-7 w-7 text-primary" /></div>
            <h1 className="text-3xl font-black tracking-tight sm:text-5xl">Koleksiyon alanın</h1>
            <p className="mt-3 text-muted-foreground">Kendine sakla, herkesle paylaş veya takım arkadaşlarınla birlikte düzenle.</p>
          </div>
          {accessToken && <Button onClick={() => setIsCreateOpen(true)} className="w-fit rounded-full"><Plus className="mr-2 h-4 w-4" /> Yeni koleksiyon</Button>}
        </div>
      </section>

      <div className="mb-6 flex gap-2 overflow-x-auto rounded-2xl border border-border bg-card p-1.5">
        {([
          ['public', 'Keşfet', Globe2],
          ['mine', 'Koleksiyonlarım', Lock],
          ['shared', 'Benimle paylaşılan', Users],
        ] as const).map(([value, label, Icon]) => (
          <button key={value} onClick={() => setActiveTab(value)} className={cn('flex min-w-fit flex-1 items-center justify-center gap-2 rounded-xl px-4 py-2.5 text-sm font-bold transition-colors', activeTab === value ? 'bg-primary text-primary-foreground shadow-sm' : 'text-muted-foreground hover:bg-muted')}>
            <Icon className="h-4 w-4" /> {label}<span className={cn('rounded-full px-1.5 py-0.5 text-[10px]', activeTab === value ? 'bg-primary-foreground/20' : 'bg-muted')}>{counts[value]}</span>
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="flex justify-center py-24"><Loader2 className="h-9 w-9 animate-spin text-primary" /></div>
      ) : visibleCollections.length > 0 ? (
        <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
          {visibleCollections.map(collection => {
            const meta = visibilityMeta[collection.visibility]
            const VisibilityIcon = meta.icon
            return (
              <Link key={collection.id} href={`/collection/${collection.slug}`} className="group block">
                <article className="relative flex h-full min-h-56 flex-col overflow-hidden rounded-3xl border border-border bg-card p-6 shadow-sm transition-all hover:-translate-y-1 hover:border-primary/30 hover:shadow-lg">
                  <Bookmark className="absolute -right-5 -top-5 h-24 w-24 text-primary opacity-[0.06] transition-transform group-hover:rotate-6 group-hover:scale-110" />
                  <div className="mb-5 flex items-center justify-between">
                    <span className={cn('inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-bold', meta.className)}><VisibilityIcon className="h-3.5 w-3.5" />{meta.label}</span>
                    {collection.isOwner && <span className="text-xs font-bold text-primary">Sahibi sensin</span>}
                  </div>
                  <h2 className="relative text-xl font-extrabold transition-colors group-hover:text-primary">{collection.name}</h2>
                  <p className="mt-2 line-clamp-2 flex-1 text-sm leading-relaxed text-muted-foreground">{collection.description || 'Bu koleksiyon için henüz açıklama eklenmemiş.'}</p>
                  <div className="mt-6 flex items-center justify-between border-t border-border pt-4 text-xs font-semibold text-muted-foreground">
                    <span>{collection.productCount} ürün</span>
                    <span className="flex items-center gap-1"><Users className="h-3.5 w-3.5" />{collection.collaboratorCount} ortak</span>
                  </div>
                </article>
              </Link>
            )
          })}
        </div>
      ) : (
        <div className="rounded-3xl border border-dashed border-border bg-muted/20 py-20 text-center"><Bookmark className="mx-auto mb-4 h-12 w-12 text-muted-foreground/30" /><h2 className="text-xl font-bold">Bu bölüm henüz boş</h2><p className="mt-2 text-sm text-muted-foreground">Yeni bir koleksiyon oluşturabilir veya herkese açık listeleri keşfedebilirsin.</p></div>
      )}

      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Yeni koleksiyon</DialogTitle><DialogDescription>Ürünlerini kimlerin görebileceğini baştan belirle.</DialogDescription></DialogHeader>
          <div className="space-y-4 py-2">
            <Input value={name} onChange={event => setName(event.target.value)} placeholder="Koleksiyon adı" maxLength={100} />
            <Textarea value={description} onChange={event => setDescription(event.target.value)} placeholder="Kısa bir açıklama" maxLength={500} />
            <div className="grid gap-2 sm:grid-cols-3">
              {([CollectionVisibility.Private, CollectionVisibility.Public, CollectionVisibility.Shared] as const).map(value => {
                const meta = visibilityMeta[value]
                const Icon = meta.icon
                return <button key={value} type="button" onClick={() => setVisibility(value)} className={cn('rounded-2xl border p-3 text-left transition-colors', visibility === value ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'border-border hover:bg-muted')}><Icon className="mb-2 h-5 w-5 text-primary" /><span className="block text-sm font-bold">{meta.label}</span><span className="mt-1 block text-[11px] text-muted-foreground">{value === CollectionVisibility.Private ? 'Yalnızca sen' : value === CollectionVisibility.Public ? 'Tüm ziyaretçiler' : 'Davet edilenler'}</span></button>
              })}
            </div>
            <Button onClick={createCollection} disabled={isCreating || !name.trim()} className="w-full">{isCreating && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}Koleksiyonu oluştur</Button>
          </div>
        </DialogContent>
      </Dialog>
    </main>
  )
}
