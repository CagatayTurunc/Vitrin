"use client";

import { useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import { Bot, CalendarClock, Code2, Loader2, Mail, Newspaper, Rocket, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
type PreferenceKey = "dailyLaunches" | "weeklyRoundup" | "productUpdates" | "upcomingLaunches" | "aiDigest" | "developerDigest";
const choices: Array<{ key: PreferenceKey; title: string; description: string; icon: typeof Mail }> = [
  { key: "dailyLaunches", title: "Günün lansmanları", description: "Her sabah günün ilk 10 ürünü", icon: Rocket },
  { key: "weeklyRoundup", title: "Haftalık özet", description: "Pazar günü kazananlar ve yükselenler", icon: Newspaper },
  { key: "productUpdates", title: "Takip ettiklerin", description: "Ürün ve maker güncellemeleri", icon: Sparkles },
  { key: "upcomingLaunches", title: "Yaklaşan lansmanlar", description: "Hatırlatmalar ve takvim özeti", icon: CalendarClock },
  { key: "aiDigest", title: "AI seçkisi", description: "Türkiye’den ve dünyadan AI ürünleri", icon: Bot },
  { key: "developerDigest", title: "Geliştirici araçları", description: "Yeni API, açık kaynak ve devtool’lar", icon: Code2 },
];
export default function NewsletterPage() {
  const { data: session } = useSession(); const accessToken = session?.accessToken as string | undefined;
  const sessionEmail = session?.user?.email ?? "";
  const [email, setEmail] = useState(sessionEmail); const [preferences, setPreferences] = useState<Record<PreferenceKey, boolean>>({ dailyLaunches: false, weeklyRoundup: true, productUpdates: false, upcomingLaunches: false, aiDigest: false, developerDigest: false }); const [saving, setSaving] = useState(false); const [status, setStatus] = useState("");
  useEffect(() => { if (!accessToken) return; void fetch(`${API_URL}/api/newsletter/me`, { headers: { Authorization: `Bearer ${accessToken}` } }).then(async response => { if (!response.ok) return; const data = await response.json(); setEmail(data.emailAddress); setPreferences({ dailyLaunches: data.dailyLaunches, weeklyRoundup: data.weeklyRoundup, productUpdates: data.productUpdates, upcomingLaunches: data.upcomingLaunches, aiDigest: data.aiDigest, developerDigest: data.developerDigest }); }); }, [accessToken]);
  useEffect(() => {
    if (!email && sessionEmail) {
      // Session hydration supplies the initial account email after mount.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setEmail(sessionEmail);
    }
  }, [email, sessionEmail]);
  const subscribe = async () => { setSaving(true); setStatus(""); try { const response = await fetch(`${API_URL}/api/newsletter/subscribe`, { method: "POST", headers: { ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}), "Content-Type": "application/json" }, body: JSON.stringify({ emailAddress: email, ...preferences }) }); if (!response.ok) throw new Error("Abonelik kaydedilemedi. E-posta adresini kontrol et."); setStatus("Bülten tercihlerin kaydedildi. Bir sonraki seçkide görüşürüz."); } catch (error) { setStatus(error instanceof Error ? error.message : "Abonelik kaydedilemedi."); } finally { setSaving(false); } };
  return <main className="mx-auto min-h-screen w-full max-w-5xl px-4 py-10 sm:px-6"><section className="relative overflow-hidden rounded-[2.5rem] border bg-card p-8 sm:p-12"><div className="absolute -right-20 -top-20 h-72 w-72 rounded-full bg-primary/10 blur-3xl" /><div className="relative max-w-2xl"><div className="mb-5 inline-flex items-center gap-2 rounded-full bg-primary/10 px-3 py-1 text-sm font-bold text-primary"><Mail className="h-4 w-4" /> Vitrin Bültenleri</div><h1 className="text-4xl font-black tracking-tight sm:text-6xl">Ürün ekosistemi gelen kutunda.</h1><p className="mt-4 text-lg leading-8 text-muted-foreground">Her şeyi göndermiyoruz. İlgi duyduğun seçkileri seç; lansmanları, kazananları ve maker güncellemelerini kendi ritminde al.</p></div></section>
    <section className="mt-8 grid gap-4 md:grid-cols-2">{choices.map(choice => { const Icon = choice.icon; const selected = preferences[choice.key]; return <button key={choice.key} type="button" onClick={() => setPreferences(current => ({ ...current, [choice.key]: !selected }))} className={`flex items-center gap-4 rounded-2xl border p-5 text-left transition ${selected ? "border-primary bg-primary/5 ring-1 ring-primary" : "bg-card hover:border-primary/30"}`}><span className="rounded-xl bg-primary/10 p-3 text-primary"><Icon className="h-5 w-5" /></span><span className="flex-1"><span className="block font-black">{choice.title}</span><span className="mt-1 block text-sm text-muted-foreground">{choice.description}</span></span><span className={`h-5 w-9 rounded-full p-0.5 ${selected ? "bg-primary" : "bg-muted"}`}><span className={`block h-4 w-4 rounded-full bg-white transition ${selected ? "translate-x-4" : ""}`} /></span></button>; })}</section>
    <section className="mt-8 rounded-3xl border bg-card p-6"><div className="flex flex-col gap-3 sm:flex-row"><Input type="email" value={email} onChange={event => setEmail(event.target.value)} placeholder="sen@ornek.com" className="h-11 flex-1" /><Button onClick={subscribe} disabled={saving || !email.includes("@") || !Object.values(preferences).some(Boolean)} className="h-11 sm:px-8">{saving && <Loader2 className="animate-spin" />} Tercihlerimi kaydet</Button></div>{status && <p className="mt-3 text-sm text-muted-foreground" role="status">{status}</p>}<p className="mt-4 text-xs text-muted-foreground">İstediğin zaman abonelikten çıkabilir veya seçkilerini değiştirebilirsin. E-posta adresin yalnızca bu gönderimler için kullanılır.</p></section>
  </main>;
}
