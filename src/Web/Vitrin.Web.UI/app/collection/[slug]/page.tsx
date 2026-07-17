'use client'

import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'next/navigation'
import { useSession } from 'next-auth/react'
import { Bookmark, Calendar, Globe2, Loader2, Lock, Settings2, Trash2, UserPlus, Users } from 'lucide-react'
import { ProductRow } from '@/components/product-row'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { useToast } from '@/components/ui/use-toast'
import type { Product, ProductApiModel } from '@/core/domain/product.types'
import { CollectionCollaboratorRole, CollectionVisibility, type CollectionDetail } from '@/core/domain/collection.types'
import { cn } from '@/lib/utils'

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

interface UserProfile { id: string; username: string; fullName?: string }

const visibilityMeta = {
  [CollectionVisibility.Private]: { label: 'Özel koleksiyon', icon: Lock, description: 'Yalnızca sen görebilirsin.' },
  [CollectionVisibility.Public]: { label: 'Herkese açık', icon: Globe2, description: 'Bağlantıya sahip herkes görebilir.' },
  [CollectionVisibility.Shared]: { label: 'Ortak koleksiyon', icon: Users, description: 'Yalnızca davet edilen üyeler görebilir.' },
}

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

export default function CollectionDetailPage() {
  const params = useParams()
  const slug = params.slug as string
  const { data: session } = useSession()
  const { toast } = useToast()
  const accessToken = session?.accessToken as string | undefined
  const [collection, setCollection] = useState<CollectionDetail | null>(null)
  const [products, setProducts] = useState<Product[]>([])
  const [profiles, setProfiles] = useState<Record<string, UserProfile>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [isManageOpen, setIsManageOpen] = useState(false)
  const [memberUsername, setMemberUsername] = useState('')
  const [memberRole, setMemberRole] = useState(CollectionCollaboratorRole.Editor)
  const [isSaving, setIsSaving] = useState(false)

  const fetchCollection = useCallback(async () => {
    if (!slug) return
    setIsLoading(true)
    try {
      const response = await fetch(`${apiUrl}/api/collections/by-slug/${slug}`, {
        headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined,
      })
      if (!response.ok) {
        setCollection(null)
        return
      }
      const data = await response.json() as CollectionDetail
      setCollection(data)
      setProducts((data.products ?? []).map((product, index) => mapProduct(product, index + 1)))

      const userIds = [data.userId, ...(data.collaborators ?? []).map(member => member.userId)]
      const entries = await Promise.all(userIds.map(async userId => {
        try {
          const profileResponse = await fetch(`${apiUrl}/api/auth/users/${userId}`)
          return [userId, profileResponse.ok ? await profileResponse.json() as UserProfile : null] as const
        } catch { return [userId, null] as const }
      }))
      setProfiles(Object.fromEntries(entries.filter((entry): entry is readonly [string, UserProfile] => entry[1] !== null)))
    } finally {
      setIsLoading(false)
    }
  }, [accessToken, slug])

  useEffect(() => {
    // Slug or session changes intentionally refresh access-controlled details.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void fetchCollection()
  }, [fetchCollection])

  const authHeaders = () => ({ Authorization: `Bearer ${accessToken}`, 'Content-Type': 'application/json' })

  const updateVisibility = async (visibility: CollectionVisibility) => {
    if (!collection || !accessToken) return
    setIsSaving(true)
    try {
      const response = await fetch(`${apiUrl}/api/collections/${collection.id}/visibility`, { method: 'PATCH', headers: authHeaders(), body: JSON.stringify({ visibility }) })
      if (!response.ok) throw new Error()
      setCollection(current => current ? { ...current, visibility } : current)
      toast({ title: 'Görünürlük güncellendi' })
    } catch { toast({ title: 'Görünürlük değiştirilemedi', variant: 'destructive' }) }
    finally { setIsSaving(false) }
  }

  const addCollaborator = async () => {
    const username = memberUsername.trim().replace(/^@/, '')
    if (!collection || !accessToken || !username) return
    setIsSaving(true)
    try {
      const profileResponse = await fetch(`${apiUrl}/api/auth/users/by-username/${encodeURIComponent(username)}`, { headers: { Authorization: `Bearer ${accessToken}` } })
      if (!profileResponse.ok) throw new Error('Kullanıcı bulunamadı')
      const profile = await profileResponse.json() as UserProfile
      const response = await fetch(`${apiUrl}/api/collections/${collection.id}/collaborators`, { method: 'POST', headers: authHeaders(), body: JSON.stringify({ userId: profile.id, role: memberRole }) })
      if (!response.ok) throw new Error('Üye eklenemedi')
      setMemberUsername('')
      await fetchCollection()
      toast({ title: 'Ortak eklendi', description: `@${profile.username} artık bu koleksiyonda.` })
    } catch (error) { toast({ title: error instanceof Error ? error.message : 'Ortak eklenemedi', variant: 'destructive' }) }
    finally { setIsSaving(false) }
  }

  const removeCollaborator = async (userId: string) => {
    if (!collection || !accessToken) return
    const response = await fetch(`${apiUrl}/api/collections/${collection.id}/collaborators/${userId}`, { method: 'DELETE', headers: { Authorization: `Bearer ${accessToken}` } })
    if (response.ok) await fetchCollection()
  }

  const removeProduct = async (productId: string) => {
    if (!collection || !accessToken) return
    const response = await fetch(`${apiUrl}/api/collections/${collection.id}/products/${productId}`, { method: 'DELETE', headers: { Authorization: `Bearer ${accessToken}` } })
    if (response.ok) {
      setProducts(current => current.filter(product => product.id !== productId).map((product, index) => ({ ...product, rank: index + 1 })))
      toast({ title: 'Ürün koleksiyondan çıkarıldı' })
    }
  }

  if (isLoading) return <div className="flex min-h-[60vh] flex-col items-center justify-center text-muted-foreground"><Loader2 className="mb-4 h-10 w-10 animate-spin text-primary" /><p>Koleksiyon yükleniyor...</p></div>

  if (!collection) return <div className="flex min-h-[60vh] flex-col items-center justify-center text-center"><Bookmark className="mb-4 h-16 w-16 opacity-20" /><h1 className="mb-2 text-3xl font-bold">Koleksiyona erişilemiyor</h1><p className="text-muted-foreground">Koleksiyon özel olabilir veya artık mevcut olmayabilir.</p></div>

  const meta = visibilityMeta[collection.visibility]
  const VisibilityIcon = meta.icon
  const owner = profiles[collection.userId]

  return (
    <main className="mx-auto min-h-screen w-full max-w-5xl px-4 py-8 sm:px-6 sm:py-12">
      <section className="relative mb-10 overflow-hidden rounded-[2rem] border border-border bg-card p-7 shadow-sm sm:p-10">
        <div className="absolute -left-20 -top-20 h-64 w-64 rounded-full bg-primary/10 blur-3xl" />
        <div className="relative flex flex-col items-start justify-between gap-6 sm:flex-row sm:items-end">
          <div className="max-w-3xl">
            <span className="mb-4 inline-flex items-center gap-2 rounded-full bg-primary/10 px-3 py-1 text-xs font-bold text-primary"><VisibilityIcon className="h-3.5 w-3.5" />{meta.label}</span>
            <h1 className="text-3xl font-black tracking-tight sm:text-5xl">{collection.name}</h1>
            {collection.description && <p className="mt-3 text-lg text-muted-foreground">{collection.description}</p>}
            <div className="mt-6 flex flex-wrap items-center gap-4 text-sm font-semibold text-muted-foreground">
              <span>@{owner?.username || collection.userId.substring(0, 8)}</span>
              <span className="flex items-center gap-1.5"><Calendar className="h-4 w-4" />{new Date(collection.createdAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })}</span>
              <span>{products.length} ürün</span><span>{collection.collaborators?.length ?? 0} ortak</span>
            </div>
          </div>
          {collection.isOwner && <Button variant="outline" onClick={() => setIsManageOpen(true)}><Settings2 className="mr-2 h-4 w-4" /> Koleksiyonu yönet</Button>}
        </div>
      </section>

      {products.length > 0 ? (
        <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3"><div className="divide-y divide-border/60">{products.map(product => <ProductRow key={product.id} product={product} onRemove={collection.canEdit ? removeProduct : undefined} />)}</div></div>
      ) : (
        <div className="rounded-3xl border border-dashed border-border bg-muted/20 py-20 text-center"><Bookmark className="mx-auto mb-4 h-12 w-12 opacity-20" /><h2 className="text-xl font-bold">Koleksiyon henüz boş</h2><p className="mt-2 text-sm text-muted-foreground">Ürün sayfasındaki yer imi butonuyla buraya ürün ekleyebilirsin.</p></div>
      )}

      <Dialog open={isManageOpen} onOpenChange={setIsManageOpen}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-xl">
          <DialogHeader><DialogTitle>Koleksiyonu yönet</DialogTitle><DialogDescription>Görünürlüğü ve birlikte çalışacağın kişileri düzenle.</DialogDescription></DialogHeader>
          <div className="space-y-6 py-2">
            <div><h3 className="mb-3 text-sm font-bold">Kimler görebilir?</h3><div className="grid gap-2 sm:grid-cols-3">{([CollectionVisibility.Private, CollectionVisibility.Public, CollectionVisibility.Shared] as const).map(value => { const item = visibilityMeta[value]; const Icon = item.icon; return <button key={value} type="button" disabled={isSaving} onClick={() => void updateVisibility(value)} className={cn('rounded-2xl border p-3 text-left transition-colors', collection.visibility === value ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'border-border hover:bg-muted')}><Icon className="mb-2 h-5 w-5 text-primary" /><span className="block text-sm font-bold">{item.label}</span><span className="mt-1 block text-[11px] text-muted-foreground">{item.description}</span></button> })}</div></div>

            <div className="border-t border-border pt-5"><h3 className="mb-3 flex items-center gap-2 text-sm font-bold"><UserPlus className="h-4 w-4 text-primary" /> Ortak ekle</h3><div className="flex gap-2"><Input value={memberUsername} onChange={event => setMemberUsername(event.target.value)} placeholder="@kullaniciadi" /><select value={memberRole} onChange={event => setMemberRole(Number(event.target.value) as CollectionCollaboratorRole)} className="rounded-md border border-input bg-background px-3 text-sm"><option value={CollectionCollaboratorRole.Editor}>Editör</option><option value={CollectionCollaboratorRole.Viewer}>Görüntüleyici</option></select><Button onClick={addCollaborator} disabled={isSaving || !memberUsername.trim()}>{isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserPlus className="h-4 w-4" />}</Button></div></div>

            <div className="space-y-2">{collection.collaborators?.map(member => { const profile = profiles[member.userId]; return <div key={member.userId} className="flex items-center gap-3 rounded-2xl border border-border p-3"><div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-xs font-black text-primary">{(profile?.username || member.userId).slice(0, 2).toUpperCase()}</div><div className="min-w-0 flex-1"><p className="truncate text-sm font-bold">{profile?.fullName || profile?.username || member.userId}</p><p className="text-xs text-muted-foreground">{member.role === CollectionCollaboratorRole.Editor ? 'Editör · ürün ekleyebilir' : 'Görüntüleyici'}</p></div><Button variant="ghost" size="icon" onClick={() => void removeCollaborator(member.userId)} className="text-muted-foreground hover:text-destructive"><Trash2 className="h-4 w-4" /></Button></div> })}</div>
          </div>
        </DialogContent>
      </Dialog>
    </main>
  )
}
