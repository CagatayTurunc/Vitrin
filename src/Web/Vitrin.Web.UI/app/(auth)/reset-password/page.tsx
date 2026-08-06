"use client";

import { useState } from "react";
import Link from "next/link";
import { CheckCircle2, Lock } from "lucide-react";
import { AuthBrandPanel } from "@/components/auth-brand-panel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiProblemMessage } from "@/lib/errors";

export default function ResetPasswordPage() {
  const [token] = useState(() => typeof window === "undefined" ? "" : new URLSearchParams(window.location.search).get("token") ?? "");
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [completed, setCompleted] = useState(false);
  const [error, setError] = useState("");

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!token) return setError("Şifre yenileme tokenı bulunamadı.");
    if (password !== confirmation) return setError("Şifreler eşleşmiyor.");
    setIsLoading(true);
    setError("");
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/reset-password`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, password }),
      });
      if (!response.ok) {
        const data: unknown = await response.json();
        throw new Error(getApiProblemMessage(data, "Şifre yenilenemedi."));
      }
      setCompleted(true);
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
          {completed ? (
            <div className="rounded-3xl border bg-card p-8 text-center">
              <CheckCircle2 className="mx-auto mb-5 h-12 w-12 text-emerald-600" />
              <h1 className="text-2xl font-bold">Şifren yenilendi</h1>
              <p className="mt-3 text-sm text-muted-foreground">Yeni şifrenle hesabına giriş yapabilirsin.</p>
              <Link href="/login" className="mt-6 inline-flex font-semibold text-[#007A52] hover:underline">Giriş yap</Link>
            </div>
          ) : (
            <>
              <div><h1 className="text-3xl font-bold">Yeni şifre belirle</h1><p className="mt-2 text-sm text-muted-foreground">Güçlü ve daha önce kullanmadığın bir şifre seç.</p></div>
              <form onSubmit={submit} className="space-y-5">
                <PasswordField id="password" label="Yeni şifre" value={password} onChange={setPassword} />
                <PasswordField id="confirmation" label="Yeni şifre tekrar" value={confirmation} onChange={setConfirmation} />
                <p className="text-xs text-muted-foreground">12-128 karakter; büyük/küçük harf, rakam ve özel karakter.</p>
                {error && <p className="text-sm text-destructive">{error}</p>}
                <Button type="submit" disabled={isLoading} className="h-11 w-full rounded-xl bg-[#007A52] text-white hover:bg-[#006B48]">{isLoading ? "Kaydediliyor..." : "Şifremi yenile"}</Button>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function PasswordField({ id, label, value, onChange }: { id: string; label: string; value: string; onChange: (value: string) => void }) {
  return <div className="space-y-2"><Label htmlFor={id}>{label}</Label><div className="relative"><Lock className="absolute left-3 top-3 h-5 w-5 text-muted-foreground" /><Input id={id} type="password" autoComplete="new-password" minLength={12} maxLength={128} required value={value} onChange={(event) => onChange(event.target.value)} className="h-11 rounded-xl pl-10" /></div></div>;
}
