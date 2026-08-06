"use client";

import { useCallback, useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import Image from "next/image";
import Link from "next/link";
import {
  BarChart2, Eye, Heart, MessageSquare, TrendingUp, Users,
  ArrowUpRight, Loader2, RefreshCw, ExternalLink, Clock3, ListChecks, Star, Bell, MessagesSquare
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
// Analytics endpointleri gateway üzerinden /api/analytics/* ile ulaşılır
const ANALYTICS_BASE = API_URL;

interface MyProduct {
  id: string;
  name: string;
  slug: string;
  tagline: string;
  thumbnailUrl: string;
  status: number;
  upvotes: number;
  publishedAt: string | null;
}

interface ProductStat {
  productId: string;
  totalViews: number;
  totalUpvotes: number;
  totalComments: number;
  conversionRate: number;
}

interface DailyPoint {
  date: string;
  count: number;
}

interface ReferrerStat {
  source: string;
  count: number;
  percentage: number;
}

interface RetentionStat {
  totalViews: number;
  uniqueViewers: number;
  returnViewers: number;
  retentionRate: number;
}

interface ProductDetail {
  product: MyProduct;
  stat: ProductStat;
  timeseries?: DailyPoint[];      // views günlük
  upvoteSeries?: DailyPoint[];    // upvotes günlük
  referrers?: ReferrerStat[];
  retention?: RetentionStat;
}

interface MakerControl {
  id: string; name: string; slug: string; status: number; scheduledLaunchAt?: string | null;
  hasLogo: boolean; hasLongDescription: boolean; hasGallery: boolean; hasWebsite: boolean; hasCategory: boolean; hasTopics: boolean;
  followerCount: number; reviewCount: number; averageRating: number; changelogCount: number; forumThreadCount: number; forumReplyCount: number; completenessScore: number;
}

const PERIODS = [
  { value: 7, label: "7 gün" },
  { value: 30, label: "30 gün" },
  { value: 90, label: "90 gün" },
];

function MetricCard({
  icon: Icon,
  label,
  value,
  sub,
  color,
}: {
  icon: typeof Eye;
  label: string;
  value: string | number;
  sub?: string;
  color: string;
}) {
  return (
    <div className="flex items-center gap-4 rounded-2xl border border-border bg-card p-4">
      <div className={`rounded-xl p-2.5 ${color}`}>
        <Icon className="h-5 w-5" />
      </div>
      <div>
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-xl font-bold">{value}</p>
        {sub && <p className="text-xs text-muted-foreground">{sub}</p>}
      </div>
    </div>
  );
}

function SimpleBarChart({ data, color = "bg-primary" }: { data: DailyPoint[]; color?: string }) {
  if (!data.length) return null;
  const max = Math.max(...data.map((d) => d.count), 1);
  return (
    <div className="flex h-20 items-end gap-0.5 overflow-hidden">
      {data.map((point) => (
        <div
          key={point.date}
          className="flex-1 min-w-0"
          title={`${point.date}: ${point.count}`}
        >
          <div
            className={`${color} rounded-sm opacity-80 transition-all`}
            style={{ height: `${Math.max((point.count / max) * 100, 2)}%` }}
          />
        </div>
      ))}
    </div>
  );
}

export default function DashboardPage() {
  const { data: session, status } = useSession();
  const router = useRouter();
  const accessToken = session?.accessToken;

  const [products, setProducts] = useState<MyProduct[]>([]);
  const [stats, setStats] = useState<Map<string, ProductStat>>(new Map());
  const [selected, setSelected] = useState<string | null>(null);
  const [detail, setDetail] = useState<Partial<ProductDetail> | null>(null);
  const [period, setPeriod] = useState(30);
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [controls, setControls] = useState<MakerControl[]>([]);

  useEffect(() => {
    if (status === "unauthenticated") router.replace("/login");
  }, [status, router]);

  // Fetch published products + aggregate stats
  const load = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      const controlResponse = await fetch(`${API_URL}/api/maker/dashboard`, { headers: { Authorization: `Bearer ${accessToken}` } });
      if (controlResponse.ok) setControls(await controlResponse.json() as MakerControl[]);
      const res = await fetch(`${API_URL}/api/products/my-products`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (!res.ok) throw new Error("Ürünler yüklenemedi.");
      const all = (await res.json()) as MyProduct[];
      const published = all.filter((p) => p.status === 2);
      setProducts(published);

      if (published.length === 0) return;

      // Bulk analytics stats
      const ids = published.map((p) => p.id);
      const statsRes = await fetch(`${ANALYTICS_BASE}/api/analytics/maker/products`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ productIds: ids }),
      });
      if (statsRes.ok) {
        const data = (await statsRes.json()) as ProductStat[];
        const map = new Map<string, ProductStat>(data.map((s) => [s.productId, s]));
        setStats(map);
        if (!selected && published[0]) setSelected(published[0].id);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Veri yüklenemedi.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, selected]);

  useEffect(() => {
    // Authentication changes intentionally refresh the maker dashboard.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const publishChangelog = async (productId: string) => {
    if (!accessToken) return;
    const version = window.prompt("Sürüm (ör. 2.1.0)");
    if (!version) return;
    const title = window.prompt("Güncelleme başlığı");
    if (!title) return;
    const body = window.prompt("Neler değişti? (en az 10 karakter)");
    if (!body || body.trim().length < 10) return;
    const response = await fetch(`${API_URL}/api/products/${productId}/changelog`, {
      method: "POST",
      headers: { Authorization: `Bearer ${accessToken}`, "Content-Type": "application/json" },
      body: JSON.stringify({ version, title, body }),
    });
    if (response.ok) await load();
  };

  // Fetch detail for selected product
  useEffect(() => {
    if (!selected || !accessToken) return;
    // Selecting a product intentionally resets and reloads its detail panel.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setIsDetailLoading(true);
    setDetail(null);

    const product = products.find((p) => p.id === selected);
    if (!product) { setIsDetailLoading(false); return; }

    const headers: HeadersInit = { Authorization: `Bearer ${accessToken}` };
    const base = `${ANALYTICS_BASE}/api/analytics/product/${selected}`;

    Promise.all([
      fetch(`${base}/timeseries?metric=views&days=${period}`, { headers }).then((r) => r.ok ? r.json() : null),
      fetch(`${base}/timeseries?metric=upvotes&days=${period}`, { headers }).then((r) => r.ok ? r.json() : null),
      fetch(`${base}/referrers?days=${period}`, { headers }).then((r) => r.ok ? r.json() : null),
      fetch(`${base}/retention?days=${period}`, { headers }).then((r) => r.ok ? r.json() : null),
    ]).then(([tsData, upvoteData, refData, retData]) => {
      setDetail({
        product,
        stat: stats.get(selected) ?? { productId: selected, totalViews: 0, totalUpvotes: 0, totalComments: 0, conversionRate: 0 },
        timeseries: (tsData as { series: DailyPoint[] } | null)?.series,
        upvoteSeries: (upvoteData as { series: DailyPoint[] } | null)?.series,
        referrers: (refData as { referrers: ReferrerStat[] } | null)?.referrers,
        retention: (retData as { retention: RetentionStat } | null)?.retention,
      });
    }).catch(() => {
      setDetail({ product, stat: stats.get(selected) ?? { productId: selected, totalViews: 0, totalUpvotes: 0, totalComments: 0, conversionRate: 0 } });
    }).finally(() => setIsDetailLoading(false));
  }, [selected, period, accessToken, products, stats]);

  if (status === "loading" || isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center space-y-3">
          <p className="text-destructive">{error}</p>
          <Button onClick={() => void load()}><RefreshCw className="mr-2 h-4 w-4" /> Tekrar dene</Button>
        </div>
      </div>
    );
  }

  const totalViews = [...stats.values()].reduce((s, v) => s + v.totalViews, 0);
  const totalUpvotes = [...stats.values()].reduce((s, v) => s + v.totalUpvotes, 0);
  const totalComments = [...stats.values()].reduce((s, v) => s + v.totalComments, 0);
  const avgConversion = stats.size > 0
    ? ([...stats.values()].reduce((s, v) => s + v.conversionRate, 0) / stats.size).toFixed(1)
    : "0";

  return (
    <main className="mx-auto min-h-screen max-w-7xl px-4 py-8 sm:px-6 sm:py-12">
      {/* Header */}
      <div className="mb-8 flex items-start justify-between gap-4">
        <div>
          <div className="mb-1 inline-flex items-center gap-2 rounded-full border border-primary/20 bg-primary/10 px-3 py-1 text-xs font-bold uppercase tracking-widest text-primary">
            <BarChart2 className="h-3.5 w-3.5" /> Maker Dashboard
          </div>
          <h1 className="text-3xl font-black tracking-tight sm:text-4xl">Ürün Analizleri</h1>
          <p className="mt-1 text-muted-foreground">Yayınlanan ürünlerin views, upvote ve referrer istatistikleri</p>
        </div>
        <div className="flex items-center gap-2">
          {PERIODS.map((p) => (
            <button
              key={p.value}
              onClick={() => setPeriod(p.value)}
              className={`rounded-full px-3 py-1.5 text-xs font-semibold transition-colors ${
                period === p.value ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"
              }`}
            >
              {p.label}
            </button>
          ))}
        </div>
      </div>

      {controls.length > 0 && <section className="mb-8 rounded-3xl border bg-card p-5 sm:p-6">
        <div className="mb-5 flex items-center justify-between gap-3"><div><h2 className="flex items-center gap-2 text-lg font-black"><ListChecks className="h-5 w-5 text-primary" /> Lansman kontrol merkezi</h2><p className="mt-1 text-sm text-muted-foreground">Hazırlık, topluluk ve geri dönüş sinyalleri tek yerde.</p></div></div>
        <div className="grid gap-4 lg:grid-cols-2">{controls.map(control => {
          const scheduled = control.scheduledLaunchAt ? new Date(control.scheduledLaunchAt) : null;
          const checks = [control.hasLogo, control.hasLongDescription, control.hasGallery, control.hasWebsite, control.hasCategory, control.hasTopics];
          return <article key={control.id} className="rounded-2xl border p-5"><div className="flex items-start justify-between gap-3"><div><h3 className="font-black">{control.name}</h3><p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground"><Clock3 className="h-3 w-3" />{scheduled ? `${scheduled.toLocaleString("tr-TR")} lansmanı` : "Henüz planlı lansman yok"}</p></div><span className="rounded-full bg-primary/10 px-2.5 py-1 text-xs font-black text-primary">%{control.completenessScore}</span></div>
            <div className="mt-4 h-2 overflow-hidden rounded-full bg-muted"><div className="h-full rounded-full bg-primary" style={{ width: `${control.completenessScore}%` }} /></div><p className="mt-2 text-xs text-muted-foreground">{checks.filter(Boolean).length}/6 hazırlık adımı tamamlandı · logo, açıklama, galeri, site, kategori ve topic</p>
            <div className="mt-4 grid grid-cols-3 gap-2 text-center"><div className="rounded-xl bg-muted/50 p-2"><Bell className="mx-auto h-4 w-4 text-primary" /><p className="mt-1 text-sm font-black">{control.followerCount}</p><p className="text-[10px] text-muted-foreground">takipçi</p></div><div className="rounded-xl bg-muted/50 p-2"><Star className="mx-auto h-4 w-4 text-amber-500" /><p className="mt-1 text-sm font-black">{control.reviewCount} · {control.averageRating.toFixed(1)}</p><p className="text-[10px] text-muted-foreground">review</p></div><div className="rounded-xl bg-muted/50 p-2"><MessagesSquare className="mx-auto h-4 w-4 text-violet-500" /><p className="mt-1 text-sm font-black">{control.forumThreadCount}/{control.forumReplyCount}</p><p className="text-[10px] text-muted-foreground">konu/yanıt</p></div></div>
            <div className="mt-4 flex flex-wrap gap-2"><Link href={`/product/${control.slug}`}><Button size="sm" variant="outline">Ürün sayfası</Button></Link><Link href={`/p/${control.slug}`}><Button size="sm" variant="outline">Forumu yönet</Button></Link><Button size="sm" variant="outline" onClick={() => void publishChangelog(control.id)}>Güncelleme yayınla ({control.changelogCount})</Button><Link href={`/product/${control.slug}?utm_source=vitrin&utm_medium=maker_dashboard&utm_campaign=launch`}><Button size="sm">UTM bağlantısı</Button></Link></div>
          </article>;
        })}</div>
      </section>}

      {products.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-border bg-muted/20 p-16 text-center">
          <BarChart2 className="mx-auto mb-4 h-12 w-12 text-muted-foreground opacity-30" />
          <h3 className="text-xl font-bold">Yayınlanmış ürün yok</h3>
          <p className="mt-2 text-sm text-muted-foreground">Analiz görmek için en az bir ürün yayına alınmalı.</p>
          <Link href="/submit" className="mt-4 inline-block">
            <Button>Ürün Ekle</Button>
          </Link>
        </div>
      ) : (
        <>
          {/* Summary cards */}
          <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <MetricCard icon={Eye} label="Toplam görüntülenme" value={totalViews.toLocaleString("tr-TR")} color="bg-blue-500/10 text-blue-500" />
            <MetricCard icon={Heart} label="Toplam upvote" value={totalUpvotes.toLocaleString("tr-TR")} color="bg-pink-500/10 text-pink-500" />
            <MetricCard icon={MessageSquare} label="Toplam yorum" value={totalComments.toLocaleString("tr-TR")} color="bg-violet-500/10 text-violet-500" />
            <MetricCard icon={TrendingUp} label="Ort. conversion" value={`%${avgConversion}`} sub="görüntülenme → upvote" color="bg-emerald-500/10 text-emerald-500" />
          </div>

          <div className="grid gap-6 lg:grid-cols-[280px_minmax(0,1fr)]">
            {/* Product list */}
            <div className="space-y-2">
              <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-muted-foreground">Ürünler</h2>
              {products.map((product) => {
                const s = stats.get(product.id);
                return (
                  <button
                    key={product.id}
                    onClick={() => setSelected(product.id)}
                    className={`flex w-full items-center gap-3 rounded-2xl border p-3 text-left transition-colors ${
                      selected === product.id
                        ? "border-primary/40 bg-primary/5"
                        : "border-border bg-card hover:bg-muted/30"
                    }`}
                  >
                    <div className="relative h-10 w-10 shrink-0 overflow-hidden rounded-lg border border-border bg-muted">
                      {product.thumbnailUrl ? (
                        <Image src={product.thumbnailUrl} alt={product.name} fill sizes="40px" className="object-cover" />
                      ) : (
                        <span className="absolute inset-0 flex items-center justify-center text-xs font-bold text-muted-foreground">
                          {product.name.substring(0, 2).toUpperCase()}
                        </span>
                      )}
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-semibold">{product.name}</p>
                      <div className="mt-0.5 flex items-center gap-2 text-xs text-muted-foreground">
                        <span className="flex items-center gap-0.5"><Eye className="h-3 w-3" />{s?.totalViews ?? "–"}</span>
                        <span className="flex items-center gap-0.5"><Heart className="h-3 w-3" />{s?.totalUpvotes ?? "–"}</span>
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>

            {/* Detail panel */}
            <div>
              {isDetailLoading ? (
                <div className="flex h-64 items-center justify-center rounded-3xl border border-border bg-card">
                  <Loader2 className="h-6 w-6 animate-spin text-primary" />
                </div>
              ) : detail ? (
                <div className="space-y-5">
                  {/* Product header */}
                  <div className="flex items-center justify-between gap-4 rounded-2xl border border-border bg-card px-5 py-4">
                    <div>
                      <h3 className="font-bold">{detail.product?.name}</h3>
                      <p className="text-sm text-muted-foreground">{detail.product?.tagline}</p>
                    </div>
                    <Link href={`/product/${detail.product?.slug}`} target="_blank">
                      <Button variant="outline" size="sm">
                        <ExternalLink className="mr-1.5 h-3.5 w-3.5" /> Görüntüle
                      </Button>
                    </Link>
                  </div>

                  {/* Metric cards */}
                  <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                    <MetricCard icon={Eye} label="Görüntülenme" value={(detail.stat?.totalViews ?? 0).toLocaleString("tr-TR")} color="bg-blue-500/10 text-blue-500" />
                    <MetricCard icon={Heart} label="Upvote" value={(detail.stat?.totalUpvotes ?? 0).toLocaleString("tr-TR")} color="bg-pink-500/10 text-pink-500" />
                    <MetricCard icon={MessageSquare} label="Yorum" value={(detail.stat?.totalComments ?? 0).toLocaleString("tr-TR")} color="bg-violet-500/10 text-violet-500" />
                    <MetricCard icon={TrendingUp} label="Conversion" value={`%${detail.stat?.conversionRate ?? 0}`} sub="görüntülenme → upvote" color="bg-emerald-500/10 text-emerald-500" />
                  </div>

                  <div className="grid gap-5 lg:grid-cols-2">
                    {/* Views time series */}
                    {detail.timeseries && detail.timeseries.length > 0 && (
                      <Card>
                        <CardHeader className="pb-2">
                          <CardTitle className="flex items-center gap-2 text-sm font-semibold">
                            <Eye className="h-4 w-4 text-blue-500" /> Günlük görüntülenme
                          </CardTitle>
                        </CardHeader>
                        <CardContent>
                          <SimpleBarChart data={detail.timeseries} color="bg-blue-500" />
                          <div className="mt-2 flex justify-between text-xs text-muted-foreground">
                            <span>{detail.timeseries[0]?.date}</span>
                            <span>{detail.timeseries[detail.timeseries.length - 1]?.date}</span>
                          </div>
                        </CardContent>
                      </Card>
                    )}

                    {/* Upvote time series */}
                    {detail.upvoteSeries && detail.upvoteSeries.length > 0 && (
                      <Card>
                        <CardHeader className="pb-2">
                          <CardTitle className="flex items-center gap-2 text-sm font-semibold">
                            <Heart className="h-4 w-4 text-pink-500" /> Günlük upvote
                          </CardTitle>
                        </CardHeader>
                        <CardContent>
                          <SimpleBarChart data={detail.upvoteSeries} color="bg-pink-500" />
                          <div className="mt-2 flex justify-between text-xs text-muted-foreground">
                            <span>{detail.upvoteSeries[0]?.date}</span>
                            <span>{detail.upvoteSeries[detail.upvoteSeries.length - 1]?.date}</span>
                          </div>
                        </CardContent>
                      </Card>
                    )}

                    {/* Referrers */}
                    {detail.referrers && detail.referrers.length > 0 && (
                      <Card>
                        <CardHeader className="pb-2">
                          <CardTitle className="flex items-center gap-2 text-sm font-semibold">
                            <ArrowUpRight className="h-4 w-4 text-emerald-500" /> Trafik kaynakları
                          </CardTitle>
                        </CardHeader>
                        <CardContent>
                          <div className="space-y-2">
                            {detail.referrers.slice(0, 6).map((r) => (
                              <div key={r.source} className="flex items-center gap-3">
                                <span className="w-32 truncate text-xs font-medium">{r.source}</span>
                                <div className="flex-1 overflow-hidden rounded-full bg-muted h-2">
                                  <div
                                    className="h-2 rounded-full bg-emerald-500"
                                    style={{ width: `${r.percentage}%` }}
                                  />
                                </div>
                                <span className="w-10 text-right text-xs text-muted-foreground">%{r.percentage}</span>
                              </div>
                            ))}
                          </div>
                        </CardContent>
                      </Card>
                    )}

                    {/* Conversion funnel */}
                    {detail.stat && (detail.stat.totalViews > 0 || detail.stat.totalUpvotes > 0) && (
                      <Card>
                        <CardHeader className="pb-2">
                          <CardTitle className="flex items-center gap-2 text-sm font-semibold">
                            <TrendingUp className="h-4 w-4 text-emerald-500" /> Conversion funnel
                          </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-3">
                          {[
                            {
                              label: "Görüntülenme",
                              value: detail.stat.totalViews,
                              max: detail.stat.totalViews,
                              color: "bg-blue-500",
                            },
                            {
                              label: "Upvote",
                              value: detail.stat.totalUpvotes,
                              max: detail.stat.totalViews,
                              color: "bg-pink-500",
                            },
                            {
                              label: "Yorum",
                              value: detail.stat.totalComments,
                              max: detail.stat.totalViews,
                              color: "bg-violet-500",
                            },
                          ].map((item) => {
                            const pct = item.max > 0
                              ? Math.max((item.value / item.max) * 100, item.value > 0 ? 2 : 0)
                              : 0;
                            return (
                              <div key={item.label}>
                                <div className="mb-1 flex justify-between text-xs">
                                  <span className="font-medium">{item.label}</span>
                                  <span className="text-muted-foreground">
                                    {item.value.toLocaleString("tr-TR")}
                                    {item.max > 0 && item.label !== "Görüntülenme" && (
                                      <span className="ml-1 opacity-60">
                                        (%{((item.value / item.max) * 100).toFixed(1)})
                                      </span>
                                    )}
                                  </span>
                                </div>
                                <div className="h-2.5 w-full overflow-hidden rounded-full bg-muted">
                                  <div
                                    className={`h-full rounded-full transition-all ${item.color}`}
                                    style={{ width: `${pct}%` }}
                                  />
                                </div>
                              </div>
                            );
                          })}
                        </CardContent>
                      </Card>
                    )}
                  </div>

                  {/* Retention */}
                  {detail.retention && (
                    <Card>
                      <CardHeader className="pb-2">
                        <CardTitle className="flex items-center gap-2 text-sm font-semibold">
                          <Users className="h-4 w-4 text-violet-500" /> Retention
                        </CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                          {[
                            { label: "Toplam görüntülenme", value: detail.retention.totalViews },
                            { label: "Tekil ziyaretçi", value: detail.retention.uniqueViewers },
                            { label: "Geri dönen", value: detail.retention.returnViewers },
                            { label: "Retention oranı", value: `%${detail.retention.retentionRate}` },
                          ].map((item) => (
                            <div key={item.label} className="rounded-xl border border-border bg-muted/20 p-3 text-center">
                              <p className="text-lg font-bold">{item.value}</p>
                              <p className="text-xs text-muted-foreground">{item.label}</p>
                            </div>
                          ))}
                        </div>
                      </CardContent>
                    </Card>
                  )}
                </div>
              ) : (
                <div className="flex h-64 items-center justify-center rounded-3xl border border-dashed border-border bg-muted/20">
                  <p className="text-sm text-muted-foreground">Sol taraftan bir ürün seçin.</p>
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </main>
  );
}
