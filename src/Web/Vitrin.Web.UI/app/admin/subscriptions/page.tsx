"use client";

import { useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import {
  Crown, Building2, Sparkles, Search, TrendingUp,
  CreditCard, AlertTriangle, CheckCircle, XCircle,
  Loader2, RefreshCw, ChevronLeft, ChevronRight
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

interface SubscriptionRecord {
  id: string;
  userId: string;
  userEmail: string;
  userFullName: string;
  tier: "Free" | "ProMaker" | "Enterprise";
  status: "Active" | "Trialing" | "PastDue" | "Canceled" | "Expired" | "Paused";
  currentPeriodStart: string;
  currentPeriodEnd: string;
  cancelAtPeriodEnd: boolean;
  createdAt: string;
}

interface PaymentRecord {
  id: string;
  userId: string;
  userEmail: string;
  amount: number;
  currency: string;
  status: "Succeeded" | "Failed" | "Refunded" | "Pending";
  billingDate: string;
  iyzicoPaymentId?: string;
}

interface SubscriptionStats {
  totalActive: number;
  totalPro: number;
  totalEnterprise: number;
  totalCanceled: number;
  mrr: number;
  churnRate: number;
}

const TIER_CONFIG = {
  Free: { label: "Ücretsiz", icon: Sparkles, color: "text-muted-foreground", bg: "bg-muted" },
  ProMaker: { label: "🏆 Pro", icon: Crown, color: "text-blue-500", bg: "bg-blue-500/10" },
  Enterprise: { label: "💎 Enterprise", icon: Building2, color: "text-amber-500", bg: "bg-amber-500/10" },
};

const STATUS_CONFIG = {
  Active: { label: "Aktif", color: "text-emerald-500", bg: "bg-emerald-500/10" },
  Trialing: { label: "Deneme", color: "text-blue-500", bg: "bg-blue-500/10" },
  PastDue: { label: "Ödeme Bekliyor", color: "text-amber-500", bg: "bg-amber-500/10" },
  Canceled: { label: "İptal Edildi", color: "text-red-500", bg: "bg-red-500/10" },
  Expired: { label: "Sona Erdi", color: "text-muted-foreground", bg: "bg-muted" },
  Paused: { label: "Duraklatıldı", color: "text-orange-500", bg: "bg-orange-500/10" },
};

const PAGE_SIZE = 20;

export default function AdminSubscriptionsPage() {
  const { data: session } = useSession();
  const [subscriptions, setSubscriptions] = useState<SubscriptionRecord[]>([]);
  const [payments, setPayments] = useState<PaymentRecord[]>([]);
  const [stats, setStats] = useState<SubscriptionStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [tierFilter, setTierFilter] = useState<string>("all");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [activeTab, setActiveTab] = useState<"subscriptions" | "payments">("subscriptions");
  const [page, setPage] = useState(1);

  const fetchData = async () => {
    const token = session?.accessToken;
    if (!token) return;
    setLoading(true);

    try {
      const headers = { Authorization: `Bearer ${token}` };
      const [subRes, payRes, statsRes] = await Promise.all([
        fetch(`${API_URL}/api/subscription/admin/list`, { headers }),
        fetch(`${API_URL}/api/subscription/admin/payments`, { headers }),
        fetch(`${API_URL}/api/subscription/admin/stats`, { headers }),
      ]);

      if (subRes.ok) setSubscriptions(await subRes.json() as SubscriptionRecord[]);
      if (payRes.ok) setPayments(await payRes.json() as PaymentRecord[]);
      if (statsRes.ok) setStats(await statsRes.json() as SubscriptionStats);
    } catch {
      // Sessiz hata — placeholder veri göster
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void fetchData(); }, [session?.accessToken]);

  const filteredSubs = subscriptions.filter((s) => {
    const matchSearch = !search ||
      s.userEmail.toLowerCase().includes(search.toLowerCase()) ||
      s.userFullName.toLowerCase().includes(search.toLowerCase());
    const matchTier = tierFilter === "all" || s.tier === tierFilter;
    const matchStatus = statusFilter === "all" || s.status === statusFilter;
    return matchSearch && matchTier && matchStatus;
  });

  const pagedSubs = filteredSubs.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const totalPages = Math.ceil(filteredSubs.length / PAGE_SIZE);

  const filteredPayments = payments.filter((p) =>
    !search || p.userEmail.toLowerCase().includes(search.toLowerCase())
  );

  if (loading) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <Loader2 className="h-7 w-7 animate-spin text-emerald-600" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Abonelik Yönetimi</h1>
          <p className="mt-1 text-muted-foreground">Abonelikler, ödemeler ve gelir analizi</p>
        </div>
        <button
          onClick={() => void fetchData()}
          className="flex items-center gap-2 px-3 py-2 rounded-lg border hover:bg-muted transition-colors text-sm"
        >
          <RefreshCw className="w-4 h-4" />
          Yenile
        </button>
      </div>

      {/* Stats */}
      {stats ? (
        <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-4">
          {[
            { label: "Aktif Abonelik", value: stats.totalActive, icon: CheckCircle, color: "text-emerald-500" },
            { label: "Pro Maker", value: stats.totalPro, icon: Crown, color: "text-blue-500" },
            { label: "Enterprise", value: stats.totalEnterprise, icon: Building2, color: "text-amber-500" },
            { label: "İptal Edilen", value: stats.totalCanceled, icon: XCircle, color: "text-red-500" },
            { label: "MRR (₺)", value: `₺${stats.mrr.toLocaleString("tr-TR")}`, icon: TrendingUp, color: "text-emerald-500" },
            { label: "Churn Rate", value: `%${stats.churnRate.toFixed(1)}`, icon: AlertTriangle, color: "text-amber-500" },
          ].map((stat) => (
            <Card key={stat.label}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-xs font-medium text-muted-foreground">{stat.label}</CardTitle>
                <stat.icon className={`h-4 w-4 ${stat.color}`} />
              </CardHeader>
              <CardContent>
                <div className={`text-2xl font-bold ${stat.color}`}>{stat.value}</div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        /* Placeholder stats — backend endpoint henüz yoksa */
        <div className="rounded-xl border border-amber-500/20 bg-amber-500/5 px-4 py-3 text-sm text-amber-500">
          ⚠️ Abonelik istatistikleri yüklenemedi. Backend endpoint'lerinin hazır olduğundan emin olun.
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-1 p-1 bg-muted rounded-lg w-fit">
        {(["subscriptions", "payments"] as const).map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors ${
              activeTab === tab ? "bg-background shadow-sm text-foreground" : "text-muted-foreground hover:text-foreground"
            }`}
          >
            {tab === "subscriptions" ? "Abonelikler" : "Ödemeler"}
          </button>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3">
        <div className="relative">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Email veya isim ara..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            className="pl-9 w-64"
          />
        </div>
        {activeTab === "subscriptions" && (
          <>
            <select
              value={tierFilter}
              onChange={(e) => { setTierFilter(e.target.value); setPage(1); }}
              className="px-3 py-2 rounded-lg border bg-background text-sm"
            >
              <option value="all">Tüm Planlar</option>
              <option value="Free">Ücretsiz</option>
              <option value="ProMaker">Pro Maker</option>
              <option value="Enterprise">Enterprise</option>
            </select>
            <select
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
              className="px-3 py-2 rounded-lg border bg-background text-sm"
            >
              <option value="all">Tüm Durumlar</option>
              <option value="Active">Aktif</option>
              <option value="PastDue">Ödeme Bekliyor</option>
              <option value="Canceled">İptal Edildi</option>
              <option value="Expired">Sona Erdi</option>
            </select>
          </>
        )}
        <span className="ml-auto self-center text-sm text-muted-foreground">
          {activeTab === "subscriptions" ? filteredSubs.length : filteredPayments.length} kayıt
        </span>
      </div>

      {/* Subscriptions Table */}
      {activeTab === "subscriptions" && (
        <div className="rounded-xl border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                {["Kullanıcı", "Plan", "Durum", "Dönem Sonu", "İptal"].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-muted-foreground">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y">
              {pagedSubs.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                    Kayıt bulunamadı
                  </td>
                </tr>
              ) : pagedSubs.map((sub) => {
                const tier = TIER_CONFIG[sub.tier];
                const status = STATUS_CONFIG[sub.status];
                return (
                  <tr key={sub.id} className="hover:bg-muted/30 transition-colors">
                    <td className="px-4 py-3">
                      <div className="font-medium">{sub.userFullName || "—"}</div>
                      <div className="text-xs text-muted-foreground">{sub.userEmail}</div>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md text-xs font-medium ${tier.bg} ${tier.color}`}>
                        <tier.icon className="w-3 h-3" />
                        {tier.label}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex px-2 py-0.5 rounded-md text-xs font-medium ${status.bg} ${status.color}`}>
                        {status.label}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {new Date(sub.currentPeriodEnd).toLocaleDateString("tr-TR")}
                    </td>
                    <td className="px-4 py-3">
                      {sub.cancelAtPeriodEnd ? (
                        <span className="text-xs text-amber-500">Dönem sonunda iptal</span>
                      ) : (
                        <span className="text-xs text-muted-foreground">—</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between px-4 py-3 border-t bg-muted/20">
              <span className="text-xs text-muted-foreground">
                Sayfa {page} / {totalPages}
              </span>
              <div className="flex gap-1">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="p-1.5 rounded-lg hover:bg-muted disabled:opacity-40"
                >
                  <ChevronLeft className="w-4 h-4" />
                </button>
                <button
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                  className="p-1.5 rounded-lg hover:bg-muted disabled:opacity-40"
                >
                  <ChevronRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Payments Table */}
      {activeTab === "payments" && (
        <div className="rounded-xl border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                {["Kullanıcı", "Tutar", "Durum", "Tarih", "İyzico ID"].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-muted-foreground">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y">
              {filteredPayments.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                    Ödeme kaydı bulunamadı
                  </td>
                </tr>
              ) : filteredPayments.map((payment) => (
                <tr key={payment.id} className="hover:bg-muted/30 transition-colors">
                  <td className="px-4 py-3">
                    <div className="text-xs text-muted-foreground">{payment.userEmail}</div>
                  </td>
                  <td className="px-4 py-3 font-medium">
                    ₺{payment.amount.toLocaleString("tr-TR")}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-medium ${
                      payment.status === "Succeeded" ? "bg-emerald-500/10 text-emerald-500" :
                      payment.status === "Failed" ? "bg-red-500/10 text-red-500" :
                      payment.status === "Refunded" ? "bg-blue-500/10 text-blue-500" :
                      "bg-muted text-muted-foreground"
                    }`}>
                      {payment.status === "Succeeded" ? <CheckCircle className="w-3 h-3" /> :
                       payment.status === "Failed" ? <XCircle className="w-3 h-3" /> :
                       <CreditCard className="w-3 h-3" />}
                      {payment.status === "Succeeded" ? "Başarılı" :
                       payment.status === "Failed" ? "Başarısız" :
                       payment.status === "Refunded" ? "İade Edildi" : "Bekliyor"}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(payment.billingDate).toLocaleDateString("tr-TR")}
                  </td>
                  <td className="px-4 py-3">
                    <span className="font-mono text-xs text-muted-foreground">
                      {payment.iyzicoPaymentId?.slice(0, 16) ?? "—"}…
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
