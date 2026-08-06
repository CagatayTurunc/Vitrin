"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import { ShieldCheck, Download, Trash2, Loader2, AlertTriangle, Undo2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

interface KvkkDataControlsProps {
  deleteRequestedAt?: string | null;
}

export function KvkkDataControls({ deleteRequestedAt }: KvkkDataControlsProps) {
  const { data: session } = useSession();
  const [isExporting, setIsExporting] = useState(false);
  const [isRequesting, setIsRequesting] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  const [pendingDeletion, setPendingDeletion] = useState<string | null>(deleteRequestedAt ?? null);
  const [status, setStatus] = useState<{ kind: "success" | "error"; text: string } | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const accessToken = session?.accessToken;

  const exportData = async () => {
    if (!accessToken) return;
    setIsExporting(true);
    setStatus(null);
    try {
      const res = await fetch(`${API_URL}/api/auth/users/me/data-export`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (!res.ok) throw new Error("Veri dışa aktarılamadı.");
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `vitrin-verilerim-${new Date().toISOString().slice(0, 10)}.json`;
      a.click();
      URL.revokeObjectURL(url);
      setStatus({ kind: "success", text: "Verileriniz indirildi." });
    } catch (e) {
      setStatus({ kind: "error", text: e instanceof Error ? e.message : "Hata oluştu." });
    } finally {
      setIsExporting(false);
    }
  };

  const requestDeletion = async () => {
    if (!accessToken || !confirmDelete) return;
    setIsRequesting(true);
    setStatus(null);
    try {
      const res = await fetch(`${API_URL}/api/auth/users/me/request-deletion`, {
        method: "POST",
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (!res.ok) throw new Error("Silme talebi gönderilemedi.");
      const data = await res.json() as { scheduledDeletionAt: string };
      setPendingDeletion(data.scheduledDeletionAt);
      setConfirmDelete(false);
      setStatus({ kind: "success", text: `Hesap silme talebi alındı. Verileriniz ${new Date(data.scheduledDeletionAt).toLocaleDateString("tr-TR")} tarihinde silinecek.` });
    } catch (e) {
      setStatus({ kind: "error", text: e instanceof Error ? e.message : "Hata oluştu." });
    } finally {
      setIsRequesting(false);
    }
  };

  const cancelDeletion = async () => {
    if (!accessToken) return;
    setIsCancelling(true);
    setStatus(null);
    try {
      const res = await fetch(`${API_URL}/api/auth/users/me/request-deletion`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (!res.ok) throw new Error("Talep iptal edilemedi.");
      setPendingDeletion(null);
      setStatus({ kind: "success", text: "Hesap silme talebi iptal edildi." });
    } catch (e) {
      setStatus({ kind: "error", text: e instanceof Error ? e.message : "Hata oluştu." });
    } finally {
      setIsCancelling(false);
    }
  };

  return (
    <Card className="mb-8 overflow-hidden border-border/70 bg-gradient-to-br from-card via-card to-blue-500/5 shadow-sm">
      <CardHeader className="border-b border-border/60">
        <CardTitle className="flex items-center gap-2 text-xl">
          <ShieldCheck className="h-5 w-5 text-blue-500" />
          Veri kontrolü (KVKK)
        </CardTitle>
        <CardDescription>
          6698 sayılı KVKK kapsamında verilerinizi indirme ve hesap silme hakkınızı kullanın.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6 p-6">
        {/* Data export */}
        <div className="flex items-start justify-between gap-4 rounded-xl border border-border/70 bg-background/60 p-4">
          <div>
            <p className="font-medium">Verilerimi indir</p>
            <p className="mt-1 text-xs text-muted-foreground">
              Profil bilgileri, rozetler ve takip listenizi JSON formatında alın.
            </p>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={exportData}
            disabled={isExporting}
            className="shrink-0"
          >
            {isExporting ? <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" /> : <Download className="mr-1.5 h-3.5 w-3.5" />}
            İndir
          </Button>
        </div>

        {/* Account deletion */}
        <div className="rounded-xl border border-red-200 bg-red-50/50 p-4 dark:border-red-900/30 dark:bg-red-950/10">
          <div className="flex items-start gap-3">
            <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-red-500" />
            <div className="flex-1">
              <p className="font-medium text-red-600 dark:text-red-400">Hesabı sil</p>
              <p className="mt-1 text-xs text-muted-foreground">
                Talebinizden 30 gün sonra kişisel verileriniz anonim hale getirilir.
                İstatistiksel veriler (oylar, yorumlar) yazar bilgisi olmadan korunabilir.
              </p>

              {pendingDeletion ? (
                <div className="mt-3 flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950/20">
                  <p className="flex-1 text-xs text-amber-700 dark:text-amber-400">
                    Silme talebi aktif — {new Date(pendingDeletion).toLocaleDateString("tr-TR")} tarihinde işleme alınacak.
                  </p>
                  <Button variant="outline" size="sm" onClick={cancelDeletion} disabled={isCancelling} className="shrink-0">
                    {isCancelling ? <Loader2 className="mr-1 h-3 w-3 animate-spin" /> : <Undo2 className="mr-1 h-3 w-3" />}
                    İptal et
                  </Button>
                </div>
              ) : (
                <div className="mt-3">
                  {!confirmDelete ? (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setConfirmDelete(true)}
                      className="border-red-300 text-red-600 hover:bg-red-50 dark:border-red-800 dark:text-red-400"
                    >
                      <Trash2 className="mr-1.5 h-3.5 w-3.5" /> Hesabı sil
                    </Button>
                  ) : (
                    <div className="flex items-center gap-2">
                      <Button
                        size="sm"
                        onClick={requestDeletion}
                        disabled={isRequesting}
                        className="bg-red-500 hover:bg-red-600 text-white"
                      >
                        {isRequesting ? <Loader2 className="mr-1 h-3 w-3 animate-spin" /> : null}
                        Evet, talebimi gönder
                      </Button>
                      <Button variant="outline" size="sm" onClick={() => setConfirmDelete(false)}>
                        İptal
                      </Button>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>

        {status && (
          <p
            className={`text-sm ${status.kind === "error" ? "text-red-500" : "text-emerald-600"}`}
            role="status"
          >
            {status.text}
          </p>
        )}

        <p className="text-xs text-muted-foreground">
          KVKK haklarınız hakkında daha fazla bilgi için{" "}
          <a href="/kvkk" className="text-blue-500 hover:underline">aydınlatma metnimizi</a>{" "}
          inceleyebilir ya da{" "}
          <a href="mailto:kvkk@vitrin.app" className="text-blue-500 hover:underline">kvkk@vitrin.app</a>{" "}
          adresinden iletişime geçebilirsiniz.
        </p>
      </CardContent>
    </Card>
  );
}
