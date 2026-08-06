"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useSession } from "next-auth/react";
import { Clock, Eye, Loader2, MessageCircle, Pin, Plus, ThumbsUp, Users } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { CommunityThreadCategory, CommunityThreadKind, communityCategoryLabels, type CommunityThread } from "@/core/domain/community.types";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export function CommunityFeed({ productId, productName }: { productId?: string; productName?: string }) {
  const { data: session } = useSession();
  const accessToken = session?.accessToken as string | undefined;
  const [items, setItems] = useState<CommunityThread[]>([]);
  const [category, setCategory] = useState<CommunityThreadCategory | "all">("all");
  const [sort, setSort] = useState("new");
  const [loading, setLoading] = useState(true);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [draft, setDraft] = useState({ title: "", body: "", category: productId ? CommunityThreadCategory.Support : CommunityThreadCategory.General, kind: CommunityThreadKind.Discussion });

  const query = useMemo(() => {
    const params = new URLSearchParams({ sort });
    if (productId) params.set("productId", productId);
    if (category !== "all") params.set("category", String(category));
    return params.toString();
  }, [category, productId, sort]);

  useEffect(() => {
    const controller = new AbortController();
    // The query change intentionally starts a new remote loading cycle.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setLoading(true);
    void fetch(`${API_URL}/api/community/threads?${query}`, { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined, signal: controller.signal })
      .then(response => response.ok ? response.json() as Promise<CommunityThread[]> : [])
      .then(setItems)
      .finally(() => setLoading(false));
    return () => controller.abort();
  }, [accessToken, query]);

  const createThread = async () => {
    if (!accessToken || !draft.title.trim() || !draft.body.trim()) return;
    setSaving(true);
    try {
      const response = await fetch(`${API_URL}/api/community/threads`, {
        method: "POST", headers: { Authorization: `Bearer ${accessToken}`, "Content-Type": "application/json" },
        body: JSON.stringify({ ...draft, productId }),
      });
      if (!response.ok) throw new Error("Konu oluşturulamadı.");
      const result = await response.json() as { slug: string };
      window.location.href = `/discussions/${result.slug}`;
    } finally { setSaving(false); }
  };

  const categories = productId
    ? [CommunityThreadCategory.Support, CommunityThreadCategory.Feedback, CommunityThreadCategory.Changelog, CommunityThreadCategory.General]
    : [CommunityThreadCategory.General, CommunityThreadCategory.Maker, CommunityThreadCategory.Technical, CommunityThreadCategory.Feedback, CommunityThreadCategory.Collaboration];

  return <>
    <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex gap-2 overflow-x-auto">
        <Button size="sm" variant={category === "all" ? "default" : "outline"} onClick={() => setCategory("all")}>Tümü</Button>
        {categories.map(item => <Button key={item} size="sm" variant={category === item ? "default" : "outline"} onClick={() => setCategory(item)}>{communityCategoryLabels[item]}</Button>)}
      </div>
      <div className="flex gap-2">
        <select value={sort} onChange={event => setSort(event.target.value)} className="h-8 rounded-lg border border-input bg-background px-3 text-sm"><option value="new">Yeni</option><option value="trending">Trend</option><option value="top">En iyi</option></select>
        <Button size="sm" onClick={() => setOpen(true)} disabled={!accessToken}><Plus /> Konu aç</Button>
      </div>
    </div>

    {loading ? <div className="flex justify-center py-20"><Loader2 className="h-8 w-8 animate-spin text-primary" /></div> : items.length === 0 ?
      <div className="rounded-3xl border border-dashed py-20 text-center text-muted-foreground"><MessageCircle className="mx-auto mb-3 h-10 w-10 opacity-30" /><p>İlk konuyu açarak topluluğu başlat.</p></div> :
      <div className="space-y-3">{items.map(item => <Link key={item.id} href={`/discussions/${item.slug}`} className="group block rounded-2xl border border-border bg-card p-5 transition hover:border-primary/40 hover:shadow-sm">
        <div className="flex items-start gap-4"><div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10 font-black text-primary">{item.authorId.slice(0, 2).toUpperCase()}</div>
          <div className="min-w-0 flex-1"><div className="mb-1 flex flex-wrap items-center gap-2">{item.isPinned && <span className="flex items-center gap-1 text-xs font-bold text-amber-500"><Pin className="h-3 w-3" /> Sabit</span>}<span className="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-bold text-primary">{communityCategoryLabels[item.category]}</span></div>
            <h2 className="text-lg font-bold group-hover:text-primary">{item.title}</h2><p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{item.body}</p>
            <div className="mt-3 flex flex-wrap gap-4 text-xs text-muted-foreground"><span className="flex items-center gap-1"><Clock className="h-3 w-3" />{new Date(item.createdAtUtc).toLocaleDateString("tr-TR")}</span><span className="flex items-center gap-1"><MessageCircle className="h-3 w-3" />{item.replyCount}</span><span className="flex items-center gap-1"><Eye className="h-3 w-3" />{item.viewCount}</span><span className="flex items-center gap-1"><ThumbsUp className="h-3 w-3" />{item.reactionCount}</span><span className="flex items-center gap-1"><Users className="h-3 w-3" />{item.followerCount}</span></div>
          </div></div></Link>)}</div>}

    <Dialog open={open} onOpenChange={setOpen}><DialogContent><DialogHeader><DialogTitle>{productName ? `${productName} forumunda konu aç` : "Yeni tartışma başlat"}</DialogTitle><DialogDescription>Net bir başlık yaz; bağlamı ve beklediğin geri bildirimi içerikte anlat.</DialogDescription></DialogHeader>
      <div className="space-y-4"><Input value={draft.title} onChange={event => setDraft(current => ({ ...current, title: event.target.value }))} placeholder="Konu başlığı" maxLength={160} /><Textarea value={draft.body} onChange={event => setDraft(current => ({ ...current, body: event.target.value }))} placeholder="Toplulukla ne paylaşmak istiyorsun?" className="min-h-40" maxLength={20000} />
        <div className="grid grid-cols-2 gap-3"><select value={draft.category} onChange={event => setDraft(current => ({ ...current, category: Number(event.target.value) }))} className="h-10 rounded-lg border border-input bg-background px-3 text-sm">{categories.map(item => <option key={item} value={item}>{communityCategoryLabels[item]}</option>)}</select><select value={draft.kind} onChange={event => setDraft(current => ({ ...current, kind: Number(event.target.value) }))} className="h-10 rounded-lg border border-input bg-background px-3 text-sm"><option value={CommunityThreadKind.Discussion}>Tartışma</option><option value={CommunityThreadKind.Question}>Soru</option><option value={CommunityThreadKind.Feedback}>Geri bildirim</option><option value={CommunityThreadKind.Poll}>Anket</option><option value={CommunityThreadKind.Ama}>AMA</option><option value={CommunityThreadKind.BuildInPublic}>Build in public</option></select></div>
        <Button onClick={createThread} disabled={saving || draft.title.trim().length < 5 || draft.body.trim().length < 10} className="w-full">{saving && <Loader2 className="animate-spin" />} Konuyu yayınla</Button></div>
    </DialogContent></Dialog>
  </>;
}
