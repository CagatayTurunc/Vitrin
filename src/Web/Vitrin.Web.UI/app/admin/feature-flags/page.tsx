"use client";

import { useCallback, useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import {
  Flag, Plus, Pencil, Trash2, Loader2, Check, X,
  ToggleLeft, ToggleRight, AlertTriangle, ChevronDown, ChevronUp, Beaker,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { invalidateFeatureFlagCache } from "@/core/application/useFeatureFlags";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

interface FeatureFlag {
  id: string;
  key: string;
  description: string;
  isEnabled: boolean;
  rolloutPercentage: number;
  allowedRoles: string | null;
  variantPayload: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  updatedByUserId: string | null;
}

const EMPTY_FORM = {
  key: "",
  description: "",
  isEnabled: false,
  rolloutPercentage: 100,
  allowedRoles: "",
  variantPayload: "",
};

type FormState = typeof EMPTY_FORM;

export default function FeatureFlagsPage() {
  const { data: session } = useSession();
  const token = session?.accessToken;

  const [flags, setFlags] = useState<FeatureFlag[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [editingKey, setEditingKey] = useState<string | null>(null); // null = new
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  // Delete confirmation
  const [deletingKey, setDeletingKey] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  // Expanded rows (variant payload preview)
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  const load = useCallback(async () => {
    if (!token) return;
    setIsLoading(true);
    setError(null);
    try {
      const res = await fetch(`${API_URL}/api/admin/feature-flags`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error("Flag'ler yüklenemedi.");
      const data = (await res.json()) as FeatureFlag[];
      setFlags(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Veri yüklenemedi.");
    } finally {
      setIsLoading(false);
    }
  }, [token]);

  useEffect(() => { void load(); }, [load]);

  function openNew() {
    setEditingKey(null);
    setForm(EMPTY_FORM);
    setSaveError(null);
    setSaveSuccess(false);
    setShowForm(true);
  }

  function openEdit(flag: FeatureFlag) {
    setEditingKey(flag.key);
    setForm({
      key: flag.key,
      description: flag.description,
      isEnabled: flag.isEnabled,
      rolloutPercentage: flag.rolloutPercentage,
      allowedRoles: flag.allowedRoles ?? "",
      variantPayload: flag.variantPayload ?? "",
    });
    setSaveError(null);
    setSaveSuccess(false);
    setShowForm(true);
  }

  function cancelForm() {
    setShowForm(false);
    setEditingKey(null);
    setForm(EMPTY_FORM);
    setSaveError(null);
    setSaveSuccess(false);
  }

  async function handleSave() {
    if (!token) return;
    if (!form.key.trim()) { setSaveError("Key zorunludur."); return; }
    if (!form.description.trim()) { setSaveError("Açıklama zorunludur."); return; }

    // Variant JSON doğrulama
    if (form.variantPayload.trim()) {
      try { JSON.parse(form.variantPayload); }
      catch { setSaveError("Variant payload geçerli JSON olmalıdır."); return; }
    }

    setIsSaving(true);
    setSaveError(null);
    setSaveSuccess(false);
    try {
      const res = await fetch(`${API_URL}/api/admin/feature-flags`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          key: form.key.trim().toLowerCase().replace(/\s+/g, "-"),
          description: form.description.trim(),
          isEnabled: form.isEnabled,
          rolloutPercentage: Math.max(0, Math.min(100, form.rolloutPercentage)),
          allowedRoles: form.allowedRoles.trim() || null,
          variantPayload: form.variantPayload.trim() || null,
        }),
      });
      if (!res.ok) {
        const body = await res.json().catch(() => ({}));
        throw new Error((body as { detail?: string }).detail ?? "Kaydetme başarısız.");
      }
      setSaveSuccess(true);
      invalidateFeatureFlagCache();
      await load();
      setTimeout(() => {
        setShowForm(false);
        setSaveSuccess(false);
      }, 900);
    } catch (e) {
      setSaveError(e instanceof Error ? e.message : "Hata oluştu.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(key: string) {
    if (!token) return;
    setIsDeleting(true);
    try {
      const res = await fetch(`${API_URL}/api/admin/feature-flags/${encodeURIComponent(key)}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok && res.status !== 404) throw new Error("Silme başarısız.");
      setDeletingKey(null);
      invalidateFeatureFlagCache();
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Hata oluştu.");
    } finally {
      setIsDeleting(false);
    }
  }

  async function toggleEnabled(flag: FeatureFlag) {
    if (!token) return;
    try {
      await fetch(`${API_URL}/api/admin/feature-flags`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          key: flag.key,
          description: flag.description,
          isEnabled: !flag.isEnabled,
          rolloutPercentage: flag.rolloutPercentage,
          allowedRoles: flag.allowedRoles ?? null,
          variantPayload: flag.variantPayload ?? null,
        }),
      });
      invalidateFeatureFlagCache();
      await load();
    } catch {
      // ignore
    }
  }

  function toggleExpand(key: string) {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  const isAbTest = (flag: FeatureFlag) =>
    !!flag.variantPayload;

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
            <Flag className="h-6 w-6 text-primary" />
            Feature Flags & A/B Testleri
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Platform flag'lerini ve A/B test varyantlarını yönetin.
            Değişiklikler 5 dakika içinde frontend'e yansır.
          </p>
        </div>
        <Button onClick={openNew} className="shrink-0">
          <Plus className="mr-1.5 h-4 w-4" /> Yeni Flag
        </Button>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
          <AlertTriangle className="h-4 w-4 shrink-0" />
          {error}
        </div>
      )}

      {/* Form */}
      {showForm && (
        <Card className="border-primary/30 bg-primary/5">
          <CardHeader className="pb-3">
            <CardTitle className="text-base">
              {editingKey ? `"${editingKey}" flag'ini düzenle` : "Yeni feature flag"}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              {/* Key */}
              <div>
                <label className="mb-1 block text-xs font-medium text-muted-foreground">
                  Key <span className="text-destructive">*</span>
                </label>
                <input
                  type="text"
                  value={form.key}
                  onChange={(e) => setForm((f) => ({ ...f, key: e.target.value }))}
                  disabled={!!editingKey}
                  placeholder="ornek-flag-adi"
                  className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 disabled:opacity-60"
                />
                <p className="mt-1 text-xs text-muted-foreground">Küçük harf, tire ile ayrılmış. Sonradan değiştirilemez.</p>
              </div>
              {/* Description */}
              <div>
                <label className="mb-1 block text-xs font-medium text-muted-foreground">
                  Açıklama <span className="text-destructive">*</span>
                </label>
                <input
                  type="text"
                  value={form.description}
                  onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                  placeholder="Flag ne işe yarar?"
                  className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50"
                />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-3">
              {/* Enabled toggle */}
              <div className="flex items-center gap-3 rounded-xl border border-border bg-background p-3">
                <div className="flex-1">
                  <p className="text-sm font-medium">Etkin</p>
                  <p className="text-xs text-muted-foreground">Flag aktif mi?</p>
                </div>
                <button
                  type="button"
                  onClick={() => setForm((f) => ({ ...f, isEnabled: !f.isEnabled }))}
                  className={`flex h-6 w-11 items-center rounded-full transition-colors ${
                    form.isEnabled ? "bg-primary" : "bg-muted"
                  }`}
                >
                  <span
                    className={`h-5 w-5 rounded-full bg-white shadow transition-transform ${
                      form.isEnabled ? "translate-x-5" : "translate-x-0.5"
                    }`}
                  />
                </button>
              </div>
              {/* Rollout */}
              <div>
                <label className="mb-1 block text-xs font-medium text-muted-foreground">
                  Rollout %
                </label>
                <input
                  type="number"
                  min={0}
                  max={100}
                  value={form.rolloutPercentage}
                  onChange={(e) => setForm((f) => ({ ...f, rolloutPercentage: Number(e.target.value) }))}
                  className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50"
                />
                <p className="mt-1 text-xs text-muted-foreground">100 = herkes, 50 = %50 rastgele</p>
              </div>
              {/* Allowed roles */}
              <div>
                <label className="mb-1 block text-xs font-medium text-muted-foreground">
                  İzin verilen roller
                </label>
                <input
                  type="text"
                  value={form.allowedRoles}
                  onChange={(e) => setForm((f) => ({ ...f, allowedRoles: e.target.value }))}
                  placeholder="Admin,Maker (boş = herkes)"
                  className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50"
                />
              </div>
            </div>

            {/* Variant payload */}
            <div>
              <label className="mb-1 flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
                <Beaker className="h-3.5 w-3.5" />
                A/B Variant Payload (JSON, opsiyonel)
              </label>
              <textarea
                value={form.variantPayload}
                onChange={(e) => setForm((f) => ({ ...f, variantPayload: e.target.value }))}
                rows={3}
                placeholder={'{"variant":"B","headline":"Yeni Başlık"}'}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 font-mono text-xs focus:outline-none focus:ring-2 focus:ring-primary/50"
              />
              <p className="mt-1 text-xs text-muted-foreground">
                Dolu ise A/B test flag'i sayılır. Frontend'de{" "}
                <code className="rounded bg-muted px-1">getVariant("key")</code> ile okunur.
              </p>
            </div>

            {saveError && (
              <p className="text-sm text-destructive">{saveError}</p>
            )}
            {saveSuccess && (
              <p className="flex items-center gap-1.5 text-sm text-emerald-600">
                <Check className="h-4 w-4" /> Kaydedildi.
              </p>
            )}

            <div className="flex items-center gap-2">
              <Button onClick={() => void handleSave()} disabled={isSaving}>
                {isSaving ? <Loader2 className="mr-1.5 h-4 w-4 animate-spin" /> : <Check className="mr-1.5 h-4 w-4" />}
                Kaydet
              </Button>
              <Button variant="outline" onClick={cancelForm} disabled={isSaving}>
                <X className="mr-1.5 h-4 w-4" /> İptal
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Flags table */}
      {flags.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-border bg-muted/10 p-16 text-center">
          <Flag className="mx-auto mb-3 h-10 w-10 text-muted-foreground opacity-30" />
          <p className="text-sm font-medium">Henüz feature flag yok.</p>
          <p className="mt-1 text-xs text-muted-foreground">
            Yeni Flag butonuna tıklayarak ilk flag'i ekleyin.
          </p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/30">
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground">Key</th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground hidden md:table-cell">Açıklama</th>
                <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wider text-muted-foreground">Rollout</th>
                <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wider text-muted-foreground hidden sm:table-cell">Roller</th>
                <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wider text-muted-foreground">Durum</th>
                <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-muted-foreground">İşlem</th>
              </tr>
            </thead>
            <tbody>
              {flags.map((flag, idx) => (
                <>
                  <tr
                    key={flag.key}
                    className={`border-b border-border/50 transition-colors hover:bg-muted/20 ${
                      idx % 2 === 0 ? "bg-background" : "bg-muted/5"
                    }`}
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <code className="rounded bg-muted px-1.5 py-0.5 text-xs font-mono">{flag.key}</code>
                        {isAbTest(flag) && (
                          <span className="flex items-center gap-0.5 rounded-full bg-violet-500/10 px-1.5 py-0.5 text-xs font-semibold text-violet-600">
                            <Beaker className="h-3 w-3" /> A/B
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground hidden md:table-cell max-w-xs truncate">
                      {flag.description}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                        flag.rolloutPercentage === 100
                          ? "bg-emerald-500/10 text-emerald-600"
                          : flag.rolloutPercentage === 0
                          ? "bg-muted text-muted-foreground"
                          : "bg-amber-500/10 text-amber-600"
                      }`}>
                        %{flag.rolloutPercentage}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center hidden sm:table-cell">
                      {flag.allowedRoles
                        ? (
                          <span className="rounded-full bg-blue-500/10 px-2 py-0.5 text-xs text-blue-600">
                            {flag.allowedRoles}
                          </span>
                        )
                        : <span className="text-xs text-muted-foreground">—</span>
                      }
                    </td>
                    <td className="px-4 py-3 text-center">
                      <button
                        onClick={() => void toggleEnabled(flag)}
                        title={flag.isEnabled ? "Devre dışı bırak" : "Etkinleştir"}
                        className="transition-opacity hover:opacity-70"
                      >
                        {flag.isEnabled
                          ? <ToggleRight className="h-6 w-6 text-emerald-500" />
                          : <ToggleLeft className="h-6 w-6 text-muted-foreground" />
                        }
                      </button>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-1">
                        {flag.variantPayload && (
                          <button
                            onClick={() => toggleExpand(flag.key)}
                            className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted transition-colors"
                            title="Variant payload"
                          >
                            {expanded.has(flag.key)
                              ? <ChevronUp className="h-4 w-4" />
                              : <ChevronDown className="h-4 w-4" />}
                          </button>
                        )}
                        <button
                          onClick={() => openEdit(flag)}
                          className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted transition-colors"
                          title="Düzenle"
                        >
                          <Pencil className="h-4 w-4" />
                        </button>
                        {deletingKey === flag.key ? (
                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => void handleDelete(flag.key)}
                              disabled={isDeleting}
                              className="rounded-lg p-1.5 text-destructive hover:bg-destructive/10 transition-colors"
                              title="Onayla"
                            >
                              {isDeleting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}
                            </button>
                            <button
                              onClick={() => setDeletingKey(null)}
                              className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted transition-colors"
                              title="İptal"
                            >
                              <X className="h-4 w-4" />
                            </button>
                          </div>
                        ) : (
                          <button
                            onClick={() => setDeletingKey(flag.key)}
                            className="rounded-lg p-1.5 text-muted-foreground hover:bg-destructive/10 hover:text-destructive transition-colors"
                            title="Sil"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                  {expanded.has(flag.key) && flag.variantPayload && (
                    <tr key={`${flag.key}-expand`} className="bg-violet-500/5 border-b border-border/50">
                      <td colSpan={6} className="px-4 pb-3 pt-0">
                        <div className="mt-2 rounded-lg border border-violet-200 bg-background p-3">
                          <p className="mb-1.5 text-xs font-semibold text-violet-600 flex items-center gap-1">
                            <Beaker className="h-3.5 w-3.5" /> Variant Payload
                          </p>
                          <pre className="overflow-x-auto text-xs text-muted-foreground">
                            {(() => {
                              try { return JSON.stringify(JSON.parse(flag.variantPayload), null, 2); }
                              catch { return flag.variantPayload; }
                            })()}
                          </pre>
                        </div>
                      </td>
                    </tr>
                  )}
                </>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Legend */}
      <div className="rounded-xl border border-border bg-muted/20 p-4 text-xs text-muted-foreground">
        <p className="mb-1 font-semibold text-foreground">Nasıl çalışır?</p>
        <ul className="space-y-1">
          <li>• Frontend <code className="rounded bg-muted px-1">useFeatureFlags()</code> hook'u ile flag'leri çeker, 5 dk TTL cache kullanır.</li>
          <li>• Rollout yüzdesi deterministik hash ile hesaplanır — aynı kullanıcı her zaman aynı gruba düşer.</li>
          <li>• <span className="font-semibold text-violet-600">A/B flag:</span> Variant Payload dolu ise <code className="rounded bg-muted px-1">getVariant&lt;T&gt;("key")</code> ile JSON deserialize edilir.</li>
          <li>• İzin verilen roller boş bırakılırsa flag tüm kullanıcılara gösterilir.</li>
        </ul>
      </div>
    </div>
  );
}
