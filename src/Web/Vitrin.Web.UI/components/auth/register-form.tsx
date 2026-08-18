"use client";

import { useState, useEffect } from "react";
import { signIn } from "next-auth/react";
import Link from "next/link";
import { ArrowRight, Eye, EyeOff, Lock, Mail, MailCheck, User, Shield, Sparkles } from "lucide-react";
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
  const [mounted, setMounted] = useState(false);
  const [focusedField, setFocusedField] = useState<string | null>(null);
  
  const [formData, setFormData] = useState({
    fullName: "",
    username: "",
    email: "",
    password: "",
  });

  useEffect(() => {
    setMounted(true);
  }, []);

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
      // Next.js rewrites kullan - production'da /api/* otomatik yönlendiriliyor
      const fullUrl = '/api/auth/resend-confirmation';
      
      await fetch(fullUrl, {
        method: "POST",
        headers: { 
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ email: formData.email }),
      });
      setResendStatus("Yeni doğrulama e-postası gönderildi.");
    } catch {
      setResendStatus("E-posta yeniden gönderilemedi. Lütfen tekrar dene.");
    }
  };

  if (registered) {
    return (
      <div className={`mx-auto w-full max-w-md transition-all duration-700 ${mounted ? 'opacity-100 scale-100' : 'opacity-0 scale-95'}`}>
        <div className="rounded-2xl bg-gradient-to-b from-card/50 to-card border border-border/50 p-8 shadow-xl backdrop-blur-sm text-center">
          <div className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-primary/20 to-primary/10 border border-primary/20">
            <MailCheck className="h-8 w-8 text-primary" />
          </div>
          <h1 className="text-3xl font-bold mb-4 bg-gradient-to-r from-foreground to-foreground/80 bg-clip-text text-transparent">
            E-postanı kontrol et
          </h1>
          <p className="text-muted-foreground mb-6 leading-relaxed">
            <strong className="text-foreground">{formData.email}</strong> adresine doğrulama bağlantısı gönderdik.
            Bağlantı 24 saat geçerlidir.
          </p>
          
          <div className="space-y-4">
            <Link href="/login">
              <Button className="w-full bg-gradient-to-r from-primary to-primary/90 hover:from-primary/90 hover:to-primary/80 text-primary-foreground rounded-xl h-12 font-semibold transition-all duration-300 hover:scale-[1.02] hover:shadow-lg hover:shadow-primary/20">
                Giriş sayfasına dön
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            </Link>
            
            <button 
              type="button" 
              onClick={() => void resendConfirmation()} 
              className="w-full text-sm text-muted-foreground hover:text-foreground transition-colors duration-200 hover:underline"
            >
              E-posta gelmedi mi? Tekrar gönder
            </button>
            
            {resendStatus && (
              <p className="text-xs text-muted-foreground animate-fade-in-up">{resendStatus}</p>
            )}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto flex w-full flex-col justify-center space-y-8 sm:w-[400px]">
      {/* Animated header */}
      <div className={`flex flex-col space-y-3 text-left transition-all duration-700 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-10'}`}>
        <div className="inline-flex items-center rounded-full border border-border/40 bg-gradient-to-r from-primary/10 to-primary/5 px-3 py-1.5 text-xs font-medium w-fit mb-3 transition-all duration-500 hover:from-primary/20 hover:to-primary/10">
          <Sparkles className="mr-2 h-3 w-3 text-primary" />
          Vitrin'e katıl
        </div>
        <h1 className="text-4xl font-bold tracking-tight bg-gradient-to-r from-foreground to-foreground/80 bg-clip-text text-transparent">
          Hesap oluştur
        </h1>
        <p className="text-muted-foreground">
          Yeni ürünleri keşfetmek için bilgilerini gir.
        </p>
      </div>

      {/* Main form card */}
      <div className={`transition-all duration-700 delay-200 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-10'}`}>
        <div className="rounded-2xl bg-gradient-to-b from-card/50 to-card border border-border/50 p-8 shadow-xl backdrop-blur-sm">
          <form onSubmit={onSubmit} className="space-y-6">
            {/* Full Name field */}
            <Field 
              label="Ad soyad" 
              icon={<User className={`h-5 w-5 transition-colors duration-300 ${focusedField === 'fullName' ? 'text-primary' : 'text-muted-foreground'}`} />}
              focused={focusedField === 'fullName'}
            >
              <Input 
                name="fullName" 
                autoComplete="name" 
                minLength={2} 
                maxLength={100} 
                required 
                disabled={isLoading}
                value={formData.fullName} 
                onChange={handleChange} 
                onFocus={() => setFocusedField('fullName')}
                onBlur={() => setFocusedField(null)}
                placeholder="Ad Soyad" 
                className="pl-11 h-12 rounded-xl border-border/50 bg-background/50 backdrop-blur-sm transition-all duration-300 focus:border-primary/50 focus:ring-primary/20 focus:shadow-lg focus:shadow-primary/10" 
              />
            </Field>

            {/* Username field */}
            <Field 
              label="Kullanıcı adı" 
              icon={<span className={`text-base transition-colors duration-300 ${focusedField === 'username' ? 'text-primary' : 'text-muted-foreground'}`}>@</span>}
              focused={focusedField === 'username'}
            >
              <Input 
                name="username" 
                autoComplete="username" 
                minLength={3} 
                maxLength={50} 
                pattern="[A-Za-z0-9_]+" 
                required 
                disabled={isLoading}
                value={formData.username} 
                onChange={handleChange}
                onFocus={() => setFocusedField('username')}
                onBlur={() => setFocusedField(null)}
                placeholder="kullanici_adi" 
                className="pl-11 h-12 rounded-xl border-border/50 bg-background/50 backdrop-blur-sm transition-all duration-300 focus:border-primary/50 focus:ring-primary/20 focus:shadow-lg focus:shadow-primary/10" 
              />
            </Field>

            {/* Email field */}
            <Field 
              label="E-posta" 
              icon={<Mail className={`h-5 w-5 transition-colors duration-300 ${focusedField === 'email' ? 'text-primary' : 'text-muted-foreground'}`} />}
              focused={focusedField === 'email'}
            >
              <Input 
                name="email" 
                type="email" 
                autoComplete="email" 
                maxLength={255} 
                required 
                disabled={isLoading}
                value={formData.email} 
                onChange={handleChange}
                onFocus={() => setFocusedField('email')}
                onBlur={() => setFocusedField(null)}
                placeholder="isim@ornek.com" 
                className="pl-11 h-12 rounded-xl border-border/50 bg-background/50 backdrop-blur-sm transition-all duration-300 focus:border-primary/50 focus:ring-primary/20 focus:shadow-lg focus:shadow-primary/10" 
              />
            </Field>

            {/* Password field */}
            <Field 
              label="Şifre" 
              icon={<Lock className={`h-5 w-5 transition-colors duration-300 ${focusedField === 'password' ? 'text-primary' : 'text-muted-foreground'}`} />}
              focused={focusedField === 'password'}
            >
              <div className="relative">
                <Input 
                  name="password" 
                  type={showPassword ? "text" : "password"} 
                  autoComplete="new-password" 
                  minLength={8} 
                  maxLength={128} 
                  required 
                  disabled={isLoading}
                  value={formData.password} 
                  onChange={handleChange}
                  onFocus={() => setFocusedField('password')}
                  onBlur={() => setFocusedField(null)}
                  placeholder="••••••••" 
                  className="pl-11 pr-11 h-12 rounded-xl border-border/50 bg-background/50 backdrop-blur-sm transition-all duration-300 focus:border-primary/50 focus:ring-primary/20 focus:shadow-lg focus:shadow-primary/10" 
                />
                <button 
                  type="button" 
                  aria-label={showPassword ? "Şifreyi gizle" : "Şifreyi göster"}
                  className="absolute right-3 top-3 text-muted-foreground hover:text-foreground transition-all duration-200 hover:scale-110" 
                  onClick={() => setShowPassword((value) => !value)}
                >
                  {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                </button>
              </div>
              <p className="mt-2 text-xs text-muted-foreground">
                En az 8 karakter; büyük/küçük harf, rakam ve özel karakter kullanın.
              </p>
            </Field>

            {/* Error message */}
            {error && (
              <div className="p-4 rounded-xl bg-destructive/10 border border-destructive/20 text-sm font-medium text-destructive animate-fade-in-up">
                <div className="flex items-center gap-2">
                  <Shield className="h-4 w-4" />
                  {error}
                </div>
              </div>
            )}

            {/* Submit button */}
            <Button 
              type="submit" 
              disabled={isLoading} 
              className="w-full bg-gradient-to-r from-primary to-primary/90 hover:from-primary/90 hover:to-primary/80 text-primary-foreground rounded-xl h-12 font-semibold transition-all duration-300 hover:scale-[1.02] hover:shadow-lg hover:shadow-primary/20 group"
            >
              {isLoading ? (
                <div className="flex items-center gap-2">
                  <div className="w-4 h-4 border-2 border-primary-foreground/30 border-t-primary-foreground rounded-full animate-spin" />
                  Kaydediliyor...
                </div>
              ) : (
                <div className="flex items-center justify-center gap-2">
                  Kayıt ol 
                  <ArrowRight className="h-4 w-4 group-hover:translate-x-1 transition-transform duration-200" />
                </div>
              )}
            </Button>
          </form>

          {/* Divider */}
          <div className="relative my-8">
            <div className="absolute inset-0 flex items-center">
              <span className="w-full border-t border-border/50" />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-card px-4 text-muted-foreground font-medium">veya</span>
            </div>
          </div>

          {/* Social login buttons */}
          <div className="grid grid-cols-2 gap-4">
            <Button 
              variant="outline" 
              className="rounded-xl h-12 border-border/50 hover:border-primary/30 hover:bg-primary/5 transition-all duration-300 hover:scale-[1.02] group" 
              disabled={isLoading} 
              onClick={() => void signIn("google", { callbackUrl: "/" })}
            >
              <svg aria-hidden="true" viewBox="0 0 24 24" className="mr-2 h-5 w-5 group-hover:scale-110 transition-transform duration-200">
                <path fill="currentColor" d="M12.48 10.92v3.28h7.84c-.24 1.84-.853 3.187-1.787 4.133-1.147 1.147-2.933 2.4-6.053 2.4-4.827 0-8.6-3.893-8.6-8.72s3.773-8.72 8.6-8.72c2.6 0 4.507 1.027 5.907 2.347l2.307-2.307C18.747 1.44 16.133 0 12.48 0 5.867 0 .307 5.387.307 12s5.56 12 12.173 12c3.573 0 6.267-1.173 8.373-3.36 2.16-2.16 2.84-5.213 2.84-7.667 0-.76-.053-1.467-.173-2.053H12.48z" />
              </svg>
              Google
            </Button>
            <Button 
              variant="outline" 
              className="rounded-xl h-12 border-border/50 hover:border-primary/30 hover:bg-primary/5 transition-all duration-300 hover:scale-[1.02] group" 
              disabled={isLoading} 
              onClick={() => void signIn("github", { callbackUrl: "/" })}
            >
              <svg aria-hidden="true" viewBox="0 0 24 24" className="mr-2 h-5 w-5 group-hover:scale-110 transition-transform duration-200" fill="currentColor">
                <path d="M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12" />
              </svg>
              GitHub
            </Button>
          </div>
        </div>
      </div>

      {/* Footer */}
      <div className={`text-center transition-all duration-700 delay-400 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-10'}`}>
        <p className="text-sm text-muted-foreground">
          Zaten hesabın var mı?{" "}
          <Link href="/login" className="font-semibold text-primary hover:text-primary/80 underline-offset-4 hover:underline transition-all duration-200">
            Giriş yap
          </Link>
        </p>
      </div>
    </div>
  );
}

function Field({ label, icon, children, focused }: { label: string; icon: React.ReactNode; children: React.ReactNode; focused?: boolean }) {
  return (
    <div className="space-y-2">
      <Label className="text-sm font-medium text-foreground">{label}</Label>
      <div className={`relative transition-all duration-300 ${focused ? 'scale-[1.02]' : ''}`}>
        <span className="absolute left-3 top-3 z-10">{icon}</span>
        {children}
        <div className={`absolute inset-0 rounded-xl bg-gradient-to-r from-primary/5 to-transparent opacity-0 pointer-events-none transition-opacity duration-300 ${focused ? 'opacity-100' : ''}`} />
      </div>
    </div>
  );
}
