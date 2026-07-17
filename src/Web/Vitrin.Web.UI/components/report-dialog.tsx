"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import { Flag, Loader2, ShieldCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

const categories = [
  { value: "Spam", label: "Spam veya yanıltıcı içerik" },
  { value: "Harassment", label: "Taciz veya zorbalık" },
  { value: "Hate", label: "Nefret söylemi" },
  { value: "Misinformation", label: "Yanlış bilgi" },
  { value: "Illegal", label: "Yasadışı içerik" },
  { value: "Other", label: "Diğer" },
];

interface ReportDialogProps {
  targetType: "Comment" | "Product" | "User";
  targetId: string;
  targetOwnerUserId?: string | null;
  triggerClassName?: string;
  compact?: boolean;
}

export function ReportDialog({
  targetType,
  targetId,
  targetOwnerUserId,
  triggerClassName,
  compact = false,
}: ReportDialogProps) {
  const { data: session } = useSession();
  const [open, setOpen] = useState(false);
  const [category, setCategory] = useState("Spam");
  const [details, setDetails] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);

  const submit = async () => {
    if (!session?.accessToken || details.trim().length < 10) return;
    setIsSubmitting(true);
    setFeedback(null);
    try {
      const response = await fetch(`${API_URL}/api/auth/moderation/reports`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${session.accessToken}`,
        },
        body: JSON.stringify({
          targetType,
          targetId,
          targetOwnerUserId: targetOwnerUserId || null,
          category,
          details: details.trim(),
        }),
      });

      if (!response.ok) {
        const problem = await response.json().catch(() => null) as { detail?: string } | null;
        throw new Error(problem?.detail || "Rapor gönderilemedi.");
      }

      setFeedback("Rapor güvenli şekilde moderasyon kuyruğuna alındı.");
      setDetails("");
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : "Rapor gönderilemedi.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(next) => {
      setOpen(next);
      if (!next) setFeedback(null);
    }}>
      <DialogTrigger asChild>
        <button
          type="button"
          disabled={!session?.accessToken}
          title={session ? "İçeriği raporla" : "Raporlamak için giriş yapın"}
          className={triggerClassName ?? "inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-red-500 disabled:cursor-not-allowed disabled:opacity-40"}
        >
          <Flag className={compact ? "h-3 w-3" : "h-4 w-4"} />
          {!compact && "Raporla"}
        </button>
      </DialogTrigger>
      <DialogContent className="overflow-hidden border-border/70 sm:max-w-xl">
        <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-amber-400 via-rose-500 to-purple-500" />
        <DialogHeader>
          <div className="mb-3 flex h-11 w-11 items-center justify-center rounded-2xl bg-rose-500/10 text-rose-500">
            <Flag className="h-5 w-5" />
          </div>
          <DialogTitle>Topluluk güvenliği raporu</DialogTitle>
          <DialogDescription>
            Moderatörlerin doğru kararı verebilmesi için sorunu açık ve somut biçimde anlatın.
          </DialogDescription>
        </DialogHeader>

        {feedback?.startsWith("Rapor güvenli") ? (
          <div className="flex items-start gap-3 rounded-2xl border border-emerald-500/20 bg-emerald-500/10 p-4 text-sm text-emerald-700 dark:text-emerald-300">
            <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="font-semibold">Rapor alındı</p>
              <p className="mt-1 opacity-90">{feedback}</p>
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            <label className="grid gap-2 text-sm font-medium">
              Kategori
              <select
                value={category}
                onChange={(event) => setCategory(event.target.value)}
                className="h-11 rounded-xl border bg-background px-3 text-sm outline-none focus:ring-2 focus:ring-primary/30"
              >
                {categories.map((item) => (
                  <option key={item.value} value={item.value}>{item.label}</option>
                ))}
              </select>
            </label>
            <label className="grid gap-2 text-sm font-medium">
              Ne oldu?
              <Textarea
                value={details}
                onChange={(event) => setDetails(event.target.value)}
                placeholder="Bağlamı ve hangi kuralın ihlal edildiğini açıklayın..."
                className="min-h-32 resize-none rounded-xl"
                maxLength={2000}
              />
              <span className="text-right text-xs font-normal text-muted-foreground">{details.length}/2000</span>
            </label>
            {feedback && <p className="text-sm text-destructive">{feedback}</p>}
          </div>
        )}

        <DialogFooter>
          {feedback?.startsWith("Rapor güvenli") ? (
            <Button onClick={() => setOpen(false)}>Tamam</Button>
          ) : (
            <Button onClick={submit} disabled={isSubmitting || details.trim().length < 10} className="gap-2">
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Raporu gönder
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
