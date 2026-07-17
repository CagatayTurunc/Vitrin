"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  Activity,
  ArrowUpRight,
  Heart,
  MessageSquare,
  Package,
  Sparkles,
  UserPlus,
} from "lucide-react";
import { Button } from "@/components/ui/button";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

type FeedType = "launch" | "comment" | "reaction" | "follow";

interface ActivityItem {
  id: string;
  type: FeedType;
  actorUsername: string;
  summary: string;
  entityType: string;
  entityId: string;
  productId?: string | null;
  metadata?: string | null;
  createdAtUtc: string;
  href: string;
}

interface ProductItem {
  id: string;
  name: string;
  slug: string;
  makerId: string;
  tagline: string;
  publishedAt?: string | null;
  createdAt: string;
}

const filters = [
  { value: "all", label: "Tümü" },
  { value: "launch", label: "Lansmanlar" },
  { value: "comment", label: "Yorumlar" },
  { value: "social", label: "Sosyal" },
] as const;

const feedVisuals: Record<FeedType, { icon: typeof Activity; tone: string; label: string }> = {
  launch: { icon: Package, tone: "bg-emerald-500/10 text-emerald-500", label: "Lansman" },
  comment: { icon: MessageSquare, tone: "bg-blue-500/10 text-blue-500", label: "Yorum" },
  reaction: { icon: Heart, tone: "bg-rose-500/10 text-rose-500", label: "Tepki" },
  follow: { icon: UserPlus, tone: "bg-violet-500/10 text-violet-500", label: "Takip" },
};

function relativeTime(value: string) {
  const seconds = Math.max(1, Math.floor((Date.now() - new Date(value).getTime()) / 1000));
  if (seconds < 60) return "az önce";
  if (seconds < 3600) return `${Math.floor(seconds / 60)} dk önce`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)} sa önce`;
  if (seconds < 604800) return `${Math.floor(seconds / 86400)} gün önce`;
  return new Date(value).toLocaleDateString("tr-TR", { day: "numeric", month: "short" });
}

export default function ActivityPage() {
  const [items, setItems] = useState<ActivityItem[]>([]);
  const [filter, setFilter] = useState<(typeof filters)[number]["value"]>("all");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const [commentResponse, socialResponse, productResponse] = await Promise.all([
          fetch(`${API_URL}/api/comments/activity?limit=40`),
          fetch(`${API_URL}/api/auth/activity?limit=30`),
          fetch(`${API_URL}/api/products?sort=newest&pageSize=30`),
        ]);
        const comments = commentResponse.ok ? await commentResponse.json() as Omit<ActivityItem, "href">[] : [];
        const social = socialResponse.ok ? await socialResponse.json() as Omit<ActivityItem, "href">[] : [];
        const productPage = productResponse.ok
          ? await productResponse.json() as { items: ProductItem[] }
          : { items: [] };
        const productById = new Map(productPage.items.map((product) => [product.id, product]));

        const launches: ActivityItem[] = productPage.items.map((product) => ({
          id: `launch:${product.id}`,
          type: "launch",
          actorUsername: "maker",
          summary: `${product.name} ürününü yayınladı`,
          entityType: "Product",
          entityId: product.id,
          productId: product.id,
          metadata: product.tagline,
          createdAtUtc: product.publishedAt || product.createdAt,
          href: `/product/${product.slug}`,
        }));
        const communityItems = [...comments, ...social].map((item) => ({
          ...item,
          type: item.type as FeedType,
          href: item.productId && productById.has(item.productId)
            ? `/product/${productById.get(item.productId)?.slug}#comments`
            : item.entityType === "User" && item.metadata
              ? `/profile/${item.metadata}`
              : "/",
        }));

        if (active) {
          setItems([...launches, ...communityItems]
            .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
            .slice(0, 80));
        }
      } catch (error) {
        console.error("Aktivite akışı yüklenemedi", error);
      } finally {
        if (active) setIsLoading(false);
      }
    };

    void load();
    return () => { active = false; };
  }, []);

  const visibleItems = useMemo(() => items.filter((item) => {
    if (filter === "all") return true;
    if (filter === "social") return item.type === "follow" || item.type === "reaction";
    return item.type === filter;
  }), [filter, items]);

  return (
    <div className="mx-auto max-w-6xl pb-20">
      <section className="relative overflow-hidden rounded-3xl border bg-card px-6 py-10 shadow-sm md:px-10">
        <div className="absolute -right-20 -top-28 h-72 w-72 rounded-full bg-emerald-500/10 blur-3xl" />
        <div className="absolute -bottom-32 left-1/3 h-72 w-72 rounded-full bg-violet-500/10 blur-3xl" />
        <div className="relative max-w-2xl">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border bg-background/70 px-3 py-1 text-xs font-semibold text-emerald-500 backdrop-blur">
            <Sparkles className="h-3.5 w-3.5" /> CANLI TOPLULUK
          </div>
          <h1 className="text-4xl font-black tracking-tight md:text-5xl">Vitrin&apos;de şu anda neler oluyor?</h1>
          <p className="mt-4 max-w-xl text-base leading-7 text-muted-foreground">
            Yeni lansmanlar, değerli yorumlar, tepkiler ve topluluk bağları tek bir canlı akışta.
          </p>
        </div>
      </section>

      <div className="mt-8 grid gap-8 lg:grid-cols-[1fr_280px]">
        <main>
          <div className="mb-5 flex flex-wrap gap-2">
            {filters.map((item) => (
              <Button
                key={item.value}
                size="sm"
                variant={filter === item.value ? "default" : "outline"}
                className="rounded-full"
                onClick={() => setFilter(item.value)}
              >
                {item.label}
              </Button>
            ))}
          </div>

          <div className="overflow-hidden rounded-3xl border bg-card">
            {isLoading ? (
              <div className="p-12 text-center text-muted-foreground">Topluluk akışı hazırlanıyor...</div>
            ) : visibleItems.length === 0 ? (
              <div className="p-12 text-center text-muted-foreground">Bu filtrede henüz aktivite yok.</div>
            ) : visibleItems.map((item, index) => {
              const visual = feedVisuals[item.type];
              const Icon = visual.icon;
              return (
                <Link
                  key={item.id}
                  href={item.href}
                  className={`group flex gap-4 p-5 transition-colors hover:bg-muted/40 ${index !== visibleItems.length - 1 ? "border-b" : ""}`}
                >
                  <div className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl ${visual.tone}`}>
                    <Icon className="h-5 w-5" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                      <span className="font-semibold">@{item.actorUsername || "topluluk"}</span>
                      <span className="text-sm text-muted-foreground">{item.summary}</span>
                    </div>
                    {item.metadata && (
                      <p className="mt-2 line-clamp-2 rounded-xl bg-muted/50 px-3 py-2 text-sm text-muted-foreground">
                        {item.type === "reaction" ? `${item.metadata} tepkisi` : item.metadata}
                      </p>
                    )}
                    <div className="mt-2 flex items-center gap-2 text-xs text-muted-foreground">
                      <span className="font-medium text-foreground/70">{visual.label}</span>
                      <span>•</span>
                      <span>{relativeTime(item.createdAtUtc)}</span>
                    </div>
                  </div>
                  <ArrowUpRight className="mt-2 h-4 w-4 shrink-0 text-muted-foreground opacity-0 transition-all group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:opacity-100" />
                </Link>
              );
            })}
          </div>
        </main>

        <aside className="space-y-4">
          <div className="rounded-3xl border bg-gradient-to-br from-emerald-500/10 to-transparent p-5">
            <Activity className="h-6 w-6 text-emerald-500" />
            <h2 className="mt-4 font-bold">Akış nasıl oluşuyor?</h2>
            <p className="mt-2 text-sm leading-6 text-muted-foreground">
              Yayınlanan ürünler, görünür yorumlar, tepkiler ve yeni takipler zamana göre birleştirilir.
            </p>
          </div>
          <div className="rounded-3xl border bg-card p-5">
            <p className="text-sm font-semibold">Topluluk ilkesi</p>
            <p className="mt-2 text-sm leading-6 text-muted-foreground">
              Gizlenen veya silinen içerikler bu akışa dahil edilmez.
            </p>
            <Link href="/rules" className="mt-4 inline-flex items-center gap-1 text-sm font-semibold text-primary hover:underline">
              Kuralları incele <ArrowUpRight className="h-3.5 w-3.5" />
            </Link>
          </div>
        </aside>
      </div>
    </div>
  );
}
