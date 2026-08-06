import Image from "next/image";
import Link from "next/link";
import { ArrowUp, Eye, MessageSquare, Sparkles } from "lucide-react";
import type { RankedLaunch } from "@/core/domain/product.types";

export function LaunchRankingList({ items, emptyText = "Bu gün için yayınlanmış lansman yok." }: { items: RankedLaunch[]; emptyText?: string }) {
  if (items.length === 0) {
    return <div className="rounded-3xl border border-dashed bg-muted/20 px-6 py-16 text-center text-muted-foreground">{emptyText}</div>;
  }

  return <div className="space-y-3">
    {items.map((launch) => (
      <article key={launch.launchId} className="group grid grid-cols-[42px_56px_minmax(0,1fr)] items-center gap-3 rounded-2xl border bg-card p-3 transition hover:border-emerald-500/35 hover:shadow-md sm:grid-cols-[52px_64px_minmax(0,1fr)_auto] sm:gap-4 sm:p-4">
        <div className="text-center">
          <div className="text-xl font-black tabular-nums text-muted-foreground group-hover:text-emerald-600">#{launch.rank}</div>
          {launch.rank <= 3 && <span className="text-[10px] font-bold uppercase tracking-wide text-amber-600">Top {launch.rank}</span>}
        </div>
        <Link href={`/product/${launch.productSlug}`} className="relative h-14 w-14 overflow-hidden rounded-2xl border bg-muted sm:h-16 sm:w-16">
          <Image src={launch.thumbnailUrl || "/products/notai.png"} alt={`${launch.productName} logosu`} fill sizes="64px" className="object-cover" />
        </Link>
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <Link href={`/product/${launch.productSlug}`} className="truncate font-extrabold hover:text-emerald-600 sm:text-lg">{launch.productName}</Link>
            {launch.isFeatured && <span className="inline-flex items-center gap-1 rounded-full bg-amber-500/10 px-2 py-0.5 text-[10px] font-bold uppercase text-amber-700"><Sparkles className="h-3 w-3" /> Editör seçimi</span>}
          </div>
          <p className="mt-0.5 line-clamp-2 text-sm text-muted-foreground">{launch.tagline}</p>
          <div className="mt-2 flex flex-wrap gap-1.5">{launch.categories.slice(0, 3).map(category => <Link key={category.id} href={`/category/${category.slug}`} className="rounded-full bg-muted px-2 py-1 text-[11px] font-semibold hover:bg-emerald-500/10 hover:text-emerald-700">{category.name}</Link>)}</div>
        </div>
        <div className="col-start-2 col-span-2 flex items-center justify-between gap-3 border-t pt-3 text-xs text-muted-foreground sm:col-auto sm:border-0 sm:pt-0">
          <div className="flex items-center gap-3">
            <span className="inline-flex items-center gap-1"><ArrowUp className="h-3.5 w-3.5" />{launch.upvotes}</span>
            <span className="inline-flex items-center gap-1"><MessageSquare className="h-3.5 w-3.5" />{launch.comments}</span>
            <span className="inline-flex items-center gap-1"><Eye className="h-3.5 w-3.5" />{launch.views}</span>
          </div>
          <div className="rounded-xl bg-emerald-500/10 px-3 py-2 text-right text-emerald-700">
            <div className="font-black tabular-nums">{launch.score.toLocaleString("tr-TR", { maximumFractionDigits: 1 })}</div>
            <div className="text-[9px] font-bold uppercase tracking-wide">puan</div>
          </div>
        </div>
      </article>
    ))}
  </div>;
}
