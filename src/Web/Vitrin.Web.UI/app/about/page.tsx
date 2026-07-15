import Link from "next/link";
import { Sparkles, Users, Zap, Globe, Heart, ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";

export const metadata = {
  title: "Hakkımızda — Vitrin",
  description: "Türkiye'nin ürün keşif platformu Vitrin hakkında her şey.",
};

const stats = [
  { value: "10K+", label: "Kayıtlı Kullanıcı" },
  { value: "2K+", label: "Keşfedilen Ürün" },
  { value: "50K+", label: "Aylık Ziyaretçi" },
  { value: "100+", label: "Onaylı Maker" },
];

const values = [
  {
    icon: Zap,
    title: "Hız & Keşif",
    desc: "En yeni Türk ürünlerini anında keşfet, ilk sen dene ve geri bildirim ver.",
  },
  {
    icon: Users,
    title: "Topluluk Odaklı",
    desc: "Geliştiriciler, tasarımcılar ve meraklılar bir arada. Fikirler burada büyür.",
  },
  {
    icon: Globe,
    title: "Yerli & Özgün",
    desc: "Türkiye'den çıkan ürünlere platform sağlıyor, yerli ekosistemi destekliyoruz.",
  },
  {
    icon: Heart,
    title: "Şeffaf & Açık",
    desc: "Algoritmalar değil, gerçek oylar öne çıkar. Topluluk karar verir.",
  },
];

export default function AboutPage() {
  return (
    <main className="min-h-screen bg-background">
      {/* Hero */}
      <section className="relative overflow-hidden border-b border-border">
        <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 via-transparent to-transparent pointer-events-none" />
        <div className="mx-auto max-w-4xl px-4 py-24 sm:py-32 text-center">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-sm font-medium mb-6">
            <Sparkles className="w-4 h-4" />
            Türkiye&apos;nin Ürün Platformu
          </div>
          <h1 className="text-4xl sm:text-6xl font-extrabold tracking-tight text-foreground mb-6 leading-tight">
            Harika ürünler{" "}
            <span className="text-emerald-500">gün yüzüne çıksın</span>
          </h1>
          <p className="text-lg sm:text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
            Vitrin, Türkiye&apos;deki geliştiriciler ve girişimcilerin yarattığı ürünleri
            dünyaya açmak için kurulmuş bir topluluk platformudur.
          </p>
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

      {/* Mission */}
      <section className="mx-auto max-w-4xl px-4 py-20">
        <div className="grid md:grid-cols-2 gap-12 items-center">
          <div>
            <h2 className="text-3xl font-extrabold text-foreground mb-4">Misyonumuz</h2>
            <p className="text-muted-foreground leading-relaxed mb-4">
              Product Hunt&apos;tan ilham alarak başladık ama kendi yolumuzda ilerliyoruz.
              Amacımız Türkiye&apos;deki maker ekosistemini güçlendirmek, ürünlere görünürlük
              kazandırmak ve topluluğun sesini yükseltmek.
            </p>
            <p className="text-muted-foreground leading-relaxed">
              Her gün yüzlerce meraklı kullanıcı en iyi ürünü oylayarak öne çıkarıyor.
              Sen de bu topluluğun bir parçası ol.
            </p>
          </div>
          <div className="grid grid-cols-2 gap-4">
            {values.map((v) => (
              <div
                key={v.title}
                className="bg-card border border-border rounded-2xl p-5 hover:border-emerald-500/30 transition-colors"
              >
                <v.icon className="w-5 h-5 text-emerald-500 mb-3" />
                <h3 className="font-semibold text-foreground text-sm mb-1">{v.title}</h3>
                <p className="text-xs text-muted-foreground leading-relaxed">{v.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Team */}
      <section className="border-t border-border bg-muted/20">
        <div className="mx-auto max-w-4xl px-4 py-20 text-center">
          <h2 className="text-3xl font-extrabold text-foreground mb-4">Küçük ama tutkulu bir ekip</h2>
          <p className="text-muted-foreground max-w-xl mx-auto mb-10">
            Vitrin, ürün tutkunu geliştiricilerden oluşan küçük bir çekirdek ekip tarafından
            hayata geçirildi. Her gün daha iyisini yapmak için çalışıyoruz.
          </p>
          <Link href="/submit">
            <Button className="rounded-full bg-emerald-500 hover:bg-emerald-600 text-white px-8 h-12 font-semibold">
              Sen de ekosisteme katıl <ArrowRight className="ml-2 w-4 h-4" />
            </Button>
          </Link>
        </div>
      </section>
    </main>
  );
}
