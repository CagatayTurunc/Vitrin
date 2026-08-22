import type { Metadata } from "next";
import { ForgotPasswordForm } from "@/components/auth/forgot-password-form";

export const metadata: Metadata = {
  title: "Şifreni Yenile — Vitrin",
  description: "Vitrin hesabınızın şifresini sıfırlamak için e-posta adresinizi girin.",
};

export default function ForgotPasswordPage() {
  return <ForgotPasswordForm />;
}
