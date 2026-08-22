"use client";

import { useState } from "react";
import Link from "next/link";
import { ArrowLeft, Mail, Send } from "lucide-react";
import { AuthBrandPanel } from "@/components/auth-brand-panel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiProblemMessage } from "@/lib/errors";

export function ForgotPasswordForm() {
  const [email, setEmail] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState("");

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsLoading(true);
    setError("");
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/account/forgot-password`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email }),
      });
      if (!response.ok) {
        const data: unknown = await response.json();
        throw new Error(getApiProblemMessage(data, "İstek tamamlanamadı."));
      }
      setSent(true);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Bağlantı hatası oluştu.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="container grid min-h-screen bg-background lg:max-w-none lg:grid-cols-2 lg:px-0">
      <AuthBrandPanel />
      <div className="flex items-center justify-center p-8">
        <div className="w-full max-w-[400px] space-y-6">
          <div className="space-y-2">
            <h1 className="text-3xl font-bold">Şifreni yenile</h1>
            <p className="text-sm leading-6 text-muted-foreground">
              Hesabındaki e-posta adresini gir; sana 1 saat geçerli bir bağlantı gönderelim.
            </p>
          </div>
          {sent ? (
            <div className="rounded-2xl border border-emerald-500/20 bg-emerald-500/5 p-6">
              <Send className="mb-3 h-6 w-6 text-emerald-600" />
              <h2 className="font-semibold">E-postanı kontrol et</h2>
              <p className="mt-2 text-sm leading-6 text-muted-foreground">
                Bu adresle bir hesap varsa şifre yenileme bağlantısı gönderildi.
              </p>
            </div>
          ) : (
            <form onSubmit={submit} className="space-y-5">
              <div className="space-y-2">
                <Label htmlFor="email">E-posta</Label>
                <div className="relative">
                  <Mail className="absolute left-3 top-3 h-5 w-5 text-muted-foreground" />
                  <Input
                    id="email"
                    type="email"
                    autoComplete="email"
                    required
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    className="h-11 rounded-xl pl-10"
                  />
                </div>
              </div>
              {error && <p className="text-sm text-destructive">{error}</p>}
              <Button
                type="submit"
                disabled={isLoading}
                className="h-11 w-full rounded-xl bg-[#007A52] text-white hover:bg-[#006B48]"
              >
                {isLoading ? "Gönderiliyor..." : "Yenileme bağlantısı gönder"}
              </Button>
            </form>
          )}
          <Link
            href="/login"
            className="inline-flex items-center text-sm font-semibold text-[#007A52] hover:underline"
          >
            <ArrowLeft className="mr-2 h-4 w-4" /> Girişe dön
          </Link>
        </div>
      </div>
    </div>
  );
}
