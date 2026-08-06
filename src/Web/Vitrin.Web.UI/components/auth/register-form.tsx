"use client";

import { useState } from "react";
import { signIn } from "next-auth/react";
import Link from "next/link";
import { ArrowRight, Eye, EyeOff, Lock, Mail, MailCheck, User } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiProblemMessage, getErrorMessage } from "@/lib/errors";

export function RegisterForm() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [registered, setRegistered] = useState(false);
  const [resendStatus, setResendStatus] = useState("");
  const [formData, setFormData] = useState({
    fullName: "",
    username: "",
    email: "",
    password: "",
  });

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setFormData((current) => ({ ...current, [event.target.name]: event.target.value }));
  };

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData),
      });
      if (!response.ok) {
        const data: unknown = await response.json();
        throw new Error(getApiProblemMessage(data, "Kayıt işlemi başarısız oldu. Lütfen tekrar deneyin."));
      }
      setRegistered(true);
    } catch (caught: unknown) {
      setError(getErrorMessage(caught, "Bilinmeyen bir hata oluştu."));
    } finally {
      setIsLoading(false);
    }
  };

  const resendConfirmation = async () => {
    setResendStatus("Gönderiliyor...");
    try {
      await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/resend-confirmation`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: formData.email }),
      });
      setResendStatus("Yeni doğrulama e-postası gönderildi.");
    } catch {
      setResendStatus("E-posta yeniden gönderilemedi. Lütfen tekrar dene.");
    }
  };

  if (registered) {
    return (
      <div className="rounded-3xl border bg-card p-8 text-center shadow-sm">
        <div className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-500/10">
          <MailCheck className="h-7 w-7 text-emerald-600" />
        </div>
        <h1 className="text-2xl font-bold">E-postanı kontrol et</h1>
        <p className="mt-3 text-sm leading-6 text-muted-foreground">
          <strong className="text-foreground">{formData.email}</strong> adresine doğrulama bağlantısı gönderdik.
          Bağlantı 24 saat geçerlidir.
        </p>
        <Link href="/login" className="mt-6 inline-flex font-semibold text-[#007A52] hover:underline">
          Giriş sayfasına dön
        </Link>
        <button type="button" onClick={() => void resendConfirmation()} className="mt-4 block w-full text-sm text-muted-foreground hover:text-foreground hover:underline">
          E-posta gelmedi mi? Tekrar gönder
        </button>
        {resendStatus && <p className="mt-2 text-xs text-muted-foreground">{resendStatus}</p>}
      </div>
    );
  }

  return (
    <div className="mx-auto flex w-full flex-col justify-center space-y-6 sm:w-[400px]">
      <div className="space-y-2 text-left">
        <div className="mb-2 inline-flex w-fit items-center rounded-full border bg-muted/50 px-2.5 py-0.5 text-xs font-medium">
          <span className="mr-1.5 h-1.5 w-1.5 rounded-full bg-[#007A52]" /> Vitrin&apos;e katıl
        </div>
        <h1 className="text-3xl font-bold tracking-tight">Hesap oluştur</h1>
        <p className="text-sm text-muted-foreground">Yeni ürünleri keşfetmek için bilgilerini gir.</p>
      </div>

      <form onSubmit={onSubmit} className="grid gap-5">
        <Field label="Ad soyad" icon={<User className="h-5 w-5" />}>
          <Input name="fullName" autoComplete="name" minLength={2} maxLength={100} required disabled={isLoading}
            value={formData.fullName} onChange={handleChange} placeholder="Ad Soyad" className="h-11 rounded-xl pl-10" />
        </Field>
        <Field label="Kullanıcı adı" icon={<span className="text-base">@</span>}>
          <Input name="username" autoComplete="username" minLength={3} maxLength={50} pattern="[A-Za-z0-9_]+" required disabled={isLoading}
            value={formData.username} onChange={handleChange} placeholder="kullanici_adi" className="h-11 rounded-xl pl-10" />
        </Field>
        <Field label="E-posta" icon={<Mail className="h-5 w-5" />}>
          <Input name="email" type="email" autoComplete="email" maxLength={255} required disabled={isLoading}
            value={formData.email} onChange={handleChange} placeholder="isim@ornek.com" className="h-11 rounded-xl pl-10" />
        </Field>
        <Field label="Şifre" icon={<Lock className="h-5 w-5" />}>
          <Input name="password" type={showPassword ? "text" : "password"} autoComplete="new-password" minLength={8} maxLength={128} required disabled={isLoading}
            value={formData.password} onChange={handleChange} placeholder="••••••••" className="h-11 rounded-xl pl-10 pr-10" />
          <button type="button" aria-label={showPassword ? "Şifreyi gizle" : "Şifreyi göster"}
            className="absolute right-3 top-2.5 text-muted-foreground" onClick={() => setShowPassword((value) => !value)}>
            {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
          </button>
          <p className="mt-2 text-xs text-muted-foreground">En az 8 karakter; büyük/küçük harf, rakam ve özel karakter kullanın.</p>
        </Field>

        {error && <div className="text-sm font-medium text-destructive">{error}</div>}
        <Button type="submit" disabled={isLoading} className="h-11 rounded-xl bg-[#007A52] text-white hover:bg-[#006B48]">
          {isLoading ? "Kaydediliyor..." : <>Kayıt ol <ArrowRight className="ml-2 h-4 w-4" /></>}
        </Button>
      </form>

      <div className="relative"><div className="absolute inset-0 flex items-center"><span className="w-full border-t" /></div><div className="relative flex justify-center text-xs uppercase"><span className="bg-background px-2 text-muted-foreground">veya</span></div></div>
      <div className="grid grid-cols-2 gap-4">
        <Button variant="outline" className="h-10 rounded-xl" disabled={isLoading} onClick={() => void signIn("google", { callbackUrl: "/" })}>Google</Button>
        <Button variant="outline" className="h-10 rounded-xl" disabled={isLoading} onClick={() => void signIn("github", { callbackUrl: "/" })}>GitHub</Button>
      </div>
      <p className="text-center text-sm text-muted-foreground">Zaten hesabın var mı? <Link href="/login" className="font-semibold text-[#007A52] hover:underline">Giriş yap</Link></p>
    </div>
  );
}

function Field({ label, icon, children }: { label: string; icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <div className="grid gap-2">
      <Label>{label}</Label>
      <div className="relative">
        <span className="absolute left-3 top-2.5 z-10 text-muted-foreground">{icon}</span>
        {children}
      </div>
    </div>
  );
}
