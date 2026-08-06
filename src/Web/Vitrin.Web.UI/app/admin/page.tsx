"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import { Activity, AlertTriangle, Eye, Loader2, Package, Rocket, UserRoundCheck, Users } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

interface DashboardData {
  generatedAtUtc: string;
  totalProducts: number;
  publishedProducts: number;
  pendingProducts: number;
  scheduledProducts: number;
  totalLaunches: number;
  launchesToday: number;
  totalViews: number;
  totalFollowers: number;
  submissionSeries: Array<{ date: string; count: number }>;
  recentProducts: Array<{ id: string; name: string; slug: string; status: number; createdAt: string; scheduledLaunchAt?: string | null }>;
}

interface AdminUser { id: string; username: string; email: string; fullName?: string | null; createdAt: string; isBanned: boolean }
interface FraudSummary { totalSignals: number; rapidVoterCount: number; productBurstCount: number }

export default function AdminDashboard() {
  const { data: session } = useSession();
  const [dashboard, setDashboard] = useState<DashboardData | null>(null);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [openReportCount, setOpenReportCount] = useState(0);
  const [fraud, setFraud] = useState<FraudSummary>({ totalSignals: 0, rapidVoterCount: 0, productBurstCount: 0 });
  const [error, setError] = useState("");

  useEffect(() => {
    const token = session?.accessToken;
    if (!token) return;
    let active = true;
    const headers = { Authorization: `Bearer ${token}` };

    void Promise.all([
      fetch(`${API_URL}/api/products/admin/dashboard`, { headers }),
      fetch(`${API_URL}/api/auth/admin/users`, { headers }),
      fetch(`${API_URL}/api/auth/admin/moderation/reports?status=Open`, { headers }),
      fetch(`${API_URL}/api/votes/admin/fraud-signals?hours=24`, { headers }),
    ]).then(async ([dashboardResponse, usersResponse, reportsResponse, fraudResponse]) => {
      if (!dashboardResponse.ok) throw new Error("Dashboard metrikleri alınamadı.");
      const dashboardPayload = await dashboardResponse.json() as DashboardData;
      const userPayload = usersResponse.ok ? await usersResponse.json() as AdminUser[] : [];
      const reportPayload = reportsResponse.ok ? await reportsResponse.json() as unknown[] : [];
      const fraudPayload = fraudResponse.ok ? await fraudResponse.json() as { summary?: FraudSummary } : {};
      if (!active) return;
      setDashboard(dashboardPayload);
      setUsers(userPayload);
      setOpenReportCount(reportPayload.length);
      setFraud(fraudPayload.summary ?? { totalSignals: 0, rapidVoterCount: 0, productBurstCount: 0 });
    }).catch((caught) => {
      if (active) setError(caught instanceof Error ? caught.message : "Admin verileri alınamadı.");
    });

    return () => { active = false; };
  }, [session?.accessToken]);

  const maxDailyCount = useMemo(() => Math.max(1, ...(dashboard?.submissionSeries.map((item) => item.count) ?? [1])), [dashboard]);

  if (!dashboard && !error) return <div className="flex min-h-[50vh] items-center justify-center"><Loader2 className="h-7 w-7 animate-spin text-emerald-600" /></div>;
  if (!dashboard) return <div className="rounded-2xl border border-destructive/20 bg-destructive/10 p-5 text-destructive">{error}</div>;

  const stats = [
    { title: "Toplam ürün", value: dashboard.totalProducts, icon: Package, desc: `${dashboard.publishedProducts} yayında` },
    { title: "Kullanıcı", value: users.length, icon: Users, desc: `${users.filter((user) => user.isBanned).length} aktif ban` },
    { title: "Toplam görüntülenme", value: dashboard.totalViews.toLocaleString("tr-TR"), icon: Eye, desc: `${dashboard.totalFollowers} ürün takibi` },
    { title: "Bugünkü lansman", value: dashboard.launchesToday, icon: Rocket, desc: `${dashboard.scheduledProducts} planlı` },
  ];

  return <div className="space-y-7">
    <div className="flex flex-wrap items-end justify-between gap-4"><div><h1 className="text-3xl font-bold tracking-tight">Operasyon dashboard</h1><p className="mt-1 text-muted-foreground">Canlı ürün, kullanıcı, lansman ve güvenlik metrikleri.</p></div><p className="text-xs text-muted-foreground">Güncellendi: {new Date(dashboard.generatedAtUtc).toLocaleString("tr-TR")}</p></div>
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">{stats.map((stat) => <Card key={stat.title}><CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2"><CardTitle className="text-sm font-medium">{stat.title}</CardTitle><stat.icon className="h-4 w-4 text-muted-foreground" /></CardHeader><CardContent><div className="text-3xl font-bold">{stat.value}</div><p className="mt-1 text-xs text-muted-foreground">{stat.desc}</p></CardContent></Card>)}</div>
    <div className="grid gap-4 md:grid-cols-3"><Link href="/admin/curation" className="rounded-2xl border bg-card p-5 transition hover:border-emerald-500/50"><UserRoundCheck className="h-5 w-5 text-emerald-600" /><p className="mt-3 text-2xl font-bold">{dashboard.pendingProducts}</p><p className="text-sm text-muted-foreground">Curation bekleyen ürün</p></Link><Link href="/admin/moderation" className="rounded-2xl border bg-card p-5 transition hover:border-amber-500/50"><Activity className="h-5 w-5 text-amber-600" /><p className="mt-3 text-2xl font-bold">{openReportCount}</p><p className="text-sm text-muted-foreground">Açık moderasyon raporu</p></Link><Link href="/admin/fraud" className="rounded-2xl border bg-card p-5 transition hover:border-rose-500/50"><AlertTriangle className="h-5 w-5 text-rose-600" /><p className="mt-3 text-2xl font-bold">{fraud.totalSignals}</p><p className="text-sm text-muted-foreground">Son 24 saat fraud sinyali</p></Link></div>
    <div className="grid gap-4 lg:grid-cols-7">
      <Card className="lg:col-span-4"><CardHeader><CardTitle>30 günlük gönderim akışı</CardTitle><CardDescription>Her sütun o gün oluşturulan ürün sayısını gösterir.</CardDescription></CardHeader><CardContent><div className="flex h-64 items-end gap-1 border-b pb-1">{dashboard.submissionSeries.map((item, index) => <div key={item.date} className="group relative flex h-full flex-1 items-end"><div className="w-full min-h-1 rounded-t bg-emerald-500/80 transition group-hover:bg-emerald-500" style={{ height: `${Math.max(2, item.count / maxDailyCount * 100)}%` }} /><span className="pointer-events-none absolute bottom-full left-1/2 mb-2 hidden -translate-x-1/2 whitespace-nowrap rounded bg-foreground px-2 py-1 text-[10px] text-background group-hover:block">{new Date(item.date).toLocaleDateString("tr-TR")}: {item.count}</span>{index % 7 === 0 && <span className="absolute top-full mt-2 text-[9px] text-muted-foreground">{new Date(item.date).toLocaleDateString("tr-TR", { day: "2-digit", month: "2-digit" })}</span>}</div>)}</div></CardContent></Card>
      <Card className="lg:col-span-3"><CardHeader><CardTitle>Son kullanıcılar</CardTitle><CardDescription>En yeni kayıtlar.</CardDescription></CardHeader><CardContent className="space-y-4">{users.slice(0, 6).map((user) => <div key={user.id} className="flex items-center justify-between gap-3"><div className="min-w-0"><p className="truncate text-sm font-semibold">{user.fullName || `@${user.username}`}</p><p className="truncate text-xs text-muted-foreground">{user.email}</p></div><span className="shrink-0 text-[11px] text-muted-foreground">{new Date(user.createdAt).toLocaleDateString("tr-TR")}</span></div>)}</CardContent></Card>
    </div>
    <Card><CardHeader><CardTitle>Son ürünler</CardTitle><CardDescription>İşlem görmesi muhtemel en yeni kayıtlar.</CardDescription></CardHeader><CardContent className="divide-y">{dashboard.recentProducts.map((product) => <div key={product.id} className="flex items-center justify-between gap-4 py-3"><div><Link href={`/product/${product.slug}`} className="font-semibold hover:text-emerald-700 hover:underline">{product.name}</Link><p className="text-xs text-muted-foreground">{new Date(product.createdAt).toLocaleString("tr-TR")}</p></div><span className="rounded-full bg-muted px-2.5 py-1 text-xs font-semibold">{statusLabel(product.status)}</span></div>)}</CardContent></Card>
  </div>;
}

function statusLabel(status: number) { return ["Taslak", "İncelemede", "Yayında", "Reddedildi", "Arşivlendi", "Planlandı"][status] ?? "Bilinmiyor"; }
