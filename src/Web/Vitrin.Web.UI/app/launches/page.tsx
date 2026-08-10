import type { Metadata } from "next";
import Link from "next/link";
import { CalendarClock, ChevronLeft, ChevronRight, History, Rocket } from "lucide-react";
import type { DailyLaunches, LaunchArchive } from "@/core/domain/product.types";
import { LaunchRankingList } from "@/components/launch-ranking-list";
import { serverApiFetch } from "@/lib/server-api";

export const dynamic = 'force-dynamic';

export const metadata: Metadata = {
  title: "Lansman Arşivi — Vitrin",
  description: "Türkiye teknoloji ekosisteminin günlük ürün lansmanlarını, sıralamalarını ve geçmiş kazananlarını keşfet.",
};

type Props = { searchParams: Promise<{ date?: string }> };

export default async function LaunchArchivePage({ searchParams }: Props) {
  const params = await searchParams;
  const requestedDate = isIsoDate(params.date) ? params.date : istanbulDate();
  const [daily, archive] = await Promise.all([
    serverApiFetch<DailyLaunches>(`/launches/daily?date=${requestedDate}&limit=100`, { revalidate: 30, tags: ["daily-launches"] }),
    serverApiFetch<LaunchArchive>("/launches/archive?days=30", { revalidate: 300, tags: ["launch-archive"] }),
  ]);

  const previous = addDays(requestedDate, -1);
  const next = addDays(requestedDate, 1);
  const today = istanbulDate();

  return <main className="mx-auto min-h-screen w-full max-w-7xl px-4 py-8 sm:px-6 sm:py-12">
    <section className="overflow-hidden rounded-[2rem] border bg-gradient-to-br from-emerald-500/10 via-card to-orange-500/10 p-7 sm:p-10">
      <div className="flex flex-col justify-between gap-6 lg:flex-row lg:items-end">
        <div className="max-w-3xl">
          <div className="inline-flex items-center gap-2 rounded-full bg-emerald-500/10 px-3 py-1 text-xs font-bold uppercase tracking-[0.18em] text-emerald-700"><History className="h-4 w-4" /> Lansman arşivi</div>
          <h1 className="mt-4 text-4xl font-black tracking-tight sm:text-6xl">Her günün ürün sahnesi</h1>
          <p className="mt-4 max-w-2xl text-base leading-7 text-muted-foreground">Sıralamalar Türkiye saatiyle 00:00–24:00 döneminde oluşur. Editör seçimi görünürlüğü etkileyebilir, topluluk puanını değiştirmez.</p>
        </div>
        <Link href="/launches/upcoming" className="inline-flex h-12 items-center justify-center gap-2 rounded-xl bg-emerald-600 px-5 font-bold text-white hover:bg-emerald-700"><CalendarClock className="h-5 w-5" /> Yaklaşan lansmanlar</Link>
      </div>
    </section>

    <div className="mt-8 grid gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
      <section>
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-emerald-600">Seçili gün</p>
            <h2 className="text-2xl font-extrabold">{formatDate(requestedDate)}</h2>
          </div>
          <div className="flex items-center gap-2">
            <Link href={`/launches?date=${previous}`} aria-label="Önceki gün" className="rounded-xl border bg-card p-2.5 hover:bg-muted"><ChevronLeft className="h-5 w-5" /></Link>
            {requestedDate !== today && <Link href="/launches" className="rounded-xl border bg-card px-4 py-2.5 text-sm font-bold hover:bg-muted">Bugün</Link>}
            <Link href={`/launches?date=${next}`} aria-label="Sonraki gün" className="rounded-xl border bg-card p-2.5 hover:bg-muted"><ChevronRight className="h-5 w-5" /></Link>
          </div>
        </div>
        <LaunchRankingList items={daily?.items ?? []} />
      </section>

      <aside className="h-fit rounded-3xl border bg-card p-5 lg:sticky lg:top-24">
        <div className="mb-4 flex items-center gap-2"><Rocket className="h-5 w-5 text-orange-500" /><h2 className="font-extrabold">Son 30 gün</h2></div>
        <div className="space-y-1.5">{archive?.days.map(day => <Link key={day.date} href={`/launches?date=${day.date}`} className={`flex items-center justify-between rounded-xl px-3 py-2.5 text-sm transition hover:bg-muted ${day.date === requestedDate ? "bg-emerald-500/10 text-emerald-700" : ""}`}><span><strong>{shortDate(day.date)}</strong><span className="ml-2 text-xs text-muted-foreground">{day.launchCount} lansman</span></span>{day.winner && <span className="max-w-24 truncate text-xs font-semibold">#{day.winner.rank} {day.winner.productName}</span>}</Link>)}</div>
      </aside>
    </div>
  </main>;
}

function isIsoDate(value?: string): value is string { return Boolean(value && /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(Date.parse(`${value}T00:00:00Z`))); }
function istanbulDate() { const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Istanbul", year: "numeric", month: "2-digit", day: "2-digit" }).formatToParts(new Date()); const map = Object.fromEntries(parts.map(part => [part.type, part.value])); return `${map.year}-${map.month}-${map.day}`; }
function addDays(value: string, amount: number) { const date = new Date(`${value}T12:00:00Z`); date.setUTCDate(date.getUTCDate() + amount); return date.toISOString().slice(0, 10); }
function formatDate(value: string) { return new Intl.DateTimeFormat("tr-TR", { day: "numeric", month: "long", year: "numeric", weekday: "long", timeZone: "UTC" }).format(new Date(`${value}T12:00:00Z`)); }
function shortDate(value: string) { return new Intl.DateTimeFormat("tr-TR", { day: "numeric", month: "short", timeZone: "UTC" }).format(new Date(`${value}T12:00:00Z`)); }
