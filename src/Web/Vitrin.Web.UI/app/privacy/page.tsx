import { Lock } from "lucide-react";

export const metadata = {
  title: "Gizlilik Politikası — Vitrin",
  description: "Vitrin'in kişisel verileri nasıl topladığı, işlediği ve koruduğu hakkında bilgi.",
};

const sections = [
  {
    title: "1. Toplanan Veriler",
    content: `Vitrin, kayıt sırasında ad, e-posta adresi ve şifre bilgilerini toplar. Profil sayfanıza eklediğiniz ek bilgiler (avatar, biyografi, sosyal medya linkleri) isteğe bağlıdır. Platform kullanımınız sırasında IP adresi, tarayıcı bilgisi ve sayfa görüntüleme verileri otomatik olarak kaydedilebilir.`,
  },
  {
    title: "2. Verilerin Kullanımı",
    content: `Toplanan veriler; hesap yönetimi, platform güvenliği, kişiselleştirilmiş içerik sunumu ve topluluk istatistikleri için kullanılır. E-posta adresiniz yalnızca hesap doğrulama, önemli platform bildirimleri ve tercihlerinize göre haber bülteni için kullanılır. Verileriniz üçüncü taraflarla ticari amaçla paylaşılmaz.`,
  },
  {
    title: "3. Çerezler",
    content: `Vitrin, oturum yönetimi ve kullanıcı deneyimini iyileştirmek için çerezler kullanır. Zorunlu çerezler platformun işlevselliği için gereklidir. Analitik çerezler, anonim kullanım verilerini toplamak için kullanılır. Tarayıcı ayarlarınızdan çerezleri yönetebilirsiniz.`,
  },
  {
    title: "4. Veri Güvenliği",
    content: `Şifreler bcrypt algoritmasıyla şifrelenerek saklanır. Tüm veri iletimi HTTPS protokolüyle şifrelenir. Veritabanı erişimleri sınırlı yetkili personelle kısıtlıdır. Olası bir veri ihlali durumunda kullanıcılar en kısa sürede bilgilendirilir.`,
  },
  {
    title: "5. Üçüncü Taraf Hizmetler",
    content: `Vitrin, Google ve GitHub OAuth ile giriş imkânı sunar. Bu servisler kendi gizlilik politikalarına tabidir. Cloudinary, görsel yükleme için kullanılmaktadır. Vercel Analytics, anonim site istatistikleri için kullanılabilir.`,
  },
  {
    title: "6. Kullanıcı Hakları",
    content: `Hesabınızdaki kişisel verilere erişim, düzenleme ve silme hakkına sahipsiniz. Profil ayarlarından verilerinizi güncelleyebilirsiniz. Hesabınızı tamamen silmek için destek@vitrin.app adresine yazabilirsiniz. KVKK kapsamındaki haklarınız için aşağıdaki aydınlatma metnini incelemenizi öneririz.`,
  },
  {
    title: "7. Çocukların Gizliliği",
    content: `Vitrin, 13 yaşın altındaki bireylerden bilerek kişisel veri toplamaz. Ebeveyn veya vasi olarak küçüğünüzün hesabı bulunduğundan şüpheleniyorsanız derhal bizimle iletişime geçin.`,
  },
];

export default function PrivacyPage() {
  return (
    <main className="min-h-screen bg-background">
      <div className="border-b border-border bg-muted/20">
        <div className="mx-auto max-w-3xl px-4 py-16">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 flex items-center justify-center">
              <Lock className="w-5 h-5 text-emerald-500" />
            </div>
            <div>
              <h1 className="text-3xl font-extrabold text-foreground">Gizlilik Politikası</h1>
              <p className="text-sm text-muted-foreground">Son güncelleme: Temmuz 2026</p>
            </div>
          </div>
          <p className="text-muted-foreground">
            Kişisel verilerinizin güvenliği bizim için önceliklidir. Bu politika, verilerinizi nasıl
            işlediğimizi şeffaf biçimde açıklar.
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
            Gizlilik hakkında sorularınız için{" "}
            <a href="mailto:privacy@vitrin.app" className="text-emerald-500 hover:underline">
              privacy@vitrin.app
            </a>{" "}
            adresine yazabilirsiniz.
          </p>
        </div>
      </div>
    </main>
  );
}
