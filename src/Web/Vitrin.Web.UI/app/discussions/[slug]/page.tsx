"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useSession } from "next-auth/react";
import { Bell, BellOff, Loader2, MessageCircle, ShieldCheck, ThumbsUp } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { communityCategoryLabels, type CommunityThreadDetail } from "@/core/domain/community.types";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export default function DiscussionDetailPage() {
  const { slug } = useParams<{ slug: string }>(); const { data: session } = useSession();
  const accessToken = session?.accessToken as string | undefined;
  const [thread, setThread] = useState<CommunityThreadDetail | null>(); const [reply, setReply] = useState(""); const [saving, setSaving] = useState(false);
  const load = useCallback(async () => { const response = await fetch(`${API_URL}/api/community/threads/${slug}`, { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined, cache: "no-store" }); setThread(response.ok ? await response.json() as CommunityThreadDetail : null); }, [accessToken, slug]);
  useEffect(() => {
    // Slug/session changes intentionally refresh the remote thread.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);
  const sendReply = async () => { if (!accessToken || !thread || reply.trim().length < 2) return; setSaving(true); try { const response = await fetch(`${API_URL}/api/community/threads/${thread.id}/replies`, { method: "POST", headers: { Authorization: `Bearer ${accessToken}`, "Content-Type": "application/json" }, body: JSON.stringify({ body: reply }) }); if (response.ok) { setReply(""); await load(); } } finally { setSaving(false); } };
  const toggle = async (path: string, method = "PUT") => { if (!accessToken) return; await fetch(`${API_URL}${path}`, { method, headers: { Authorization: `Bearer ${accessToken}` } }); await load(); };
  if (thread === undefined) return <div className="flex min-h-[60vh] items-center justify-center"><Loader2 className="h-8 w-8 animate-spin" /></div>;
  if (!thread) return <div className="py-24 text-center">Konu bulunamadı.</div>;
  return <main className="mx-auto min-h-screen w-full max-w-4xl px-4 py-10 sm:px-6"><article className="rounded-[2rem] border bg-card p-7 sm:p-10"><div className="flex flex-wrap items-center gap-2"><span className="rounded-full bg-primary/10 px-3 py-1 text-xs font-bold text-primary">{communityCategoryLabels[thread.category]}</span>{thread.isLocked && <span className="text-xs font-bold text-amber-500">Yanıtlara kapalı</span>}</div><h1 className="mt-4 text-3xl font-black sm:text-5xl">{thread.title}</h1><p className="mt-3 text-sm text-muted-foreground">{new Date(thread.createdAtUtc).toLocaleString("tr-TR")} · {thread.viewCount} görüntüleme</p><div className="mt-8 whitespace-pre-wrap text-base leading-8">{thread.body}</div><div className="mt-8 flex gap-2 border-t pt-5"><Button variant={thread.isReacted ? "default" : "outline"} onClick={() => toggle(`/api/community/threads/${thread.id}/reaction`)} disabled={!accessToken}><ThumbsUp /> {thread.reactionCount}</Button><Button variant="outline" onClick={() => toggle(`/api/community/threads/${thread.id}/follow`, thread.isFollowing ? "DELETE" : "PUT")} disabled={!accessToken}>{thread.isFollowing ? <BellOff /> : <Bell />} {thread.isFollowing ? "Takipten çık" : "Takip et"}</Button></div></article>
    <section className="mt-8"><h2 className="mb-4 flex items-center gap-2 text-xl font-black"><MessageCircle className="h-5 w-5 text-primary" /> Yanıtlar ({thread.replies.length})</h2><div className="space-y-3">{thread.replies.map(item => <article key={item.id} className="rounded-2xl border bg-card p-5"><div className="mb-3 flex items-center justify-between"><span className="text-xs font-bold text-muted-foreground">{item.authorId.slice(0, 8)} · {new Date(item.createdAtUtc).toLocaleString("tr-TR")}</span>{item.isOfficial && <span className="flex items-center gap-1 rounded-full bg-emerald-500/10 px-2 py-1 text-xs font-bold text-emerald-600"><ShieldCheck className="h-3 w-3" /> Resmî maker cevabı</span>}</div><p className="whitespace-pre-wrap leading-7">{item.body}</p><Button variant="ghost" size="sm" className="mt-3" onClick={() => toggle(`/api/community/replies/${item.id}/reaction`)} disabled={!accessToken}><ThumbsUp className={item.isReacted ? "fill-current" : ""} /> {item.reactionCount}</Button></article>)}</div>
      {!thread.isLocked && <div className="mt-6 rounded-2xl border bg-card p-5"><Textarea value={reply} onChange={event => setReply(event.target.value)} placeholder={accessToken ? "Yanıtını yaz... @kullanici ile mention ekleyebilirsin." : "Yanıtlamak için giriş yap."} disabled={!accessToken} className="min-h-32" /><div className="mt-3 flex justify-end"><Button onClick={sendReply} disabled={!accessToken || saving || reply.trim().length < 2}>{saving && <Loader2 className="animate-spin" />} Yanıtla</Button></div></div>}
    </section></main>;
}
