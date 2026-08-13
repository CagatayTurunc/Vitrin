"use client";

import { useState, useEffect } from "react";
import { signIn } from "next-auth/react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useRouter } from "next/navigation";
import { getErrorMessage } from "@/lib/errors";
import Link from "next/link";
import { Mail, Lock, Eye, EyeOff, ArrowRight, Sparkles, Shield } from "lucide-react";

export function LoginForm() {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [mounted, setMounted] = useState(false);
  const [focusedField, setFocusedField] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    email: "",
    password: "",
  });

  useEffect(() => {
    setMounted(true);
  }, []);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      const result = await signIn("credentials", {
        redirect: false,
        email: formData.email,
        password: formData.password,
      });

      if (result?.error) {
        setError("Giriş bilgileri hatalı. Lütfen kontrol edip tekrar deneyin.");
      } else {
        router.push("/");
        router.refresh();
      }
    } catch (err: unknown) {
      console.error("Login Error:", err);
      setError(getErrorMessage(err, "Giriş yapılırken bir hata oluştu."));
    } finally {
      setIsLoading(false);
    }
  };

  const loginWithGoogle = () => {
    setIsLoading(true);
    signIn("google", { callbackUrl: "/" });
  };

  const loginWithGithub = () => {
    setIsLoading(true);
    signIn("github", { callbackUrl: "/" });
  };

  return (
    <div className="mx-auto flex w-full flex-col justify-center space-y-8 sm:w-[400px]">
      {/* Animated header */}
      <div className={`flex flex-col space-y-3 text-left transition-all duration-700 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-10'}`}>
        <div className="inline-flex items-center rounded-full border border-border/40 bg-gradient-to-r from-primary/10 to-primary/5 px-3 py-1.5 text-xs font-medium w-fit mb-3 transition-all duration-500 hover:from-primary/20 hover:to-primary/10">
          <div className="mr-2 h-1.5 w-1.5 rounded-full bg-primary animate-pulse"></div>
          Tekrar aramızda
        </div>
        <h1 className="text-4xl font-bold tracking-tight bg-gradient-to-r from-foreground to-foreground/80 bg-clip-text text-transparent">
          Tekrar Hoş Geldin
        </h1>
        <p className="text-muted-foreground">
          Hesabına giriş yapmak için bilgilerini gir.
        </p>
      </div>

      {/* Main form card with premium styling */}
      <div className={`transition-all duration-700 delay-200 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-10'}`}>
        <div className="rounded-2xl bg-gradient-to-b from-card/50 to-card border border-border/50 p-8 shadow-xl backdrop-blur-sm">
          <form onSubmit={onSubmit} className="space-y-6">
            {/* Email field */}
            <div className="space-y-2">
              <Label htmlFor="email" className="text-sm font-medium text-foreground">E-posta</Label>
              <div className={`relative transition-all duration-300 ${focusedField === 'email' ? 'scale-[1.02]' : ''}`}>
                <Mail className={`absolute left-3 top-3 h-5 w-5 transition-colors duration-300 ${focusedField === 'email' ? 'text-primary' : 'text-muted-foreground'}`} />
                <Input
                  id="email"
                  name="email"
                  placeholder="isim@ornek.com"
                  type="email"
                  autoCapitalize="none"
                  autoComplete="email"
                  autoCorrect="off"
                  disabled={isLoading}
                  required
                  value={formData.email}
                  onChange={handleChange}
                  onFocus={() => setFocusedField('email')}
                  onBlur={() => setFocusedField(null)}
                  className="pl-11 h-12 rounded-xl border-border/50 bg-background/50 backdrop-blur-sm transition-all duration-300 focus:border-primary/50 focus:ring-primary/20 focus:shadow-lg focus:shadow-primary/10"
                />
                <div className={`absolute inset-0 rounded-xl bg-gradient-to-r from-primary/5 to-transparent opacity-0 pointer-events-none transition-opacity duration-300 ${focusedField === 'email' ? 'opacity-100' : ''}`} />
              </div>
            </div>

            {/* Password field */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label htmlFor="password" className="text-sm font-medium text-foreground">Şifre</Label>
                <Link href="/forgot-password" className="text-xs text-primary hover:text-primary/80 transition-colors duration-200 hover:underline">
                  Şifreni mi unuttun?
                </Link>
              </div>
              <div className={`relative transition-all duration-300 ${focusedField === 'password' ? 'scale-[1.02]' : ''}`}>
                <Lock className={`absolute left-3 top-3 h-5 w-5 transition-colors duration-300 ${focusedField === 'password' ? 'text-primary' : 'text-muted-foreground'}`} />
                <Input
                  id="password"
                  name="password"
                  placeholder="••••••••"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  disabled={isLoading}
                  required
                  value={formData.password}
                  onChange={handleChange}
                  onFocus={() => setFocusedField('password')}
                  onBlur={() => setFocusedField(null)}
                  className="pl-11 pr-11 h-12 rounded-xl border-border/50 bg-background/50 backdrop-blur-sm transition-all duration-300 focus:border-primary/50 focus:ring-primary/20 focus:shadow-lg focus:shadow-primary/10"
                />
                <button
                  type="button"
                  aria-label={showPassword ? "Şifreyi gizle" : "Şifreyi göster"}
                  className="absolute right-3 top-3 text-muted-foreground hover:text-foreground transition-all duration-200 hover:scale-110"
                  onClick={() => setShowPassword(!showPassword)}
                >
                  {showPassword ? (
                    <EyeOff className="h-5 w-5" />
                  ) : (
                    <Eye className="h-5 w-5" />
                  )}
                </button>
                <div className={`absolute inset-0 rounded-xl bg-gradient-to-r from-primary/5 to-transparent opacity-0 pointer-events-none transition-opacity duration-300 ${focusedField === 'password' ? 'opacity-100' : ''}`} />
              </div>
            </div>
            
            {/* Remember me */}
            <div className="flex items-center space-x-3">
              <input
                type="checkbox"
                id="remember"
                className="h-4 w-4 rounded border-border text-primary focus:ring-primary focus:ring-offset-0 transition-all duration-200"
              />
              <label
                htmlFor="remember"
                className="text-sm font-medium text-muted-foreground cursor-pointer select-none hover:text-foreground transition-colors duration-200"
              >
                Beni hatırla
              </label>
            </div>

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
                  Bekleniyor...
                </div>
              ) : (
                <div className="flex items-center justify-center gap-2">
                  Giriş Yap 
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
              <span className="bg-card px-4 text-muted-foreground font-medium">
                veya şununla devam et
              </span>
            </div>
          </div>

          {/* Social login buttons */}
          <div className="grid grid-cols-2 gap-4">
            <Button 
              variant="outline" 
              className="rounded-xl h-12 border-border/50 hover:border-primary/30 hover:bg-primary/5 transition-all duration-300 hover:scale-[1.02] group" 
              type="button" 
              disabled={isLoading} 
              onClick={loginWithGoogle}
            >
              <svg aria-hidden="true" viewBox="0 0 24 24" className="mr-2 h-5 w-5 group-hover:scale-110 transition-transform duration-200">
                <path
                  fill="currentColor"
                  d="M12.48 10.92v3.28h7.84c-.24 1.84-.853 3.187-1.787 4.133-1.147 1.147-2.933 2.4-6.053 2.4-4.827 0-8.6-3.893-8.6-8.72s3.773-8.72 8.6-8.72c2.6 0 4.507 1.027 5.907 2.347l2.307-2.307C18.747 1.44 16.133 0 12.48 0 5.867 0 .307 5.387.307 12s5.56 12 12.173 12c3.573 0 6.267-1.173 8.373-3.36 2.16-2.16 2.84-5.213 2.84-7.667 0-.76-.053-1.467-.173-2.053H12.48z"
                />
              </svg>
              Google
            </Button>
            <Button 
              variant="outline" 
              className="rounded-xl h-12 border-border/50 hover:border-primary/30 hover:bg-primary/5 transition-all duration-300 hover:scale-[1.02] group" 
              type="button" 
              disabled={isLoading} 
              onClick={loginWithGithub}
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
          Henüz hesabın yok mu?{" "}
          <Link href="/register" className="font-semibold text-primary hover:text-primary/80 underline-offset-4 hover:underline transition-all duration-200">
            Kayıt Ol
          </Link>
        </p>
      </div>
    </div>
  );
}
