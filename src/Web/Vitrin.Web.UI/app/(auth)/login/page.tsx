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
      <AuthBrandPanel />
      <div className="lg:p-8 flex items-center justify-center">
        <div className="mx-auto flex w-full flex-col justify-center space-y-6 sm:w-[400px]">
          <LoginForm />
        </div>
      </div>
    </div>
  );
}
