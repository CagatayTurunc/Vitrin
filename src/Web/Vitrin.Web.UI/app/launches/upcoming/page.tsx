import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { ArrowLeft, CalendarClock, Clock3, Rocket } from "lucide-react";
import type { UpcomingLaunches } from "@/core/domain/product.types";
import { serverApiFetch } from "@/lib/server-api";

export const metadata: Metadata = {
  title: "Yaklaşan Lansmanlar — Vitrin",
  description: "Önümüzdeki 30 gün içinde Vitrin'de yayınlanacak Türkiye merkezli ürünleri keşfet.",
};

export default async function UpcomingLaunchesPage() {
  const data = await serverApiFetch<UpcomingLaunches>("/launches/upcoming?days=30&limit=100", { revalidate: 30, tags: ["upcoming-launches"] });

  return <main className="mx-auto min-h-screen w-full max-w-6xl px-4 py-8 sm:px-6 sm:py-12">
    <Link href="/launches" className="mb-6 inline-flex items-center gap-2 text-sm font-bold text-muted-foreground hover:text-foreground"><ArrowLeft className="h-4 w-4" /> Lansman arşivi</Link>
    <section className="rounded-[2rem] border bg-gradient-to-br from-orange-500/10 via-card to-emerald-500/10 p-8 sm:p-12">
      <div className="inline-flex items-center gap-2 rounded-full bg-orange-500/10 px-3 py-1 text-xs font-bold uppercase tracking-[0.18em] text-orange-700"><CalendarClock className="h-4 w-4" /> Önümüzdeki 30 gün</div>
      <h1 className="mt-4 text-4xl font-black tracking-tight sm:text-6xl">Sıradaki ürünleri ilk sen gör</h1>
      <p className="mt-4 max-w-2xl leading-7 text-muted-foreground">İncelemesi onaylanmış ve yayın tarihi planlanmış lansmanlar. Saatler Türkiye saatine göre gösterilir.</p>
    </section>

    <section className="mt-8 space-y-3">
      {(data?.items.length ?? 0) === 0 && <div className="rounded-3xl border border-dashed px-6 py-20 text-center"><Rocket className="mx-auto mb-4 h-12 w-12 text-muted-foreground/40" /><h2 className="text-xl font-bold">Planlanmış lansman bulunmuyor</h2><p className="mt-2 text-muted-foreground">Yeni planlanan ürünler burada otomatik görünecek.</p></div>}
      {data?.items.map(item => <article key={item.launchId} className="flex flex-col gap-4 rounded-3xl border bg-card p-5 sm:flex-row sm:items-center">
        <Link href={`/product/${item.productSlug}`} className="relative h-16 w-16 shrink-0 overflow-hidden rounded-2xl border bg-muted"><Image src={item.thumbnailUrl || "/products/notai.png"} alt={`${item.productName} logosu`} fill sizes="64px" className="object-cover" /></Link>
        <div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><Link href={`/product/${item.productSlug}`} className="text-lg font-extrabold hover:text-emerald-600">{item.productName}</Link><span className="rounded-full bg-muted px-2 py-1 text-[10px] font-bold uppercase">{item.versionLabel}</span></div><p className="mt-1 text-sm text-muted-foreground">{item.tagline}</p><div className="mt-2 flex flex-wrap gap-1.5">{item.categories.map(category => <Link key={category.id} href={`/category/${category.slug}`} className="rounded-full bg-emerald-500/10 px-2 py-1 text-[11px] font-semibold text-emerald-700">{category.name}</Link>)}</div></div>
        <div className="rounded-2xl border bg-background px-4 py-3 sm:text-right"><div className="inline-flex items-center gap-2 font-bold"><Clock3 className="h-4 w-4 text-orange-500" />{formatLaunchDate(item.scheduledAtUtc)}</div><div className="mt-1 text-xs text-muted-foreground">Türkiye saati</div></div>
      </article>)}
    </section>
  </main>;
}

function formatLaunchDate(value: string) { return new Intl.DateTimeFormat("tr-TR", { timeZone: "Europe/Istanbul", day: "numeric", month: "long", year: "numeric", hour: "2-digit", minute: "2-digit" }).format(new Date(value)); }
