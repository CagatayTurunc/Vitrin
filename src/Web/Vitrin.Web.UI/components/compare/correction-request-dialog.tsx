"use client";

import { useState } from "react";
import { AlertCircle, CheckCircle2, Loader2, MessageSquareWarning } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export function CorrectionRequestDialog({ productId, productName }: { productId: string; productName: string }) {
  const [isOpen, setIsOpen] = useState(false);
  const [details, setDetails] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);

  const handleSubmit = async () => {
    if (!details.trim()) return;
    setIsSubmitting(true);
    try {
      // Simulate API call for now
      await new Promise(resolve => setTimeout(resolve, 800));
      setIsSuccess(true);
      setTimeout(() => {
        setIsOpen(false);
        setIsSuccess(false);
        setDetails("");
      }, 2000);
    } catch (e) {
      console.error(e);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) {
    return (
      <Button variant="ghost" size="sm" onClick={() => setIsOpen(true)} className="text-xs text-muted-foreground mt-4 w-full">
        <MessageSquareWarning className="w-3 h-3 mr-2" />
        Hatalı veri bildir
      </Button>
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-md rounded-2xl bg-card p-6 shadow-xl border border-border">
        {isSuccess ? (
          <div className="text-center space-y-3 py-6">
            <CheckCircle2 className="mx-auto h-12 w-12 text-emerald-500" />
            <h3 className="text-xl font-bold">Talebiniz Alındı</h3>
            <p className="text-sm text-muted-foreground">İnceledikten sonra veriler güncellenecektir.</p>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex items-center gap-3 border-b border-border pb-3">
              <div className="rounded-full bg-amber-500/10 p-2">
                <AlertCircle className="h-5 w-5 text-amber-500" />
              </div>
              <div>
                <h3 className="font-bold">{productName} - Veri Düzeltme</h3>
                <p className="text-xs text-muted-foreground">Karşılaştırma tablosundaki yanlış bilgileri bize bildirin.</p>
              </div>
            </div>
            
            <textarea
              className="w-full h-32 rounded-lg border border-input bg-background px-3 py-2 text-sm focus:border-primary focus:outline-none"
              placeholder="Hangi veri hatalı ve doğrusu ne olmalı?"
              value={details}
              onChange={(e) => setDetails(e.target.value)}
            />
            
            <div className="flex gap-2 justify-end pt-2">
              <Button variant="ghost" onClick={() => setIsOpen(false)} disabled={isSubmitting}>İptal</Button>
              <Button onClick={handleSubmit} disabled={isSubmitting || !details.trim()}>
                {isSubmitting ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : null}
                Gönder
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
