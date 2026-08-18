"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { CheckCircle2, LoaderCircle, XCircle } from "lucide-react";
import { AuthBrandPanel } from "@/components/auth-brand-panel";

type ConfirmationState = "loading" | "success" | "error";

export default function ConfirmEmailPage() {
  return <Suspense fallback={<ConfirmationShell state="loading" message="E-posta adresin doğrulanıyor..." />}><ConfirmationCard /></Suspense>;
}

function ConfirmationCard() {
  const token = useSearchParams().get("token") ?? "";
  const [state, setState] = useState<ConfirmationState>(() => token ? "loading" : "error");
  const [message, setMessage] = useState(() => token ? "E-posta adresin doğrulanıyor..." : "Doğrulama bağlantısında token bulunamadı.");

  useEffect(() => {
    if (!token) return;

    // Next.js rewrites kullan - production'da /api/* otomatik yönlendiriliyor
    const fullUrl = '/api/auth/confirm-email';

    void fetch(fullUrl, {
      method: "POST",
      headers: { 
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ token }),
    }).then(async (response) => {
      const data = await response.json() as { message?: string; detail?: string };
      setMessage(data.message ?? data.detail ?? (response.ok ? "E-posta adresin doğrulandı." : "Bağlantı geçersiz veya süresi dolmuş."));
      setState(response.ok ? "success" : "error");
    }).catch(() => {
      setMessage("Doğrulama servisine ulaşılamadı. Lütfen tekrar dene.");
      setState("error");
    });
  }, [token]);

  return <ConfirmationShell state={state} message={message} />;
}

function ConfirmationShell({ state, message }: { state: ConfirmationState; message: string }) {
  return (
    <div className="container grid min-h-screen bg-background lg:max-w-none lg:grid-cols-2 lg:px-0">
      <AuthBrandPanel />
      <div className="flex items-center justify-center p-8">
        <div className="w-full max-w-[400px] rounded-3xl border bg-card p-8 text-center">
          {state === "loading" && <LoaderCircle className="mx-auto mb-5 h-12 w-12 animate-spin text-[#007A52]" />}
          {state === "success" && <CheckCircle2 className="mx-auto mb-5 h-12 w-12 text-emerald-600" />}
          {state === "error" && <XCircle className="mx-auto mb-5 h-12 w-12 text-destructive" />}
          <h1 className="text-2xl font-bold">{state === "success" ? "Doğrulama tamamlandı" : state === "error" ? "Doğrulama tamamlanamadı" : "Lütfen bekle"}</h1>
          <p className="mt-3 text-sm leading-6 text-muted-foreground">{message}</p>
          {state !== "loading" && <Link href="/login" className="mt-6 inline-flex font-semibold text-[#007A52] hover:underline">Giriş sayfasına git</Link>}
        </div>
      </div>
    </div>
  );
}
