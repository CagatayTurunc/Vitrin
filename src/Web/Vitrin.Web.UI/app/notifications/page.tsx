"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useSession } from "next-auth/react";
import { Bell, CheckCheck, Filter, Loader2, MessageCircle, Package, Settings2, UserPlus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
interface NotificationItem { id: string; message: string; isRead: boolean; createdAt: string; notificationType?: string | null; relatedEntityId?: string | null; actionUrl: string; }
const filters = [{ value: "all", label: "Tümü" }, { value: "product", label: "Ürün" }, { value: "comment", label: "Yorum" }, { value: "social", label: "Sosyal" }, { value: "maker", label: "Maker" }] as const;
function iconFor(type?: string | null) { if (type?.startsWith("comment")) return MessageCircle; if (type?.startsWith("product")) return Package; if (type === "follow" || type?.startsWith("social")) return UserPlus; return Bell; }

export default function NotificationsPage() {
  const { data: session, status } = useSession(); const accessToken = session?.accessToken as string | undefined;
  const [items, setItems] = useState<NotificationItem[]>([]); const [filter, setFilter] = useState("all"); const [unreadOnly, setUnreadOnly] = useState(false); const [loading, setLoading] = useState(true);
  const load = useCallback(async () => { if (!accessToken) return; setLoading(true); try { const params = new URLSearchParams({ type: filter, take: "250" }); if (unreadOnly) params.set("unread", "true"); const response = await fetch(`${API_URL}/api/notifications/me?${params}`, { headers: { Authorization: `Bearer ${accessToken}` }, cache: "no-store" }); setItems(response.ok ? await response.json() : []); } finally { setLoading(false); } }, [accessToken, filter, unreadOnly]);
  useEffect(() => { if (!accessToken) return; /* eslint-disable-next-line react-hooks/set-state-in-effect */ void load(); }, [accessToken, load]);
  const grouped = useMemo(() => Object.entries(items.reduce<Record<string, NotificationItem[]>>((groups, item) => { const date = new Date(item.createdAt); const today = new Date(); const key = date.toDateString() === today.toDateString() ? "Bugün" : date.toLocaleDateString("tr-TR", { day: "numeric", month: "long", year: "numeric" }); (groups[key] ??= []).push(item); return groups; }, {})), [items]);
  const markRead = async (item: NotificationItem) => { if (!accessToken || item.isRead) return; setItems(current => current.map(value => value.id === item.id ? { ...value, isRead: true } : value)); await fetch(`${API_URL}/api/notifications/${item.id}/read`, { method: "POST", headers: { Authorization: `Bearer ${accessToken}` } }); };
  const markAll = async () => { if (!accessToken) return; await fetch(`${API_URL}/api/notifications/read-all`, { method: "POST", headers: { Authorization: `Bearer ${accessToken}` } }); setItems(current => current.map(item => ({ ...item, isRead: true }))); };
  if (status === "loading") return <div className="flex min-h-[60vh] items-center justify-center"><Loader2 className="h-8 w-8 animate-spin" /></div>;
  if (!accessToken) return <div className="py-24 text-center"><h1 className="text-2xl font-black">Bildirimlerini görmek için giriş yap.</h1></div>;
  return <main className="mx-auto min-h-screen w-full max-w-4xl px-4 py-10 sm:px-6"><header className="mb-7 flex flex-col justify-between gap-4 sm:flex-row sm:items-end"><div><div className="mb-3 inline-flex rounded-2xl bg-primary/10 p-3"><Bell className="h-7 w-7 text-primary" /></div><h1 className="text-4xl font-black">Bildirim merkezi</h1><p className="mt-2 text-muted-foreground">Ürün, yorum, takip ve maker gelişmelerinin tamamı.</p></div><div className="flex gap-2"><Button variant="outline" onClick={markAll}><CheckCheck /> Tümünü okundu yap</Button><Link href="/settings"><Button variant="outline" size="icon"><Settings2 /></Button></Link></div></header>
    <div className="mb-6 rounded-2xl border border-primary/20 bg-primary/5 p-5 flex flex-col sm:flex-row gap-4 items-center justify-between">
      <div>
        <h3 className="font-bold text-primary">Haftalık Özetiniz Hazır</h3>
        <p className="text-sm text-muted-foreground">Bu hafta takip ettiğiniz ürünlerde 12 yeni gelişme oldu. 3 ürün yeni sürüme geçti.</p>
      </div>
      <Button variant="default" size="sm">Özeti İncele</Button>
    </div>
    <div className="mb-6 flex flex-col gap-3 rounded-2xl border bg-card p-2 sm:flex-row sm:items-center"><div className="flex flex-1 gap-1 overflow-x-auto">{filters.map(item => <button key={item.value} onClick={() => setFilter(item.value)} className={cn("min-w-fit rounded-xl px-4 py-2 text-sm font-bold", filter === item.value ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-muted")}>{item.label}</button>)}</div><button onClick={() => setUnreadOnly(value => !value)} className={cn("flex items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-bold", unreadOnly ? "bg-amber-500/10 text-amber-600" : "text-muted-foreground hover:bg-muted")}><Filter className="h-4 w-4" /> Yalnızca okunmamış</button></div>
    {loading ? <div className="flex justify-center py-24"><Loader2 className="h-8 w-8 animate-spin text-primary" /></div> : grouped.length === 0 ? <div className="rounded-3xl border border-dashed py-20 text-center text-muted-foreground">Bu filtrede bildirim bulunmuyor.</div> : <div className="space-y-7">{grouped.map(([date, notifications]) => <section key={date}><h2 className="mb-2 px-2 text-xs font-black uppercase tracking-widest text-muted-foreground">{date}</h2><div className="divide-y overflow-hidden rounded-2xl border bg-card">{notifications.map(item => { const Icon = iconFor(item.notificationType); return <Link href={item.actionUrl || "/notifications"} key={item.id} onClick={() => void markRead(item)} className={cn("flex items-start gap-4 p-5 transition hover:bg-muted/40", !item.isRead && "bg-primary/5")}><span className="rounded-xl bg-muted p-2.5"><Icon className="h-5 w-5 text-primary" /></span><span className="min-w-0 flex-1"><span className={cn("block text-sm leading-6", !item.isRead && "font-bold")}>{item.message}</span><span className="mt-1 block text-xs text-muted-foreground">{new Date(item.createdAt).toLocaleString("tr-TR")}</span></span>{!item.isRead && <span className="mt-2 h-2.5 w-2.5 rounded-full bg-primary" />}</Link>; })}</div></section>)}</div>}
  </main>;
}
