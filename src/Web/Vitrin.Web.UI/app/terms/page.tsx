import { FileText } from "lucide-react";

export const metadata = {
  title: "Kullanım Koşulları — Vitrin",
  description: "Vitrin platformunun kullanım koşulları ve şartları.",
};

const sections = [
  {
    title: "1. Hizmet Kullanımı",
    content: `Vitrin platformunu kullanarak bu koşulları kabul etmiş sayılırsınız. Platform, yalnızca yasal amaçlar için kullanılabilir. Herhangi bir yasadışı, zararlı veya topluluk kurallarına aykırı faaliyet yasaktır. Vitrin, hizmet koşullarını ihlal eden hesapları askıya alma veya silme hakkını saklı tutar.`,
  },
  {
    title: "2. Hesap Sorumlulukları",
    content: `Hesabınızın güvenliğinden siz sorumlusunuz. Hesap bilgilerinizi üçüncü taraflarla paylaşmamalısınız. Hesabınızla yapılan tüm işlemlerden siz sorumlu tutulursunuz. Hesabınızın yetkisiz kullanıldığını fark ederseniz derhal bizimle iletişime geçin.`,
  },
  {
    title: "3. İçerik Politikası",
    content: `Platforma yüklediğiniz içerikler (ürünler, yorumlar, profil bilgileri) için telif hakkı ve sorumluluk size aittir. Başkalarına ait içerikleri izinsiz paylaşmak yasaktır. Vitrin, içerikleri moderasyon amacıyla inceleme hakkına sahiptir. Uygunsuz içerikler bildirim yapılmaksızın kaldırılabilir.`,
  },
  {
    title: "4. Oy ve Sıralama",
    content: `Oy mekanizması, gerçek kullanıcı tercihlerini yansıtmak için tasarlanmıştır. Organize oy manipülasyonu, bot kullanımı veya sahte hesaplarla sıralamayı etkilemeye çalışmak kesinlikle yasaktır. Bu tür davranışlar tespit edildiğinde hesap kalıcı olarak kapatılabilir.`,
  },
  {
    title: "5. Fikri Mülkiyet",
    content: `Vitrin markası, logosu ve platform tasarımı telif hakkıyla korunmaktadır. Kullanıcılar, Vitrin'in izni olmaksızın platform içeriğini ticari amaçla kullanamaz. Maker'lar, kendi ürün sayfalarındaki içeriklerin haklarını elinde bulundurur.`,
  },
  {
    title: "6. Sorumluluk Sınırlaması",
    content: `Vitrin, platform üzerinden erişilen üçüncü taraf ürün veya hizmetlerden sorumlu tutulamaz. Platform, "olduğu gibi" sunulmaktadır ve kesintisiz erişim garantisi verilmemektedir. Vitrin'in zararlardan sorumluluğu, yasal izin verilen azami ölçüde sınırlandırılmıştır.`,
  },
  {
    title: "7. Değişiklikler",
    content: `Vitrin, kullanım koşullarını önceden bildirmeksizin güncelleme hakkını saklı tutar. Önemli değişiklikler e-posta veya platform bildirimi yoluyla duyurulacaktır. Değişiklik sonrası platformu kullanmaya devam etmeniz, yeni koşulları kabul ettiğiniz anlamına gelir.`,
  },
];

export default function TermsPage() {
  return (
    <main className="min-h-screen bg-background">
      <div className="border-b border-border bg-muted/20">
        <div className="mx-auto max-w-3xl px-4 py-16">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 flex items-center justify-center">
              <FileText className="w-5 h-5 text-emerald-500" />
            </div>
            <div>
              <h1 className="text-3xl font-extrabold text-foreground">Kullanım Koşulları</h1>
              <p className="text-sm text-muted-foreground">Son güncelleme: Temmuz 2026</p>
            </div>
          </div>
          <p className="text-muted-foreground">
            Bu kullanım koşulları, Vitrin platformunu kullanan tüm kullanıcılar için geçerlidir.
            Lütfen dikkatlice okuyun.
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-3xl px-4 py-12">
        <div className="space-y-8">
          {sections.map((s) => (
            <div key={s.title} className="border-b border-border pb-8 last:border-0">
              <h2 className="text-lg font-bold text-foreground mb-3">{s.title}</h2>
              <p className="text-sm text-muted-foreground leading-relaxed">{s.content}</p>
            </div>
          ))}
        </div>

        <div className="mt-12 bg-card border border-border rounded-2xl p-6 text-center">
          <p className="text-sm text-muted-foreground">
            Bu koşullar hakkında sorularınız için{" "}
            <a href="/contact" className="text-emerald-500 hover:underline">
              bizimle iletişime geçin
            </a>
            .
          </p>
        </div>
      </div>
    </main>
  );
}
