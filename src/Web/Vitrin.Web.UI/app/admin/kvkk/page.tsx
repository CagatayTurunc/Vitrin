"use client";

import { useCallback, useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import {
  ShieldCheck, Loader2, AlertTriangle, RefreshCw,
  Clock, CheckCircle2, UserX, Download,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

interface PendingDeletionUser {
  id: string;
  username: string;
  email: string;
  fullName: string;
  deleteRequestedAtUtc: string;
  scheduledDeletionAt: string;   // +30 gün
  daysRemaining: number;
  isOverdue: boolean;
}

interface KvkkStats {
  totalPendingDeletions: number;
  overdueDeletions: number;
  anonymizedThisMonth: number;
  anonymizedTotal: number;
}

export default function KvkkAdminPage() {
  const { data: session } = useSession();
  const token = session?.accessToken;

  const [users, setUsers] = useState<PendingDeletionUser[]>([]);
  const [stats, setStats] = useState<KvkkStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!token) return;
    setIsLoading(true);
    setError(null);
    try {
      const res = await fetch(`${API_URL}/api/auth/admin/kvkk/pending-deletions`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error("KVKK verileri yüklenemedi.");
      const data = await res.json() as { users: PendingDeletionUser[]; stats: KvkkStats };
      setUsers(data.users);
      setStats(data.stats);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Veri yüklenemedi.");
    } finally {
      setIsLoading(false);
    }
  }, [token]);

  useEffect(() => { void load(); }, [load]);

  async function exportCsv() {
    if (!token) return;
    try {
      const res = await fetch(`${API_URL}/api/auth/admin/kvkk/pending-deletions/export`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error("Export başarısız.");
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `kvkk-silme-talepleri-${new Date().toISOString().slice(0, 10)}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      alert(e instanceof Error ? e.message : "Hata oluştu.");
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-7 w-7 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <ShieldCheck className="h-6 w-6 text-emerald-500" />
            KVKK Veri Yönetimi
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Bekleyen silme talepleri ve retention akışı. RetentionCleanupWorker her gece 03:00 UTC&apos;de otomatik çalışır.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => void load()}>
            <RefreshCw className="mr-1.5 h-4 w-4" /> Yenile
          </Button>
          <Button variant="outline" size="sm" onClick={() => void exportCsv()}>
            <Download className="mr-1.5 h-4 w-4" /> CSV Export
          </Button>
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
          <AlertTriangle className="h-4 w-4 shrink-0" />
          {error}
          <Button variant="outline" size="sm" className="ml-auto" onClick={() => void load()}>
            Tekrar dene
          </Button>
        </div>
      )}

      {/* Stats */}
      {stats && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                <Clock className="h-3.5 w-3.5" /> Bekleyen Talep
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{stats.totalPendingDeletions}</p>
              <p className="text-xs text-muted-foreground mt-1">30 günlük süre dolmamış</p>
            </CardContent>
          </Card>
          <Card className={stats.overdueDeletions > 0 ? "border-red-200 bg-red-50/30 dark:border-red-900/30" : ""}>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                <AlertTriangle className={`h-3.5 w-3.5 ${stats.overdueDeletions > 0 ? "text-red-500" : ""}`} />
                Süresi Geçmiş
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className={`text-2xl font-bold ${stats.overdueDeletions > 0 ? "text-red-500" : ""}`}>
                {stats.overdueDeletions}
              </p>
              <p className="text-xs text-muted-foreground mt-1">
                {stats.overdueDeletions > 0 ? "Anonim hale getirilmesi gerekiyor" : "Tüm talepler zamanında işlendi"}
              </p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" /> Bu Ay Anonimleşen
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{stats.anonymizedThisMonth}</p>
              <p className="text-xs text-muted-foreground mt-1">Başarıyla işlendi</p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                <UserX className="h-3.5 w-3.5" /> Toplam Anonimleşen
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{stats.anonymizedTotal}</p>
              <p className="text-xs text-muted-foreground mt-1">Tüm zamanlar</p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Worker açıklaması */}
      <div className="rounded-xl border border-border bg-muted/20 p-4 text-xs text-muted-foreground">
        <p className="mb-1 font-semibold text-foreground">Otomatik retention akışı</p>
        <ul className="space-y-1">
          <li>• Kullanıcı <strong>Ayarlar → Hesabı Sil</strong> butonuna tıklar → <code className="rounded bg-muted px-1">DeleteRequestedAtUtc</code> set edilir.</li>
          <li>• Kullanıcı 30 gün içinde talebi iptal edebilir.</li>
          <li>• <code className="rounded bg-muted px-1">RetentionCleanupWorker</code> her gece UTC 03:00&apos;da çalışır, süresi dolmuş talepleri <code className="rounded bg-muted px-1">User.Anonymize()</code> ile işler.</li>
          <li>• Anonim hale getirilen kullanıcıda e-posta, parola, OAuth ID&apos;leri ve profil bilgileri silinir. Oylar ve yorumlar istatistiksel veri olarak korunur.</li>
          <li>• Süresi geçmiş talep varsa worker bir sonraki geceye kadar beklemeden işler.</li>
        </ul>
      </div>

      {/* Pending deletions table */}
      <div>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-muted-foreground">
          Bekleyen Silme Talepleri
        </h2>
        {users.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-border bg-muted/10 p-12 text-center">
            <CheckCircle2 className="mx-auto mb-3 h-10 w-10 text-emerald-500 opacity-50" />
            <p className="text-sm font-medium">Bekleyen silme talebi yok.</p>
            <p className="mt-1 text-xs text-muted-foreground">Tüm talepler işlenmiş ya da henüz talep yok.</p>
          </div>
        ) : (
          <div className="overflow-hidden rounded-2xl border border-border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/30">
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground">Kullanıcı</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground hidden md:table-cell">E-posta</th>
                  <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wider text-muted-foreground">Talep Tarihi</th>
                  <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wider text-muted-foreground">Silinecek Tarih</th>
                  <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wider text-muted-foreground">Kalan Süre</th>
                  <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wider text-muted-foreground">Durum</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user, idx) => (
                  <tr
                    key={user.id}
                    className={`border-b border-border/50 ${
                      user.isOverdue
                        ? "bg-red-50/30 dark:bg-red-950/10"
                        : idx % 2 === 0
                        ? "bg-background"
                        : "bg-muted/5"
                    }`}
                  >
                    <td className="px-4 py-3">
                      <div>
                        <p className="font-medium">{user.fullName || user.username}</p>
                        <p className="text-xs text-muted-foreground">@{user.username}</p>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground hidden md:table-cell text-xs">
                      {user.email}
                    </td>
                    <td className="px-4 py-3 text-center text-xs text-muted-foreground">
                      {new Date(user.deleteRequestedAtUtc).toLocaleDateString("tr-TR")}
                    </td>
                    <td className="px-4 py-3 text-center text-xs font-medium">
                      {new Date(user.scheduledDeletionAt).toLocaleDateString("tr-TR")}
                    </td>
                    <td className="px-4 py-3 text-center">
                      {user.isOverdue ? (
                        <span className="rounded-full bg-red-500/10 px-2 py-0.5 text-xs font-semibold text-red-500">
                          Geçti
                        </span>
                      ) : (
                        <span className="rounded-full bg-amber-500/10 px-2 py-0.5 text-xs font-semibold text-amber-600">
                          {user.daysRemaining} gün
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-center">
                      {user.isOverdue ? (
                        <span className="flex items-center justify-center gap-1 text-xs text-red-500">
                          <AlertTriangle className="h-3.5 w-3.5" /> İşlenecek
                        </span>
                      ) : (
                        <span className="flex items-center justify-center gap-1 text-xs text-muted-foreground">
                          <Clock className="h-3.5 w-3.5" /> Bekleniyor
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
