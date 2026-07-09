import { Metadata } from "next";
import { RegisterForm } from "@/components/auth/register-form";
import { AuthBrandPanel } from "@/components/auth-brand-panel";

export const metadata: Metadata = {
  title: "Kayıt Ol - Vitrin",
  description: "Vitrin hesabı oluşturarak yeni ürünler keşfedin.",
};

export default function RegisterPage() {
  return (
    <div className="container relative min-h-screen flex-col items-center justify-center grid lg:max-w-none lg:grid-cols-2 lg:px-0 bg-background">
      <AuthBrandPanel />
      <div className="lg:p-8 flex items-center justify-center">
        <div className="mx-auto flex w-full flex-col justify-center space-y-6 sm:w-[400px]">
          <RegisterForm />
        </div>
      </div>
    </div>
  );
}
