"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useSession } from "next-auth/react";
import {
  Ban,
  CheckCircle2,
  FileClock,
  Flag,
  Gavel,
  Loader2,
  RotateCcw,
  Search,
  ShieldAlert,
  XCircle,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

type Tab = "reports" | "appeals" | "bans" | "audit";

interface ReportItem {
  id: string;
  reporterUserId: string;
  reporterUsername?: string | null;
  targetType: number;
  targetId: string;
  targetOwnerUserId?: string | null;
  targetOwnerUsername?: string | null;
  category: number;
  details: string;
  status: number;
  createdAtUtc: string;
  resolution?: string | null;
}

interface AppealItem {
  id: string;
  banId: string;
  userId: string;
  username?: string | null;
  statement: string;
  status: number;
  createdAtUtc: string;
  reviewNote?: string | null;
}

interface BanItem {
  id: string;
  userId: string;
  username?: string | null;
  reason: string;
  createdAtUtc: string;
  expiresAtUtc?: string | null;
}

interface AuditItem {
  id: string;
  action: string;
  actorUserId?: string | null;
  resourceType: string;
  resourceId?: string | null;
  outcome: string;
  details?: string | null;
  occurredAtUtc: string;
}

interface AdminUser {
  id: string;
  username: string;
  fullName?: string | null;
  role: number;
  isBanned: boolean;
}

const targetLabels = ["Yorum", "Ürün", "Kullanıcı"];
const categoryLabels = ["Spam", "Taciz", "Nefret", "Yanlış bilgi", "Yasadışı", "Diğer"];
const tabs: Array<{ value: Tab; label: string; icon: typeof Flag }> = [
  { value: "reports", label: "Raporlar", icon: Flag },
  { value: "appeals", label: "İtirazlar", icon: RotateCcw },
  { value: "bans", label: "Banlar", icon: Ban },
  { value: "audit", label: "Audit log", icon: FileClock },
];

function formatDate(value: string) {
  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default function ModerationPage() {
  const { data: session } = useSession();
  const [tab, setTab] = useState<Tab>("reports");
  const [reports, setReports] = useState<ReportItem[]>([]);
  const [appeals, setAppeals] = useState<AppealItem[]>([]);
  const [bans, setBans] = useState<BanItem[]>([]);
  const [audit, setAudit] = useState<AuditItem[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [query, setQuery] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [resolution, setResolution] = useState("Topluluk kuralları ihlali doğrulandı.");
  const [newBan, setNewBan] = useState({ userId: "", reason: "", durationDays: "7" });

  const authHeaders = useCallback(() => ({
    "Content-Type": "application/json",
    Authorization: `Bearer ${session?.accessToken ?? ""}`,
  }), [session?.accessToken]);

  const load = useCallback(async () => {
    if (!session?.accessToken) return;
    setIsLoading(true);
    try {
      const headers = authHeaders();
      const [reportsResponse, appealsResponse, bansResponse, auditResponse, commentAuditResponse, usersResponse] = await Promise.all([
        fetch(`${API_URL}/api/auth/admin/moderation/reports`, { headers }),
        fetch(`${API_URL}/api/auth/admin/moderation/appeals`, { headers }),
        fetch(`${API_URL}/api/auth/admin/moderation/bans`, { headers }),
        fetch(`${API_URL}/api/auth/admin/moderation/audit`, { headers }),
        fetch(`${API_URL}/api/comments/admin/audit`, { headers }),
        fetch(`${API_URL}/api/auth/admin/users`, { headers }),
      ]);

      const [reportData, appealData, banData, auditData, commentAuditData, userData] = await Promise.all([
        reportsResponse.ok ? reportsResponse.json() as Promise<ReportItem[]> : [],
        appealsResponse.ok ? appealsResponse.json() as Promise<AppealItem[]> : [],
        bansResponse.ok ? bansResponse.json() as Promise<BanItem[]> : [],
        auditResponse.ok ? auditResponse.json() as Promise<AuditItem[]> : [],
        commentAuditResponse.ok ? commentAuditResponse.json() as Promise<AuditItem[]> : [],
        usersResponse.ok ? usersResponse.json() as Promise<AdminUser[]> : [],
      ]);
      setReports(reportData);
      setAppeals(appealData);
      setBans(banData);
      setAudit([...auditData, ...commentAuditData].sort((a, b) =>
        new Date(b.occurredAtUtc).getTime() - new Date(a.occurredAtUtc).getTime()));
      setUsers(userData);
    } finally {
      setIsLoading(false);
    }
  }, [authHeaders, session?.accessToken]);

  useEffect(() => {
    // Initial API synchronization happens after the authenticated session is available.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const resolveReport = async (report: ReportItem, mode: "dismiss" | "hide" | "ban7" | "ban30" | "banPermanent") => {
    if (!session?.accessToken) return;
    setBusyId(report.id);
    try {
      if (report.targetType === 0 && mode !== "dismiss") {
        await fetch(`${API_URL}/api/comments/admin/${report.targetId}/moderation`, {
          method: "PATCH",
          headers: authHeaders(),
          body: JSON.stringify({ status: "Hidden", reason: resolution }),
        });
      }
      const banDays = mode === "ban7" ? 7 : mode === "ban30" ? 30 : mode === "banPermanent" ? 0 : null;
      const response = await fetch(`${API_URL}/api/auth/admin/moderation/reports/${report.id}`, {
        method: "PATCH",
        headers: authHeaders(),
        body: JSON.stringify({
          resolution: mode === "dismiss" ? "İhlal doğrulanamadı; rapor kapatıldı." : resolution,
          dismissed: mode === "dismiss",
          banDays,
        }),
      });
      if (response.ok) await load();
    } finally {
      setBusyId(null);
    }
  };

  const reviewAppeal = async (appeal: AppealItem, approved: boolean) => {
    setBusyId(appeal.id);
    try {
      const response = await fetch(`${API_URL}/api/auth/admin/moderation/appeals/${appeal.id}`, {
        method: "PATCH",
        headers: authHeaders(),
        body: JSON.stringify({ approved, note: approved ? "İtiraz bağlamı incelendi ve kabul edildi." : "Mevcut yaptırım kararı geçerliliğini koruyor." }),
      });
      if (response.ok) await load();
    } finally {
      setBusyId(null);
    }
  };

  const createBan = async () => {
    if (!newBan.userId || newBan.reason.trim().length < 5) return;
    setBusyId("new-ban");
    try {
      const durationDays = newBan.durationDays === "permanent" ? null : Number(newBan.durationDays);
      const response = await fetch(`${API_URL}/api/auth/admin/moderation/bans`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ userId: newBan.userId, reason: newBan.reason, durationDays }),
      });
      if (response.ok) {
        setNewBan({ userId: "", reason: "", durationDays: "7" });
        await load();
      }
    } finally {
      setBusyId(null);
    }
  };

  const revokeBan = async (ban: BanItem) => {
    setBusyId(ban.id);
    try {
      const response = await fetch(`${API_URL}/api/auth/admin/moderation/bans/${ban.id}`, {
        method: "DELETE",
        headers: authHeaders(),
        body: JSON.stringify({ reason: "Moderatör tarafından yeniden değerlendirildi." }),
      });
      if (response.ok) await load();
    } finally {
      setBusyId(null);
    }
  };

  const filteredAudit = useMemo(() => audit.filter((item) => {
    const text = `${item.action} ${item.resourceType} ${item.resourceId} ${item.details}`.toLowerCase();
    return text.includes(query.toLowerCase());
  }), [audit, query]);

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <section className="relative overflow-hidden rounded-3xl border bg-card p-7 shadow-sm">
        <div className="absolute -right-16 -top-20 h-64 w-64 rounded-full bg-rose-500/10 blur-3xl" />
        <div className="relative flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <div className="mb-3 inline-flex items-center gap-2 rounded-full bg-rose-500/10 px-3 py-1 text-xs font-bold text-rose-500">
              <ShieldAlert className="h-3.5 w-3.5" /> TRUST & SAFETY
            </div>
            <h1 className="text-3xl font-black tracking-tight">Moderasyon merkezi</h1>
            <p className="mt-2 max-w-2xl text-muted-foreground">Raporları değerlendir, yaptırımları yönet, itirazları incele ve bütün karar zincirini denetlenebilir tut.</p>
          </div>
          <div className="grid grid-cols-3 gap-2 text-center">
            <div className="rounded-2xl border bg-background/70 px-4 py-3"><p className="text-2xl font-black">{reports.filter((item) => item.status < 2).length}</p><p className="text-[11px] text-muted-foreground">Açık rapor</p></div>
            <div className="rounded-2xl border bg-background/70 px-4 py-3"><p className="text-2xl font-black">{appeals.filter((item) => item.status === 0).length}</p><p className="text-[11px] text-muted-foreground">İtiraz</p></div>
            <div className="rounded-2xl border bg-background/70 px-4 py-3"><p className="text-2xl font-black">{bans.length}</p><p className="text-[11px] text-muted-foreground">Aktif ban</p></div>
          </div>
        </div>
      </section>

      <div className="flex flex-wrap gap-2 rounded-2xl border bg-card p-2">
        {tabs.map((item) => {
          const Icon = item.icon;
          return <button key={item.value} onClick={() => setTab(item.value)} className={`inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold transition-colors ${tab === item.value ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-muted"}`}><Icon className="h-4 w-4" />{item.label}</button>;
        })}
      </div>

      {isLoading ? (
        <div className="flex h-64 items-center justify-center rounded-3xl border bg-card text-muted-foreground"><Loader2 className="mr-2 h-5 w-5 animate-spin" /> Moderasyon verileri yükleniyor...</div>
      ) : tab === "reports" ? (
        <div className="grid gap-4">
          <div className="rounded-2xl border bg-card p-4">
            <label className="text-xs font-semibold text-muted-foreground">Karar notu</label>
            <Input value={resolution} onChange={(event) => setResolution(event.target.value)} className="mt-2" />
          </div>
          {reports.length === 0 ? <EmptyState text="Henüz rapor yok." /> : reports.map((report) => (
            <article key={report.id} className="rounded-3xl border bg-card p-5 shadow-sm">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge variant={report.status < 2 ? "destructive" : "outline"}>{report.status < 2 ? "İnceleme bekliyor" : "Kapalı"}</Badge>
                    <Badge variant="secondary">{targetLabels[report.targetType]}</Badge>
                    <Badge variant="outline">{categoryLabels[report.category]}</Badge>
                    <span className="text-xs text-muted-foreground">{formatDate(report.createdAtUtc)}</span>
                  </div>
                  <p className="mt-4 leading-7">{report.details}</p>
                  <p className="mt-3 text-xs text-muted-foreground">Raporlayan: @{report.reporterUsername || report.reporterUserId.slice(0, 8)} · Hedef: @{report.targetOwnerUsername || report.targetOwnerUserId?.slice(0, 8) || "bilinmiyor"}</p>
                  {report.resolution && <p className="mt-3 rounded-xl bg-muted p-3 text-sm"><strong>Karar:</strong> {report.resolution}</p>}
                </div>
                {report.status < 2 && <div className="flex min-w-56 flex-wrap gap-2 lg:justify-end">
                  <Button size="sm" variant="outline" disabled={busyId === report.id} onClick={() => resolveReport(report, "dismiss")}><XCircle className="mr-1 h-4 w-4" /> Kapat</Button>
                  <Button size="sm" disabled={busyId === report.id} onClick={() => resolveReport(report, "hide")}><Gavel className="mr-1 h-4 w-4" /> İçeriği gizle</Button>
                  {report.targetOwnerUserId && <>
                    <Button size="sm" variant="destructive" disabled={busyId === report.id} onClick={() => resolveReport(report, "ban7")}>7 gün ban</Button>
                    <Button size="sm" variant="destructive" disabled={busyId === report.id} onClick={() => resolveReport(report, "ban30")}>30 gün ban</Button>
                  </>}
                </div>}
              </div>
            </article>
          ))}
        </div>
      ) : tab === "appeals" ? (
        <div className="grid gap-4">
          {appeals.length === 0 ? <EmptyState text="Henüz itiraz yok." /> : appeals.map((appeal) => (
            <article key={appeal.id} className="rounded-3xl border bg-card p-5">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div><div className="flex items-center gap-2"><Badge variant={appeal.status === 0 ? "destructive" : "outline"}>{appeal.status === 0 ? "Bekliyor" : appeal.status === 1 ? "Kabul" : "Red"}</Badge><span className="text-sm font-semibold">@{appeal.username}</span><span className="text-xs text-muted-foreground">{formatDate(appeal.createdAtUtc)}</span></div><p className="mt-4 leading-7">{appeal.statement}</p>{appeal.reviewNote && <p className="mt-3 rounded-xl bg-muted p-3 text-sm">{appeal.reviewNote}</p>}</div>
                {appeal.status === 0 && <div className="flex gap-2"><Button size="sm" variant="outline" disabled={busyId === appeal.id} onClick={() => reviewAppeal(appeal, false)}><XCircle className="mr-1 h-4 w-4" /> Reddet</Button><Button size="sm" disabled={busyId === appeal.id} onClick={() => reviewAppeal(appeal, true)}><CheckCircle2 className="mr-1 h-4 w-4" /> Kabul et</Button></div>}
              </div>
            </article>
          ))}
        </div>
      ) : tab === "bans" ? (
        <div className="grid gap-5 lg:grid-cols-[360px_1fr]">
          <div className="h-fit rounded-3xl border bg-card p-5">
            <h2 className="font-bold">Yeni yaptırım</h2>
            <div className="mt-4 space-y-3">
              <select value={newBan.userId} onChange={(event) => setNewBan((current) => ({ ...current, userId: event.target.value }))} className="h-11 w-full rounded-xl border bg-background px-3 text-sm">
                <option value="">Kullanıcı seçin</option>
                {users.filter((user) => user.role !== 2 && !user.isBanned).map((user) => <option key={user.id} value={user.id}>@{user.username} · {user.fullName}</option>)}
              </select>
              <select value={newBan.durationDays} onChange={(event) => setNewBan((current) => ({ ...current, durationDays: event.target.value }))} className="h-11 w-full rounded-xl border bg-background px-3 text-sm"><option value="1">1 gün</option><option value="7">7 gün</option><option value="30">30 gün</option><option value="permanent">Kalıcı</option></select>
              <Textarea value={newBan.reason} onChange={(event) => setNewBan((current) => ({ ...current, reason: event.target.value }))} placeholder="Yaptırım gerekçesi..." className="min-h-28" />
              <Button className="w-full" variant="destructive" onClick={createBan} disabled={busyId === "new-ban" || !newBan.userId || newBan.reason.trim().length < 5}><Ban className="mr-2 h-4 w-4" /> Ban uygula</Button>
            </div>
          </div>
          <div className="grid gap-3">
            {bans.length === 0 ? <EmptyState text="Aktif ban yok." /> : bans.map((ban) => <article key={ban.id} className="flex flex-col gap-3 rounded-3xl border bg-card p-5 sm:flex-row sm:items-center sm:justify-between"><div><div className="flex items-center gap-2"><Badge variant="destructive">Aktif ban</Badge><span className="font-semibold">@{ban.username}</span></div><p className="mt-2 text-sm">{ban.reason}</p><p className="mt-2 text-xs text-muted-foreground">{ban.expiresAtUtc ? `${formatDate(ban.expiresAtUtc)} tarihine kadar` : "Kalıcı"} · {formatDate(ban.createdAtUtc)}</p></div><Button size="sm" variant="outline" disabled={busyId === ban.id} onClick={() => revokeBan(ban)}><RotateCcw className="mr-1 h-4 w-4" /> Kaldır</Button></article>)}
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          <div className="relative"><Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" /><Input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Aksiyon, kaynak veya detay ara..." className="pl-10" /></div>
          <div className="overflow-hidden rounded-3xl border bg-card">
            {filteredAudit.length === 0 ? <div className="p-10 text-center text-muted-foreground">Audit kaydı bulunamadı.</div> : filteredAudit.map((item, index) => <div key={`${item.id}-${index}`} className={`grid gap-2 p-4 text-sm md:grid-cols-[190px_1fr_160px] ${index !== filteredAudit.length - 1 ? "border-b" : ""}`}><div><p className="font-mono text-xs font-semibold text-primary">{item.action}</p><p className="mt-1 text-xs text-muted-foreground">{formatDate(item.occurredAtUtc)}</p></div><div><p className="font-medium">{item.resourceType} · {item.resourceId?.slice(0, 12)}</p>{item.details && <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">{item.details}</p>}</div><div className="md:text-right"><Badge variant={item.outcome === "Succeeded" ? "secondary" : "destructive"}>{item.outcome}</Badge><p className="mt-1 text-xs text-muted-foreground">{item.actorUserId?.slice(0, 8) || "system"}</p></div></div>)}
          </div>
        </div>
      )}
    </div>
  );
}

function EmptyState({ text }: { text: string }) {
  return <div className="flex min-h-44 flex-col items-center justify-center rounded-3xl border border-dashed bg-card text-muted-foreground"><ShieldAlert className="mb-3 h-8 w-8 opacity-40" /><p>{text}</p></div>;
}
