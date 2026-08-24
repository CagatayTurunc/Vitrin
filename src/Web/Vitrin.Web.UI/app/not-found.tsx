import Link from "next/link";
import { Compass, Home, Search, Sparkles } from "lucide-react";
import type { Metadata } from "next";
import { BackButton } from "@/components/back-button";

export const metadata: Metadata = {
  title: "Sayfa Bulunamadı — Vitrin",
  description: "Aradığın sayfaya ulaşılamadı. Ana sayfaya dönebilir veya ürünleri keşfedebilirsin.",
  robots: { index: false, follow: true },
};

export default function NotFound() {
  return (
    <main className="flex min-h-[80vh] flex-col items-center justify-center px-4 text-center">
      {/* 404 büyük sayı */}
      <div className="relative select-none">
        <span
          className="text-[9rem] font-black leading-none tracking-tighter text-muted/30 sm:text-[12rem]"
          aria-hidden="true"
        >
          404
        </span>
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="rounded-2xl bg-emerald-500/10 p-4">
            <Compass className="h-12 w-12 text-emerald-600" />
          </div>
        </div>
      </div>

      {/* Mesaj */}
      <h1 className="mt-6 text-2xl font-extrabold sm:text-3xl">
        Bu sayfa yok gibi görünüyor
      </h1>
      <p className="mx-auto mt-3 max-w-md text-muted-foreground">
        Aradığın sayfa kaldırılmış, taşınmış ya da hiç var olmamış olabilir.
        Aşağıdaki bağlantılardan devam edebilirsin.
      </p>

      {/* Aksiyonlar */}
      <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:justify-center">
        <Link
          href="/"
          className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-6 py-3 font-semibold text-white hover:bg-emerald-700 transition-colors"
        >
          <Home className="h-4 w-4" />
          Ana Sayfaya Dön
        </Link>
        <Link
          href="/discover"
          className="inline-flex items-center gap-2 rounded-xl border border-border px-6 py-3 font-semibold hover:bg-muted transition-colors"
        >
          <Sparkles className="h-4 w-4 text-emerald-600" />
          Ürünleri Keşfet
        </Link>
        <Link
          href="/search"
          className="inline-flex items-center gap-2 rounded-xl border border-border px-6 py-3 font-semibold hover:bg-muted transition-colors"
        >
          <Search className="h-4 w-4" />
          Arama Yap
        </Link>
      </div>

      {/* Geri butonu — client component, onClick gerektiriyor */}
      <BackButton className="mt-6 inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors" />
    </main>
  );
}
