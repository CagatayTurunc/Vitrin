"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import { CheckCircle2, ExternalLink, Loader2, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
interface CurationItem { id: string; name: string; slug: string; tagline: string; makerId: string; status: number; createdAt: string; scheduledLaunchAt?: string | null; completenessScore: number; signals: string[]; categories: string[]; launch?: { id: string; versionLabel: string; tagline: string; status: number; scheduledAtUtc?: string | null; isFeatured: boolean } | null }

export default function AdminCurationPage() {
  const { data: session } = useSession();
  const [items, setItems] = useState<CurationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const accessToken = session?.accessToken;
  useEffect(() => {
    if (!accessToken) return;
    let active = true;
    void fetch(`${API_URL}/api/products/admin/curation`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    })
      .then(async (response) => {
        if (!response.ok) throw new Error("Curation kuyruğu alınamadı.");
        return await response.json() as CurationItem[];
      })
      .then((data) => { if (active) setItems(data); })
      .catch((caught) => { if (active) setError(caught instanceof Error ? caught.message : "Veri alınamadı."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [accessToken]);

  const approve = async (id: string) => {
    if (!session?.accessToken) return;
    const response = await fetch(`${API_URL}/api/products/admin/${id}/approve`, { method: "POST", headers: { Authorization: `Bearer ${session.accessToken}` } });
    if (response.ok) setItems((current) => current.filter((item) => item.id !== id));
    else setError("Ürün onaylanamadı.");
  };
  const toggleFeatured = async (item: CurationItem) => {
    if (!session?.accessToken || !item.launch) return;
    const response = await fetch(`${API_URL}/api/products/admin/launches/${item.launch.id}/feature`, { method: "PATCH", headers: { Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json" }, body: JSON.stringify({ featured: !item.launch.isFeatured }) });
    if (response.ok) setItems((current) => current.map((row) => row.id === item.id && row.launch ? { ...row, launch: { ...row.launch, isFeatured: !row.launch.isFeatured } } : row));
  };

  return <div className="space-y-7"><div><h1 className="text-3xl font-bold">Curation kuyruğu</h1><p className="mt-1 text-muted-foreground">İnceleme kalitesi, lansman planı ve editör seçimini tek ekrandan yönet.</p></div>{error && <div className="rounded-xl border border-destructive/20 bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}{loading ? <div className="flex min-h-64 items-center justify-center"><Loader2 className="h-7 w-7 animate-spin" /></div> : items.length === 0 ? <div className="rounded-3xl border border-dashed p-14 text-center"><CheckCircle2 className="mx-auto h-9 w-9 text-emerald-600" /><h2 className="mt-3 text-xl font-bold">Kuyruk temiz</h2><p className="mt-1 text-muted-foreground">İnceleme veya planlama bekleyen ürün yok.</p></div> : <div className="space-y-4">{items.map((item) => <article key={item.id} className="rounded-3xl border bg-card p-5 shadow-sm"><div className="flex flex-col gap-5 xl:flex-row xl:items-center"><div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><h2 className="text-xl font-bold">{item.name}</h2>{item.launch?.isFeatured && <span className="rounded-full bg-amber-500/10 px-2 py-1 text-xs font-bold text-amber-700">Editör seçimi</span>}<span className="rounded-full bg-muted px-2 py-1 text-xs">{item.launch?.versionLabel ?? "Lansman yok"}</span></div><p className="mt-1 text-sm text-muted-foreground">{item.launch?.tagline || item.tagline}</p><div className="mt-3 flex flex-wrap gap-2">{item.categories.map((category) => <span key={category} className="rounded-full bg-violet-500/10 px-2.5 py-1 text-xs text-violet-700">{category}</span>)}{item.signals.map((signal) => <span key={signal} className="rounded-full bg-rose-500/10 px-2.5 py-1 text-xs text-rose-700">{signal}</span>)}</div><p className="mt-3 text-xs text-muted-foreground">Plan: {item.scheduledLaunchAt ? new Date(item.scheduledLaunchAt).toLocaleString("tr-TR") : "Onay sonrası"}</p></div><div className="w-full xl:w-40"><div className="mb-1 flex justify-between text-xs font-semibold"><span>Tamlık</span><span>%{item.completenessScore}</span></div><div className="h-2 overflow-hidden rounded-full bg-muted"><div className="h-full rounded-full bg-emerald-500" style={{ width: `${item.completenessScore}%` }} /></div></div><div className="flex flex-wrap gap-2"><Button asChild variant="outline"><Link href={`/product/${item.slug}`} target="_blank"><ExternalLink className="mr-2 h-4 w-4" />Önizle</Link></Button>{item.launch && <Button variant="outline" onClick={() => void toggleFeatured(item)}><Sparkles className="mr-2 h-4 w-4" />{item.launch.isFeatured ? "Seçimi kaldır" : "Editör seçimi"}</Button>}{item.status === 1 && <Button onClick={() => void approve(item.id)} className="bg-emerald-600 text-white hover:bg-emerald-700">Onayla</Button>}<Button asChild variant="destructive"><Link href="/admin/products">Reddet / detay</Link></Button></div></div></article>)}</div>}</div>;
}
