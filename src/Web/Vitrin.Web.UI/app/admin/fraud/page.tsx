"use client";

import { useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import { AlertTriangle, CheckCircle2, Clock3, Loader2, ShieldCheck } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
interface FraudData { generatedAtUtc: string; windowHours: number; rules: { rapidVoter: string; productBurst: string; uniqueVoteConstraint: boolean }; summary: { rapidVoterCount: number; productBurstCount: number; totalSignals: number }; rapidVoters: Array<{ userId: string; voteCount: number; productCount: number; firstVoteAtUtc: string; lastVoteAtUtc: string }>; productBursts: Array<{ productId: string; voteCount: number; uniqueUserCount: number; firstVoteAtUtc: string; lastVoteAtUtc: string }> }

export default function AdminFraudPage() {
  const { data: session } = useSession();
  const [data, setData] = useState<FraudData | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    if (!session?.accessToken) return;
    let active = true;
    void fetch(`${API_URL}/api/votes/admin/fraud-signals?hours=24`, { headers: { Authorization: `Bearer ${session.accessToken}` } }).then(async (response) => { if (!response.ok) throw new Error("Fraud sinyalleri alınamadı."); const payload = await response.json() as FraudData; if (active) setData(payload); }).catch((caught) => { if (active) setError(caught instanceof Error ? caught.message : "Veri alınamadı."); });
    return () => { active = false; };
  }, [session?.accessToken]);

  if (!data && !error) return <div className="flex min-h-[50vh] items-center justify-center"><Loader2 className="h-7 w-7 animate-spin" /></div>;
  if (!data) return <div className="rounded-xl bg-destructive/10 p-4 text-destructive">{error}</div>;
  return <div className="space-y-7"><div><h1 className="text-3xl font-bold">Fraud görünümü</h1><p className="mt-1 text-muted-foreground">Oy hızını ve ürün bazlı ani yükselişleri izleyen açıklanabilir sinyaller.</p></div><div className="grid gap-4 md:grid-cols-3"><Metric title="Toplam sinyal" value={data.summary.totalSignals} icon={AlertTriangle} tone="rose" /><Metric title="Hızlı oy veren" value={data.summary.rapidVoterCount} icon={Clock3} tone="amber" /><Metric title="Ürün oy patlaması" value={data.summary.productBurstCount} icon={ShieldCheck} tone="emerald" /></div><Card><CardHeader><CardTitle>Aktif kurallar</CardTitle><CardDescription>Bu sinyaller otomatik ceza vermez; admin incelemesi için öncelik oluşturur.</CardDescription></CardHeader><CardContent className="grid gap-3 md:grid-cols-3"><Rule text={data.rules.rapidVoter} /><Rule text={data.rules.productBurst} /><Rule text={data.rules.uniqueVoteConstraint ? "Kullanıcı + ürün benzersiz oy kısıtı aktif" : "Benzersiz oy kısıtı pasif"} /></CardContent></Card><SignalTable title="Hızlı oy veren hesaplar" empty="Son 24 saatte hızlı oy sinyali yok." rows={data.rapidVoters.map((item) => ({ id: item.userId, primary: `${item.voteCount} oy / ${item.productCount} ürün`, secondary: `${new Date(item.firstVoteAtUtc).toLocaleTimeString("tr-TR")} – ${new Date(item.lastVoteAtUtc).toLocaleTimeString("tr-TR")}` }))} /><SignalTable title="15 dakikalık ürün patlamaları" empty="Yakın zamanda ürün oy patlaması yok." rows={data.productBursts.map((item) => ({ id: item.productId, primary: `${item.voteCount} oy / ${item.uniqueUserCount} tekil kullanıcı`, secondary: `Son oy: ${new Date(item.lastVoteAtUtc).toLocaleString("tr-TR")}` }))} /></div>;
}

function Metric({ title, value, icon: Icon, tone }: { title: string; value: number; icon: typeof AlertTriangle; tone: "rose" | "amber" | "emerald" }) { const colors = { rose: "text-rose-600 bg-rose-500/10", amber: "text-amber-600 bg-amber-500/10", emerald: "text-emerald-600 bg-emerald-500/10" }; return <Card><CardContent className="flex items-center gap-4 p-5"><div className={`rounded-xl p-3 ${colors[tone]}`}><Icon className="h-5 w-5" /></div><div><p className="text-2xl font-bold">{value}</p><p className="text-sm text-muted-foreground">{title}</p></div></CardContent></Card>; }
function Rule({ text }: { text: string }) { return <div className="flex items-start gap-2 rounded-xl bg-muted/40 p-3 text-sm"><CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600" />{text}</div>; }
function SignalTable({ title, empty, rows }: { title: string; empty: string; rows: Array<{ id: string; primary: string; secondary: string }> }) { return <Card><CardHeader><CardTitle>{title}</CardTitle></CardHeader><CardContent>{rows.length === 0 ? <p className="rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">{empty}</p> : <div className="divide-y">{rows.map((row) => <div key={row.id} className="flex flex-wrap items-center justify-between gap-3 py-3"><code className="text-xs">{row.id}</code><div className="text-right"><p className="text-sm font-semibold">{row.primary}</p><p className="text-xs text-muted-foreground">{row.secondary}</p></div></div>)}</div>}</CardContent></Card>; }
