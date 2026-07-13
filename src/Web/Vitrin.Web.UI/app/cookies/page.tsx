import { Cookie } from "lucide-react";

export const metadata = {
  title: "Çerez Politikası — Vitrin",
  description: "Vitrin'in çerez kullanımı hakkında detaylı bilgi.",
};

const cookieTypes = [
  {
    name: "Zorunlu Çerezler",
    badge: "Her zaman aktif",
    badgeColor: "bg-emerald-500/10 text-emerald-500",
    desc: "Platformun temel işlevlerini sağlamak için gereklidir. Oturum yönetimi, güvenlik ve tercih kaydı için kullanılır. Bu çerezler devre dışı bırakılamaz.",
    examples: [
      { name: "next-auth.session-token", purpose: "Oturum kimlik doğrulaması", duration: "30 gün" },
      { name: "next-auth.csrf-token", purpose: "CSRF koruması", duration: "Oturum süresi" },
      { name: "__Secure-next-auth", purpose: "Güvenli oturum", duration: "30 gün" },
    ],
  },
  {
    name: "Analitik Çerezler",
    badge: "İsteğe bağlı",
    badgeColor: "bg-blue-500/10 text-blue-500",
    desc: "Platform kullanımını anonim olarak analiz etmek için kullanılır. Hangi sayfaların ziyaret edildiği ve kullanıcı davranışları hakkında istatistik toplar. Kişisel kimlik bilgisi içermez.",
    examples: [
      { name: "_va", purpose: "Vercel Analytics", duration: "1 yıl" },
      { name: "_vt", purpose: "Ziyaretçi takibi (anonim)", duration: "90 gün" },
    ],
  },
  {
    name: "Tercih Çerezleri",
    badge: "İsteğe bağlı",
    badgeColor: "bg-purple-500/10 text-purple-500",
    desc: "Tema tercihi (koyu/açık mod), dil ayarları gibi kullanıcı özelleştirmelerini hatırlamak için kullanılır.",
    examples: [
      { name: "theme", purpose: "Koyu/açık tema tercihi", duration: "1 yıl" },
      { name: "locale", purpose: "Dil tercihi", duration: "1 yıl" },
    ],
  },
];

export default function CookiesPage() {
  return (
    <main className="min-h-screen bg-background">
      <div className="border-b border-border bg-muted/20">
        <div className="mx-auto max-w-3xl px-4 py-16">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 flex items-center justify-center">
              <Cookie className="w-5 h-5 text-emerald-500" />
            </div>
            <div>
              <h1 className="text-3xl font-extrabold text-foreground">Çerez Politikası</h1>
              <p className="text-sm text-muted-foreground">Son güncelleme: Temmuz 2026</p>
            </div>
          </div>
          <p className="text-muted-foreground">
            Vitrin, daha iyi bir deneyim sunmak için çerezler kullanır. Bu sayfa çerezlerin
            nasıl kullanıldığını açıklar.
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-3xl px-4 py-12 space-y-8">
        <div className="bg-card border border-border rounded-2xl p-6">
          <h2 className="font-bold text-foreground mb-3">Çerez Nedir?</h2>
          <p className="text-sm text-muted-foreground leading-relaxed">
            Çerezler, web siteleri tarafından tarayıcınıza kaydedilen küçük metin dosyalarıdır.
            Oturum açık kalması, tercihlerinizin hatırlanması ve site performansının ölçülmesi
            gibi işlevler için kullanılır.
          </p>
        </div>

        {cookieTypes.map((ct) => (
          <div key={ct.name} className="bg-card border border-border rounded-2xl p-6">
            <div className="flex items-center gap-3 mb-3">
              <h2 className="font-bold text-foreground">{ct.name}</h2>
              <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${ct.badgeColor}`}>
                {ct.badge}
              </span>
            </div>
            <p className="text-sm text-muted-foreground mb-5 leading-relaxed">{ct.desc}</p>

            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-border">
                    <th className="text-left py-2 pr-4 text-xs font-semibold text-muted-foreground">Çerez Adı</th>
                    <th className="text-left py-2 pr-4 text-xs font-semibold text-muted-foreground">Amaç</th>
                    <th className="text-left py-2 text-xs font-semibold text-muted-foreground">Süre</th>
                  </tr>
                </thead>
                <tbody>
                  {ct.examples.map((ex) => (
                    <tr key={ex.name} className="border-b border-border/50 last:border-0">
                      <td className="py-2 pr-4 font-mono text-xs text-foreground">{ex.name}</td>
                      <td className="py-2 pr-4 text-xs text-muted-foreground">{ex.purpose}</td>
                      <td className="py-2 text-xs text-muted-foreground">{ex.duration}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        ))}

        <div className="bg-card border border-border rounded-2xl p-6">
          <h2 className="font-bold text-foreground mb-3">Çerezleri Nasıl Kontrol Ederim?</h2>
          <p className="text-sm text-muted-foreground leading-relaxed">
            Tarayıcı ayarlarınızdan çerezleri yönetebilir veya silebilirsiniz. Zorunlu çerezleri
            devre dışı bırakmak platformun düzgün çalışmamasına neden olabilir. Daha fazla bilgi için
            tarayıcınızın yardım bölümünü inceleyin.
          </p>
        </div>

        <div className="text-center text-sm text-muted-foreground">
          Sorularınız için{" "}
          <a href="/contact" className="text-emerald-500 hover:underline">
            bizimle iletişime geçin
          </a>
          .
        </div>
      </div>
    </main>
  );
}
