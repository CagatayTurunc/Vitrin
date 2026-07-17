"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { ArrowUpRight, Eye, Flame, MessageCircle, Sparkles } from "lucide-react";
import type { ProductApiModel } from "@/core/domain/product.types";

interface TrendingResponse {
  period: string;
  formula: string;
  computedAt: string;
  items: ProductApiModel[];
}

const PERIODS = [
  { value: "24h", label: "24 saat" },
  { value: "7d", label: "7 gün" },
  { value: "30d", label: "30 gün" },
] as const;

export function TrendingProducts() {
  const [period, setPeriod] = useState("7d");
  const [items, setItems] = useState<ProductApiModel[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isCurrent = true;
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products/trending?period=${period}&limit=6`)
      .then(async (response) => response.ok ? await response.json() as TrendingResponse : Promise.reject(response))
      .then((payload) => {
        if (isCurrent) setItems(payload.items);
      })
      .catch((error) => {
        console.error("Trend ürünleri yüklenemedi", error);
        if (isCurrent) setItems([]);
      })
      .finally(() => {
        if (isCurrent) setIsLoading(false);
      });

    return () => {
      isCurrent = false;
    };
  }, [period]);

  return (
    <section className="relative mt-8 overflow-hidden rounded-[2rem] border border-orange-500/15 bg-gradient-to-br from-orange-500/10 via-card to-primary/10 p-5 shadow-sm sm:p-7" aria-labelledby="trending-heading">
      <div className="pointer-events-none absolute -right-20 -top-24 h-64 w-64 rounded-full bg-orange-400/20 blur-3xl" />
      <div className="relative">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <div className="mb-1.5 flex items-center gap-2 text-sm font-semibold text-orange-600 dark:text-orange-400">
              <Flame className="h-4 w-4" /> Canlı trend radarı
            </div>
            <h2 id="trending-heading" className="text-2xl font-extrabold tracking-tight sm:text-3xl">Şu anda yükselenler</h2>
            <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
              Oy, yorum ve görüntülenme ivmesi ürünün yaşına göre dengelenir; yeni ve gerçekten ilgi gören ürünler öne çıkar.
            </p>
          </div>
          <div className="flex rounded-xl border bg-background/70 p-1 backdrop-blur">
            {PERIODS.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  if (period === option.value) return;
                  setIsLoading(true);
                  setPeriod(option.value);
                }}
                className={`rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors ${period === option.value ? "bg-foreground text-background shadow-sm" : "text-muted-foreground hover:text-foreground"}`}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>

        {isLoading ? (
          <div className="mt-6 grid gap-3 md:grid-cols-2 lg:grid-cols-3">
            {[0, 1, 2].map((item) => <div key={item} className="h-32 animate-pulse rounded-2xl border bg-background/50" />)}
          </div>
        ) : items.length === 0 ? (
          <div className="mt-6 rounded-2xl border border-dashed bg-background/40 p-8 text-center text-sm text-muted-foreground">
            Bu dönemde trend oluşturacak kadar etkileşim henüz yok.
          </div>
        ) : (
          <div className="mt-6 grid gap-3 md:grid-cols-2 lg:grid-cols-3">
            {items.map((product, index) => (
              <Link
                key={product.id}
                href={`/product/${product.slug}`}
                className="group relative overflow-hidden rounded-2xl border bg-background/80 p-4 shadow-sm backdrop-blur transition-all hover:-translate-y-0.5 hover:border-orange-500/30 hover:shadow-lg"
              >
                <div className="flex items-start gap-3">
                  <div className="relative h-12 w-12 shrink-0 overflow-hidden rounded-xl border bg-muted">
                    <Image src={product.thumbnailUrl || "/products/notai.png"} alt={product.name} fill sizes="48px" className="object-cover" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className={`inline-flex h-6 min-w-6 items-center justify-center rounded-full px-1.5 text-xs font-black ${index === 0 ? "bg-orange-500 text-white" : "bg-muted text-muted-foreground"}`}>#{index + 1}</span>
                      <h3 className="truncate font-bold">{product.name}</h3>
                    </div>
                    <p className="mt-1 truncate text-xs text-muted-foreground">{product.tagline || product.description}</p>
                  </div>
                  <ArrowUpRight className="h-4 w-4 text-muted-foreground transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:text-orange-500" />
                </div>
                <div className="mt-4 flex items-center gap-3 border-t pt-3 text-xs text-muted-foreground">
                  <span className="inline-flex items-center gap-1 font-semibold text-orange-600 dark:text-orange-400"><Flame className="h-3.5 w-3.5" /> {product.trendScore?.toFixed(1) ?? "0.0"}</span>
                  <span className="inline-flex items-center gap-1"><Sparkles className="h-3.5 w-3.5" /> {product.upvotes ?? 0}</span>
                  <span className="inline-flex items-center gap-1"><MessageCircle className="h-3.5 w-3.5" /> {product.commentCount ?? 0}</span>
                  <span className="inline-flex items-center gap-1"><Eye className="h-3.5 w-3.5" /> {product.viewCount ?? 0}</span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
