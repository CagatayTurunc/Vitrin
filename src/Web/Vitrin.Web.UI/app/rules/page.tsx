import { Shield, CheckCircle2, XCircle, AlertTriangle } from "lucide-react";

export const metadata = {
  title: "Topluluk Kuralları — Vitrin",
  description: "Vitrin topluluğunun sağlıklı işleyişi için belirlediğimiz kurallar.",
};

const dos = [
  "Gerçek ürünler ve projeler paylaşın",
  "Yapıcı ve saygılı geri bildirim verin",
  "Kendi ürününüzü dürüstçe tanıtın",
  "Topluluk üyelerine destek olun",
  "Kategorileri doğru seçin",
  "Oy verirken değer odaklı düşünün",
];

const donts = [
  "Sahte oy veya hesap kullanmayın",
  "Spam veya yinelenen içerik yayınlamayın",
  "Başkalarının ürünlerini karalayan yorumlar yazmayın",
  "Nefret söylemi veya ayrımcı dil kullanmayın",
  "Kötü amaçlı yazılım veya zararlı içerik paylaşmayın",
  "Platformu reklam amaçlı kötüye kullanmayın",
];

const sections = [
  {
    icon: Shield,
    title: "Genel İlkeler",
    content:
      "Vitrin, açık ve kapsayıcı bir topluluk ortamını hedefler. Her üye, diğer kullanıcılara saygıyla davranmakla yükümlüdür. Tartışmalı konularda bile yapıcı bir dil kullanılması esastır.",
  },
  {
    icon: AlertTriangle,
    title: "İçerik Standartları",
    content:
      "Paylaşılan ürünlerin gerçek ve işlevsel olması beklenir. Yanıltıcı bilgi, abartılı iddialar veya kullanıcıları yanlış yönlendiren içerikler kabul edilmez. Admin ekibi, uygunsuz içerikleri kaldırma hakkını saklı tutar.",
  },
  {
    icon: CheckCircle2,
    title: "Oy Politikası",
    content:
      "Oylar gerçek kullanıcı deneyimlerini yansıtmalıdır. Organize oy manipülasyonu, botlar veya sahte hesaplarla oy artırmak yasaktır. Bu tür davranışlar tespit edildiğinde hesap askıya alınır.",
  },
];

export default function RulesPage() {
  return (
    <main className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b border-border bg-muted/20">
        <div className="mx-auto max-w-3xl px-4 py-16 text-center">
          <div className="w-12 h-12 rounded-2xl bg-emerald-500/10 flex items-center justify-center mx-auto mb-4">
            <Shield className="w-6 h-6 text-emerald-500" />
          </div>
          <h1 className="text-4xl font-extrabold text-foreground mb-4">Topluluk Kuralları</h1>
          <p className="text-muted-foreground max-w-xl mx-auto">
            Sağlıklı ve pozitif bir topluluk için herkesin uymasını beklediğimiz temel kurallar.
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-3xl px-4 py-12 space-y-10">
        {/* Sections */}
        {sections.map((s) => (
          <div key={s.title} className="bg-card border border-border rounded-2xl p-6">
            <div className="flex items-center gap-3 mb-3">
              <div className="w-8 h-8 rounded-xl bg-emerald-500/10 flex items-center justify-center">
                <s.icon className="w-4 h-4 text-emerald-500" />
              </div>
              <h2 className="font-bold text-foreground">{s.title}</h2>
            </div>
            <p className="text-sm text-muted-foreground leading-relaxed">{s.content}</p>
          </div>
        ))}

        {/* Do & Don't */}
        <div className="grid md:grid-cols-2 gap-6">
          <div className="bg-card border border-border rounded-2xl p-6">
            <h3 className="font-bold text-foreground mb-4 flex items-center gap-2">
              <CheckCircle2 className="w-5 h-5 text-emerald-500" /> Yapabilirsiniz
            </h3>
            <ul className="space-y-3">
              {dos.map((item) => (
                <li key={item} className="flex items-start gap-2 text-sm text-muted-foreground">
                  <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 mt-1.5 shrink-0" />
                  {item}
                </li>
              ))}
            </ul>
          </div>

          <div className="bg-card border border-border rounded-2xl p-6">
            <h3 className="font-bold text-foreground mb-4 flex items-center gap-2">
              <XCircle className="w-5 h-5 text-red-500" /> Yapmamalısınız
            </h3>
            <ul className="space-y-3">
              {donts.map((item) => (
                <li key={item} className="flex items-start gap-2 text-sm text-muted-foreground">
                  <div className="w-1.5 h-1.5 rounded-full bg-red-500 mt-1.5 shrink-0" />
                  {item}
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* Footer note */}
        <div className="text-center text-sm text-muted-foreground border-t border-border pt-8">
          Bu kuralları ihlal ettiğini düşündüğün içerikleri{" "}
          <a href="/contact" className="text-emerald-500 hover:underline">
            bize bildirin
          </a>
          . Son güncelleme: Temmuz 2026.
        </div>
      </div>
    </main>
  );
}
