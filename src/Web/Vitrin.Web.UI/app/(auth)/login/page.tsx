import { Metadata } from "next";
import { Suspense } from "react";
import { LoginForm } from "@/components/auth/login-form";
import { AuthBrandPanel } from "@/components/auth-brand-panel";

export const metadata: Metadata = {
  title: "Giriş Yap - Vitrin",
  description: "Vitrin hesabınıza giriş yapın.",
};

function LoginFormFallback() {
  return (
    <div className="mx-auto flex w-full flex-col justify-center space-y-8 sm:w-[400px]">
      <div className="flex flex-col space-y-3 text-left">
        <div className="inline-flex items-center rounded-full border border-border/40 bg-gradient-to-r from-primary/10 to-primary/5 px-3 py-1.5 text-xs font-medium w-fit mb-3">
          <div className="mr-2 h-1.5 w-1.5 rounded-full bg-primary animate-pulse"></div>
          Yükleniyor...
        </div>
        <div className="h-10 bg-gradient-to-r from-muted to-transparent rounded animate-pulse"></div>
        <div className="h-5 bg-muted/50 rounded animate-pulse w-2/3"></div>
      </div>
      <div className="rounded-2xl bg-gradient-to-b from-card/50 to-card border border-border/50 p-8">
        <div className="space-y-6">
          <div className="space-y-2">
            <div className="h-4 bg-muted rounded animate-pulse w-1/4"></div>
            <div className="h-12 bg-muted/50 rounded-xl animate-pulse"></div>
          </div>
          <div className="space-y-2">
            <div className="h-4 bg-muted rounded animate-pulse w-1/4"></div>
            <div className="h-12 bg-muted/50 rounded-xl animate-pulse"></div>
          </div>
          <div className="h-12 bg-primary/20 rounded-xl animate-pulse"></div>
        </div>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <div className="container relative min-h-screen flex-col items-center justify-center grid lg:max-w-none lg:grid-cols-2 lg:px-0 bg-background">
      {/* Left side - Brand Panel with smooth transition */}
      <div className="relative overflow-hidden">
        <AuthBrandPanel />
      </div>
      
      {/* Right side - Login Form with enhanced spacing and centering */}
      <div className="lg:p-8 flex items-center justify-center">
        <div className="mx-auto flex w-full flex-col justify-center space-y-6 px-4 sm:w-[450px] sm:px-0">
          <Suspense fallback={<LoginFormFallback />}>
            <LoginForm />
          </Suspense>
        </div>
      </div>
    </div>
  );
}
