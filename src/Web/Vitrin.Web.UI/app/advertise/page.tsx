import type { Metadata } from "next";
import Link from "next/link";
import { BarChart3, Mail, Megaphone, Target, Users, Zap } from "lucide-react";
import { Button } from "@/components/ui/button";

export const metadata: Metadata = {
  title: "Reklamver — Vitrin",
  description: "Vitrin'de ürününüzü öne çıkarın. Türkiye'nin teknoloji meraklıları topluluğuna ulaşın.",
};

const adFormats = [
  {
    icon: Megaphone,
    title: "Öne Çıkarılan Lansman",
    description:
      "Ürününüz ana sayfanın üst sıralarında \"Sponsorlu\" etiketiyle listelenir. Günlük aktif kullanıcıların tamamı tarafından görülür.",
    price: "Fiyat için iletişime geçin",
    highlight: true,
  },
  {
    icon: Target,
    title: "Kategori Sponsorluğu",
    description:
      "Hedef kitlenizle örtüşen bir kategoride logo ve bağlantınız öne çıkar. Yapay Zeka, SaaS, Geliştirici Araçları ve daha fazlası.",
    price: "Fiyat için iletişime geçin",
    highlight: false,
  },
  {
    icon: Mail,
    title: "Bülten Sponsorluğu",
    description:
      "Vitrin'in haftalık bülteninde ürününüz tanıtım alanı alır. Binlerce teknoloji meraklısına doğrudan ulaşın.",
    price: "Fiyat için iletişime geçin",
    highlight: false,
  },
];

const stats = [
  { value: "50K+", label: "Aylık Ziyaretçi" },
  { value: "10K+", label: "Kayıtlı Kullanıcı" },
  { value: "2K+", label: "Keşfedilen Ürün" },
  { value: "%78", label: "Teknik Profil Oranı" },
];

export default function AdvertisePage() {
  return (
    <main className="min-h-screen bg-background">
      {/* Hero */}
      <section className="relative overflow-hidden border-b border-border">
        <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 via-transparent to-transparent pointer-events-none" />
        <div className="mx-auto max-w-4xl px-4 py-24 sm:py-32 text-center">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-sm font-medium mb-6">
            <Zap className="w-4 h-4" />
            Reklam & Sponsorluk
          </div>
          <h1 className="text-4xl sm:text-6xl font-extrabold tracking-tight text-foreground mb-6 leading-tight">
            Türkiye&apos;nin teknoloji topluluğuna{" "}
            <span className="text-emerald-500">ulaşın</span>
          </h1>
          <p className="text-lg sm:text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed mb-8">
            Vitrin, geliştiriciler, tasarımcılar ve teknoloji meraklılarından oluşan aktif bir topluluk.
            Ürününüzü tam hedef kitlenizin önüne çıkarın.
          </p>
          <Link href="/contact">
            <Button className="rounded-full bg-emerald-500 hover:bg-emerald-600 text-white px-8 h-12 font-semibold text-base">
              <Mail className="mr-2 w-5 h-5" />
              Bizimle iletişime geçin
            </Button>
          </Link>
        </div>
      </section>

      {/* Stats */}
      <section className="border-b border-border bg-muted/30">
        <div className="mx-auto max-w-5xl px-4 py-12 grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
          {stats.map((s) => (
            <div key={s.label}>
              <div className="text-3xl sm:text-4xl font-extrabold text-emerald-500 mb-1">{s.value}</div>
              <div className="text-sm text-muted-foreground">{s.label}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Audience */}
      <section className="mx-auto max-w-4xl px-4 py-20">
        <div className="text-center mb-12">
          <h2 className="text-3xl font-extrabold text-foreground mb-3">Kitleyi tanıyın</h2>
          <p className="text-muted-foreground max-w-xl mx-auto">
            Vitrin kullanıcıları teknoloji ürünlerini erken benimseyen, satın alma kararlarını
            etkileyen profesyonellerden oluşur.
          </p>
        </div>
        <div className="grid sm:grid-cols-3 gap-6">
          {[
            { icon: Users, title: "Maker & Geliştirici", desc: "Kendi ürünlerini inşa eden yazılımcılar ve girişimciler." },
            { icon: BarChart3, title: "Ürün Yöneticisi", desc: "Yeni araçları araştıran ve ekiplerine öneren PM'ler." },
            { icon: Target, title: "Erken Benimseyen", desc: "Piyasaya yeni çıkan ürünleri deneyen teknoloji meraklıları." },
          ].map((item) => (
            <div
              key={item.title}
              className="bg-card border border-border rounded-2xl p-6 hover:border-emerald-500/30 transition-colors"
            >
              <item.icon className="w-6 h-6 text-emerald-500 mb-3" />
              <h3 className="font-semibold text-foreground mb-2">{item.title}</h3>
              <p className="text-sm text-muted-foreground leading-relaxed">{item.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Ad Formats */}
      <section className="border-t border-border bg-muted/20">
        <div className="mx-auto max-w-4xl px-4 py-20">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-extrabold text-foreground mb-3">Reklam formatları</h2>
            <p className="text-muted-foreground max-w-xl mx-auto">
              Her bütçe ve hedefe uygun seçeneklerle ürününüzü öne çıkarın.
            </p>
          </div>
          <div className="grid md:grid-cols-3 gap-6">
            {adFormats.map((format) => (
              <div
                key={format.title}
                className={`bg-card border rounded-2xl p-6 flex flex-col gap-4 transition-colors ${
                  format.highlight
                    ? "border-emerald-500/40 ring-1 ring-emerald-500/20"
                    : "border-border hover:border-emerald-500/30"
                }`}
              >
                {format.highlight && (
                  <div className="inline-flex w-fit items-center gap-1 rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-semibold text-emerald-600">
                    <Zap className="w-3 h-3" /> En Popüler
                  </div>
                )}
                <format.icon className="w-6 h-6 text-emerald-500" />
                <div>
                  <h3 className="font-bold text-foreground mb-1">{format.title}</h3>
                  <p className="text-sm text-muted-foreground leading-relaxed">{format.description}</p>
                </div>
                <div className="mt-auto pt-2 border-t border-border text-xs font-semibold text-emerald-600">
                  {format.price}
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="border-t border-border">
        <div className="mx-auto max-w-3xl px-4 py-20 text-center">
          <h2 className="text-3xl font-extrabold text-foreground mb-4">
            Hazır mısınız?
          </h2>
          <p className="text-muted-foreground max-w-xl mx-auto mb-8">
            Reklam seçenekleri, fiyatlandırma ve uygunluk takvimi için bizimle iletişime geçin.
            24 saat içinde yanıt veriyoruz.
          </p>
          <Link href="/contact">
            <Button className="rounded-full bg-emerald-500 hover:bg-emerald-600 text-white px-8 h-12 font-semibold">
              İletişime Geç
            </Button>
          </Link>
        </div>
      </section>
    </main>
  );
}
