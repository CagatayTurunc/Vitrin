"use client";

import { useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import { AlertTriangle, CheckCircle2, Loader2, Scale } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

interface Appeal {
  id: string;
  statement: string;
  status: number;
  createdAtUtc: string;
  reviewNote?: string | null;
}

interface AccountModerationStatusProps {
  activeBanId?: string | null;
  suspendedUntilUtc?: string | null;
  suspensionReason?: string | null;
  isBanned?: boolean;
}

export function AccountModerationStatus(props: AccountModerationStatusProps) {
  const { data: session } = useSession();
  const [statement, setStatement] = useState("");
  const [appeals, setAppeals] = useState<Appeal[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!session?.accessToken || !props.isBanned) return;
    void fetch(`${API_URL}/api/auth/moderation/appeals/me`, {
      headers: { Authorization: `Bearer ${session.accessToken}` },
    }).then(async (response) => response.ok ? await response.json() as Appeal[] : [])
      .then(setAppeals)
      .catch(() => undefined);
  }, [props.isBanned, session?.accessToken]);

  if (!props.isBanned || !props.activeBanId) return null;

  const hasOpenAppeal = appeals.some((appeal) => appeal.status === 0);
  const submitAppeal = async () => {
    if (!session?.accessToken || statement.trim().length < 20) return;
    setIsSubmitting(true);
    try {
      const response = await fetch(`${API_URL}/api/auth/moderation/appeals`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${session.accessToken}`,
        },
        body: JSON.stringify({ banId: props.activeBanId, statement: statement.trim() }),
      });
      if (response.ok) {
        const result = await response.json() as { id: string; status: number };
        setAppeals((current) => [{ id: result.id, status: result.status, statement, createdAtUtc: new Date().toISOString() }, ...current]);
        setStatement("");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="mb-8 overflow-hidden rounded-3xl border border-amber-500/30 bg-gradient-to-br from-amber-500/10 via-card to-card shadow-sm">
      <div className="border-b border-amber-500/20 p-6">
        <div className="flex items-start gap-4">
          <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-amber-500/15 text-amber-500"><AlertTriangle className="h-6 w-6" /></div>
          <div>
            <h2 className="text-xl font-bold">Hesabınız askıya alındı</h2>
            <p className="mt-1 text-sm text-muted-foreground">{props.suspendedUntilUtc ? `${new Date(props.suspendedUntilUtc).toLocaleString("tr-TR")} tarihine kadar` : "Kalıcı yaptırım"}</p>
            <p className="mt-3 rounded-xl bg-background/70 p-3 text-sm"><strong>Gerekçe:</strong> {props.suspensionReason}</p>
          </div>
        </div>
      </div>
      <div className="p-6">
        {hasOpenAppeal ? (
          <div className="flex gap-3 text-sm"><Scale className="mt-0.5 h-5 w-5 shrink-0 text-blue-500" /><div><p className="font-semibold">İtirazınız incelemede</p><p className="mt-1 text-muted-foreground">Karar verildiğinde bildirim alacaksınız. Aynı yaptırım için ikinci bir itiraz oluşturulamaz.</p></div></div>
        ) : appeals[0]?.status === 1 ? (
          <div className="flex gap-3 text-sm text-emerald-600"><CheckCircle2 className="h-5 w-5" /> İtirazınız kabul edildi. Oturumu yenileyerek hesabınızı kullanabilirsiniz.</div>
        ) : (
          <div className="space-y-3">
            <div><h3 className="font-semibold">Karara itiraz et</h3><p className="mt-1 text-sm text-muted-foreground">Bağlamı, neden yeniden değerlendirilmesi gerektiğini ve varsa düzeltici adımlarınızı açıklayın.</p></div>
            <Textarea value={statement} onChange={(event) => setStatement(event.target.value)} className="min-h-28 resize-none" placeholder="En az 20 karakterlik itiraz açıklaması..." maxLength={3000} />
            <div className="flex justify-end"><Button onClick={submitAppeal} disabled={statement.trim().length < 20 || isSubmitting} className="gap-2">{isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />} İtirazı gönder</Button></div>
          </div>
        )}
      </div>
    </section>
  );
}
