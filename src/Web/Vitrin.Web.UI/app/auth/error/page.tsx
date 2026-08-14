"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { AlertCircle, Home, ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";

const ERROR_MESSAGES: Record<string, string> = {
  Configuration: "Kimlik doğrulama yapılandırması hatalı.",
  AccessDenied: "Bu kaynağa erişim izniniz yok.",
  Verification: "Doğrulama işlemi başarısız oldu. Lütfen tekrar deneyin.",
  SigninError: "Giriş işlemi sırasında bir hata oluştu.",
  OAuthSignin: "OAuth sağlayıcısına bağlanırken hata oluştu.",
  OAuthCallback: "OAuth geri çağrısı başarısız oldu.",
  OAuthCreateAccount: "OAuth hesabı oluşturulamadı.",
  EmailCreateAccount: "E-posta hesabı oluşturulamadı.",
  Callback: "Geri çağrı işlemi başarısız oldu.",
  OAuthAccountNotLinked: "Bu hesap daha önce farklı bir yöntemle kaydolmuş. Lütfen orijinal giriş yönteminizi kullanın.",
  EmailSignin: "E-posta gönderimi başarısız oldu.",
  CredentialsSignin: "Giriş bilgileri hatalı. E-posta ve şifrenizi kontrol edin.",
  SessionRequired: "Bu sayfayı görüntülemek için giriş yapmanız gerekiyor.",
  default: "Kimlik doğrulama işlemi sırasında beklenmeyen bir hata oluştu."
};

function AuthErrorContent() {
  const searchParams = useSearchParams();
  const error = searchParams.get("error");
  const errorMessage = ERROR_MESSAGES[error || ""] || ERROR_MESSAGES.default;

  return (
    <main className="flex min-h-screen items-center justify-center px-4 py-8">
      <div className="w-full max-w-md">
        <div className="rounded-3xl border border-border bg-card p-8 text-center shadow-sm">
          <div className="mb-6 flex justify-center">
            <div className="rounded-full bg-destructive/10 p-3">
              <AlertCircle className="h-8 w-8 text-destructive" />
            </div>
          </div>
          
          <h1 className="mb-3 text-2xl font-bold text-foreground">
            Giriş Yapılamadı
          </h1>
          
          <p className="mb-8 text-muted-foreground">
            {errorMessage}
          </p>
          
          <div className="flex flex-col gap-3 sm:flex-row">
            <Button asChild variant="outline" className="flex-1">
              <Link href="/login">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Tekrar Dene
              </Link>
            </Button>
            
            <Button asChild className="flex-1">
              <Link href="/">
                <Home className="mr-2 h-4 w-4" />
                Ana Sayfa
              </Link>
            </Button>
          </div>
          
          {error && (
            <details className="mt-6 text-left">
              <summary className="cursor-pointer text-sm text-muted-foreground">
                Teknik Detaylar
              </summary>
              <code className="mt-2 block rounded bg-muted p-2 text-xs">
                Hata Kodu: {error}
              </code>
            </details>
          )}
        </div>
      </div>
    </main>
  );
}

export default function AuthErrorPage() {
  return (
    <Suspense fallback={
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-muted-foreground">Yükleniyor...</div>
      </div>
    }>
      <AuthErrorContent />
    </Suspense>
  );
}