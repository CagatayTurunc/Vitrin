import { Metadata } from "next";
import { LoginForm } from "@/components/auth/login-form";
import { AuthBrandPanel } from "@/components/auth-brand-panel";

export const metadata: Metadata = {
  title: "Giriş Yap - Vitrin",
  description: "Vitrin hesabınıza giriş yapın.",
};

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
          <LoginForm />
        </div>
      </div>
    </div>
  );
}
