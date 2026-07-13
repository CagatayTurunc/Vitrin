import { ShieldCheck } from "lucide-react";

export const metadata = {
  title: "KVKK Aydınlatma Metni — Vitrin",
  description: "6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında aydınlatma metni.",
};

export default function KvkkPage() {
  return (
    <main className="min-h-screen bg-background">
      <div className="border-b border-border bg-muted/20">
        <div className="mx-auto max-w-3xl px-4 py-16">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 flex items-center justify-center">
              <ShieldCheck className="w-5 h-5 text-emerald-500" />
            </div>
            <div>
              <h1 className="text-3xl font-extrabold text-foreground">KVKK Aydınlatma Metni</h1>
              <p className="text-sm text-muted-foreground">Son güncelleme: Temmuz 2026</p>
            </div>
          </div>
          <p className="text-muted-foreground text-sm leading-relaxed">
            6698 sayılı Kişisel Verilerin Korunması Kanunu ("KVKK") uyarınca, kişisel verileriniz
            aşağıda açıklanan kapsamda işlenmektedir.
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-3xl px-4 py-12 space-y-8 text-sm text-muted-foreground leading-relaxed">

        <section className="border-b border-border pb-8">
          <h2 className="text-base font-bold text-foreground mb-3">1. Veri Sorumlusu</h2>
          <p>
            KVKK uyarınca veri sorumlusu sıfatıyla <strong className="text-foreground">Vitrin Teknoloji</strong> ("Vitrin"),
            kişisel verilerinizi aşağıda belirtilen amaçlar çerçevesinde işlemektedir.
          </p>
          <div className="mt-4 bg-card border border-border rounded-xl p-4 space-y-1">
            <p><span className="font-medium text-foreground">E-posta:</span> kvkk@vitrin.app</p>
            <p><span className="font-medium text-foreground">Adres:</span> İstanbul, Türkiye</p>
          </div>
        </section>

        <section className="border-b border-border pb-8">
          <h2 className="text-base font-bold text-foreground mb-3">2. İşlenen Kişisel Veriler</h2>
          <ul className="space-y-2">
            {[
              "Kimlik verileri: Ad, soyad, kullanıcı adı",
              "İletişim verileri: E-posta adresi",
              "Hesap güvenliği: Şifreli (hash'lenmiş) parola",
              "Profil verileri: Avatar görseli, biyografi, sosyal medya linkleri (isteğe bağlı)",
              "İşlem verileri: Eklenen ürünler, oylar, yorumlar",
              "Teknik veriler: IP adresi, tarayıcı bilgisi, oturum verileri",
            ].map((item) => (
              <li key={item} className="flex items-start gap-2">
                <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 mt-1.5 shrink-0" />
                {item}
              </li>
            ))}
          </ul>
        </section>

        <section className="border-b border-border pb-8">
          <h2 className="text-base font-bold text-foreground mb-3">3. İşleme Amaçları ve Hukuki Dayanakları</h2>
          <div className="space-y-4">
            {[
              {
                purpose: "Üyelik ve hesap yönetimi",
                basis: "Sözleşmenin kurulması ve ifası (KVKK m.5/2-c)",
              },
              {
                purpose: "Platform güvenliği ve dolandırıcılığın önlenmesi",
                basis: "Meşru menfaat (KVKK m.5/2-f)",
              },
              {
                purpose: "Hizmet kalitesinin iyileştirilmesi",
                basis: "Meşru menfaat (KVKK m.5/2-f)",
              },
              {
                purpose: "Yasal yükümlülüklerin yerine getirilmesi",
                basis: "Kanuni yükümlülük (KVKK m.5/2-ç)",
              },
              {
                purpose: "Bülten ve bildirim gönderimi",
                basis: "Açık rıza (KVKK m.5/1)",
              },
            ].map((item) => (
              <div key={item.purpose} className="bg-card border border-border rounded-xl p-4">
                <p className="font-medium text-foreground text-sm mb-1">{item.purpose}</p>
                <p className="text-xs">{item.basis}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="border-b border-border pb-8">
          <h2 className="text-base font-bold text-foreground mb-3">4. Verilerin Aktarılması</h2>
          <p>
            Kişisel verileriniz; yalnızca hizmet sunumu için zorunlu olan teknik altyapı sağlayıcılarıyla
            (hosting, e-posta servisi, görsel depolama) KVKK'nın 8. ve 9. maddeleri çerçevesinde
            paylaşılmaktadır. Ticari amaçlı üçüncü taraf aktarımı yapılmamaktadır.
          </p>
        </section>

        <section className="border-b border-border pb-8">
          <h2 className="text-base font-bold text-foreground mb-3">5. Saklama Süreleri</h2>
          <p>
            Kişisel verileriniz, hesabınız aktif olduğu sürece saklanır. Hesap silme talebinde
            bulunmanız durumunda verileriniz 30 gün içinde silinir veya anonim hale getirilir.
            Yasal saklama yükümlülükleri kapsamındaki veriler, ilgili mevzuatta belirtilen
            süreler boyunca tutulur.
          </p>
        </section>

        <section className="border-b border-border pb-8">
          <h2 className="text-base font-bold text-foreground mb-3">6. Veri Sahibinin Hakları (KVKK m.11)</h2>
          <ul className="space-y-2">
            {[
              "Kişisel verilerinizin işlenip işlenmediğini öğrenme",
              "İşlenmişse buna ilişkin bilgi talep etme",
              "İşlenme amacını ve amacına uygun kullanılıp kullanılmadığını öğrenme",
              "Yurt içinde veya yurt dışında verilerin aktarıldığı üçüncü kişileri bilme",
              "Eksik veya yanlış işlenmiş verilerin düzeltilmesini isteme",
              "KVKK'nın 7. maddesinde öngörülen koşullar çerçevesinde silinmesini isteme",
              "İşlemeye itiraz etme ve zararın giderilmesini talep etme",
            ].map((right) => (
              <li key={right} className="flex items-start gap-2">
                <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 mt-1.5 shrink-0" />
                {right}
              </li>
            ))}
          </ul>
          <p className="mt-4">
            Haklarınızı kullanmak için{" "}
            <a href="mailto:kvkk@vitrin.app" className="text-emerald-500 hover:underline">
              kvkk@vitrin.app
            </a>{" "}
            adresine e-posta gönderebilirsiniz.
          </p>
        </section>

        <div className="text-center text-xs text-muted-foreground">
          Bu aydınlatma metni KVKK kapsamındaki yükümlülüklerimiz çerçevesinde hazırlanmıştır.
        </div>
      </div>
    </main>
  );
}
