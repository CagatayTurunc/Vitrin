import Link from "next/link";
import { CalendarDays, ChevronRight } from "lucide-react";
import type { DailyLaunches } from "@/core/domain/product.types";
import { serverApiFetch } from "@/lib/server-api";
import { LaunchRankingList } from "@/components/launch-ranking-list";

export async function DailyLaunchFeed() {
  const data = await serverApiFetch<DailyLaunches>("/launches/daily?limit=20", { revalidate: 30, tags: ["daily-launches"] });
  if (!data) return null;

  return <section className="mt-8" aria-labelledby="daily-launches-heading">
    <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
      <div>
        <div className="inline-flex items-center gap-2 text-xs font-bold uppercase tracking-[0.18em] text-emerald-600"><CalendarDays className="h-4 w-4" /> İstanbul saatiyle bugün</div>
        <h2 id="daily-launches-heading" className="mt-1 text-2xl font-extrabold sm:text-3xl">Günün lansman sıralaması</h2>
        <p className="mt-1 text-sm text-muted-foreground">Upvote, yorum, keşif ve tazelik sinyalleriyle hesaplanan topluluk sırası.</p>
      </div>
      <Link href="/launches" className="inline-flex items-center gap-1 text-sm font-bold text-emerald-700 hover:underline">Arşivi aç <ChevronRight className="h-4 w-4" /></Link>
    </div>
    <LaunchRankingList items={data.items} />
  </section>;
}
