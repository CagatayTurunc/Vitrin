# Vitrin — SEO & UX Kontrol Listesi

> Instagram'da gördüğümüz "Sitenizi yayına almadan önce Claude'a eklemesini söylemeniz
> gereken 20 şey" listesini projeye karşı tek tek inceledik.
> Zaten mevcut olanları belgeledik, eksik olanları uyguladık, platform için
> anlamsız olanları gerekçesiyle reddettik.

---

## ✅ Zaten Mevcut Olan (Değişiklik Gerekmedi)

### 2. Üst Kısma CTA

`PremiumHero` bileşeninde iki CTA var: **"Ürünleri keşfet"** ve **"Ürününü ekle"**.
Header'da da sabit bir yeşil "Ekle" butonu var. Hem desktop hem mobilde görünür.
Değişiklik gerekmedi.

---

### 3. İç Linkleme

Ürün detay sayfasından kategorilere, maker profiline ve diğer ürünlere link veriliyor.
Footer'da tüm önemli sayfalara link var. `sitemap.ts` tüm ürün ve kategorileri kapsıyor.
Sağlıklı bir iç link yapısı mevcut.

---

### 4. Teşekkür Sayfası / Başarı Mesajı

`app/contact/page.tsx` içinde form submit sonrası **in-place success state** gösteriliyor
("Mesajınız İletildi!"). Ayrı bir `/thank-you` route'una gerek yok — kullanıcı
aynı sayfada anlık geri bildirim alıyor.

---

### 10. robots.txt

`app/robots.ts` var ve doğru yapılandırılmış:
- Admin, dashboard, my-products, settings → `Disallow`
- Tüm public sayfalar → `Allow`
- `sitemap.xml` URL'si dahil

---

### 11 + 12. Benzersiz Meta Başlık & Meta Açıklama

Tüm önemli sayfalarda `export const metadata` veya `generateMetadata` var.
Her sayfa için **ayrı title + description** tanımlı. `layout.tsx`'te global
`metadataBase` de set edilmiş — relative URL'ler otomatik absolute'a çevriliyor.

---

### 17. Google Zengin İçerik (Structured Data / JSON-LD)

`app/product/[slug]/page.tsx`'de `SoftwareApplication` JSON-LD schema'sı var:
- `upvotes`, `commentCount`, `viewCount` → `interactionStatistic`
- `author`, `image`, `datePublished` → tam dolu
- `categories`, `operatingSystem` → BusinessApplication kategorisi

---

### 18. Gizlilik Politikası Sayfası

`app/privacy/page.tsx` var — 7 bölümlü tam Gizlilik Politikası.
Ayrıca `app/kvkk/page.tsx`, `app/terms/page.tsx`, `app/cookies/page.tsx` da mevcut.

---

### 19. Google Search Console (Sitemap)

`app/sitemap.ts` var — dinamik olarak ürünler + kategoriler + 12 statik sayfa kapsıyor.
`changeFrequency` ve `priority` değerleri içerik türüne göre ayarlanmış.

---

### 20. Hakkımızda Sayfası

`app/about/page.tsx` var — misyon, istatistikler (10K+ kullanıcı, 2K+ ürün), değerler
ve ekip bölümü mevcut.

---

## ✅ Uyguladığımız Değişiklikler

### 1. Global 404 Sayfası (app/not-found.tsx)

**Ne değiştirdik:** `app/not-found.tsx` oluşturuldu.

**Neden önemli?**
Daha önce yalnızca `app/product/[slug]/not-found.tsx` vardı — ürün olmayan sayfalarda
(örn. `/hataliyol`, `/kullanici/yok`) Next.js'in varsayılan, markasız 404 sayfası açılıyordu.

**Ne eklendi?**
- Vitrin markasına uygun tasarım
- "Ana Sayfaya Dön", "Ürünleri Keşfet", "Arama Yap" CTA'ları
- Tarayıcı geri butonu (JS ile)
- `robots: { index: false }` metadata (404 sayfası indexlenmemeli)

```
app/not-found.tsx  ← yeni
```

---

### 8. Site Hızı — Image Optimization Açıldı

**Ne değiştirdik:** `next.config.mjs`'deki `images: { unoptimized: true }` kaldırıldı,
yerine `remotePatterns` whitelist eklendi.

**Neden kritik?**
`unoptimized: true` ile Next.js'in tüm image optimization özellikleri kapalıydı:
- WebP/AVIF otomatik dönüşümü yok → görseller olduğundan büyük
- Responsive `srcset` yok → mobilde desktop boyutunda görsel yükleniyor
- Lazy loading optimize edilmiyor
- `priority` attribute'u etkisiz kalıyordu

Docker standalone modunda Next.js image optimization çalışır — `unoptimized` gereksizdi.

**Eklenen `remotePatterns`:**
```js
remotePatterns: [
  { protocol: "https", hostname: "res.cloudinary.com" },       // ürün görselleri
  { protocol: "https", hostname: "avatars.githubusercontent.com" }, // GitHub avatarları
  { protocol: "https", hostname: "lh3.googleusercontent.com" },    // Google avatarları
]
```

```
src/Web/Vitrin.Web.UI/next.config.mjs  ← güncellendi
```

---

### 13. Sosyal Medya Paylaşım Görseli (Global OG Image)

**Ne değiştirdik:** `app/opengraph-image.tsx` oluşturuldu.

**Neden önemli?**
Sadece ürün detay sayfası `generateMetadata` ile ürün görselini og:image olarak set
ediyordu. Ana sayfa, kategori, about, search gibi sayfalar paylaşılınca sosyal medyada
görsel çıkmıyordu — ya boş ön izleme ya da site favicon'u görünüyordu.

**Ne eklendi?**
`app/opengraph-image.tsx` — Next.js `ImageResponse` ile oluşturulan 1200×630px SVG tabanlı
görsel. Vitrin logosu, başlık, açıklama ve alan adını içeriyor. Herhangi bir sayfada
özel OG image yoksa bu fallback olarak kullanılıyor.

```
app/opengraph-image.tsx  ← yeni
```

---

### 5. Breadcrumb (Sayfa İşaret Yolu)

**Ne değiştirdik:**
- `components/breadcrumb.tsx` — yeni genel bileşen oluşturuldu
- `app/category/[slug]/page.tsx` — breadcrumb eklendi
- `app/product/[slug]/page.tsx` — breadcrumb eklendi

**Neden önemli?**
Breadcrumb iki ayrı değer katar:

1. **UX:** Kullanıcı nerede olduğunu anlar, bir üst seviyeye kolayca çıkabilir.
2. **SEO (BreadcrumbList JSON-LD):** Google arama sonuçlarında URL yerine
   `Vitrin > Kategoriler > SaaS` şeklinde breadcrumb gösterimi → tıklama oranı artar.

**Bileşen özellikleri:**
```tsx
<Breadcrumb items={[
  { label: "Kategoriler", href: "/categories" },
  { label: "SaaS" }  // son eleman — href olmaz, aria-current="page"
]} />
```
- Otomatik "Ana Sayfa" başlangıcı
- `aria-current="page"` ile erişilebilirlik
- Her render'da `BreadcrumbList` JSON-LD üretiyor

```
components/breadcrumb.tsx                       ← yeni
app/category/[slug]/page.tsx                    ← breadcrumb eklendi
app/product/[slug]/page.tsx                     ← breadcrumb eklendi
```

---

### 11. Contact Sayfasına Metadata Eklendi

**Ne değiştirdik:**
- `app/contact/contact-form.tsx` — mevcut client component buraya taşındı
- `app/contact/page.tsx` — server wrapper + `metadata` export eklendi

**Neden önemli?**
`"use client"` directive olan dosyalarda Next.js `metadata` export'una izin vermiyor.
Önceki haliyle `/contact` sayfası hiç meta başlık ve açıklama üretmiyordu — Google
bu sayfayı başlıksız indexliyordu.

**Çözüm (Server/Client Split Pattern):**
```
page.tsx (server)          → metadata export + <ContactForm /> render
contact-form.tsx (client)  → "use client" + useState, form logic
```

Bu pattern search, topic, profile gibi diğer `"use client"` sayfalara da uygulanabilir.

```
app/contact/contact-form.tsx  ← yeni (eski page.tsx içeriği buraya)
app/contact/page.tsx          ← server wrapper + metadata (yeniden yazıldı)
```

---

### 16. Resim Alt Etiketi Düzeltildi

**Ne değiştirdik:** `app/category/[slug]/page.tsx`'deki ürün görsellerinde
`alt=""` → `alt={`${product.name} ürün logosu`}` yapıldı.

**Neden önemli?**
Ekran okuyucu kullanan kullanıcılar `alt=""` olan görseli "dekoratif" sayar ve
atlıyor. Ürün logosunun ürün adını içermesi hem erişilebilirlik hem de Google Image
Search indexlemesi için kritik.

```
app/category/[slug]/page.tsx  ← alt="" → alt={product.name + " ürün logosu"}
```

---

## ❌ Uygulamadıklarımız ve Gerekçeleri

### 6. Vaka Çalışmaları (Case Study)

**Neden yapmadık?**
Vaka çalışması içerik yönetimi gerektiriyor — gerçek kullanıcı hikayeleri, sonuçlar,
rakamlar olmadan boş bir `/case-study` sayfası açmak SEO açısından zararlı (thin content).
Bu içerik hazır olduğunda eklenebilir.

---

### 7. SSS (5 Adet FAQ)

**Neden yapmadık?**
SSS için sorular ve yanıtlar hazır değil. Boş veya jenerik FAQ yazmak kullanıcıya
değer katmaz. `FAQ` schema'sı da içerik olmadan anlamsız. İçerik hazır olduğunda
`app/faq/page.tsx` + `FAQPage` JSON-LD ile eklenebilir.

---

### 9. Sticky Telefon / İletişim CTA

**Neden yapmadık?**
Vitrin bir platform — fiziksel telefon veya WhatsApp CTA'sı olmayan, yazılım odaklı
bir ürün. Mobilde sticky iletişim butonu e-ticaret veya hizmet siteleri için anlamlı.
Burada UX'i bozmaktan başka bir şey yapmaz.

---

### 14. Google Harita ve Adres

**Neden yapmadık?**
Platform dijital, fiziksel bir mağaza veya ofis konumu yok. Contact sayfasında
"İstanbul, Türkiye" yazıyor — bu yeterli. Google Harita embed'i gereksiz ve
sayfa yükleme hızını olumsuz etkiler.

---

### 15. Müşteri Yorumları / Testimonial

**Neden yapmadık?**
`about/page.tsx`'de istatistikler var (10K+ kullanıcı, 2K+ ürün) ama gerçek kullanıcı
alıntıları yok. Uydurma testimonial koymak güven kaybettirir. Gerçek kullanıcı geri
bildirimleri toplandığında eklenebilir.

---

## 📋 Yapılan Değişikliklerin Özeti

| Dosya | Değişiklik |
|-------|-----------|
| `app/not-found.tsx` | Yeni — global 404 sayfası |
| `app/opengraph-image.tsx` | Yeni — global sosyal medya OG görseli |
| `components/breadcrumb.tsx` | Yeni — BreadcrumbList JSON-LD dahil |
| `app/contact/contact-form.tsx` | Yeni — client form bileşeni |
| `app/contact/page.tsx` | Güncellendi — server wrapper + metadata |
| `app/category/[slug]/page.tsx` | Güncellendi — breadcrumb + alt text düzeltmesi |
| `app/product/[slug]/page.tsx` | Güncellendi — breadcrumb eklendi |
| `next.config.mjs` | Güncellendi — image optimization açıldı, remotePatterns eklendi |

---

## 📋 Yayın Öncesi SEO Kontrol Listesi

- [ ] `https://vitrin.it.com/sitemap.xml` erişilebilir ve Google Search Console'a gönderildi
- [ ] `https://vitrin.it.com/robots.txt` doğru çalışıyor
- [ ] `/product/[slug]` sayfalarında JSON-LD Google'ın Rich Results Test'inden geçiyor
- [ ] Ana sayfa, kategori ve ürün sayfaları `securityheaders.com`'dan geçiyor
- [ ] Sosyal medyada paylaşımda OG görseli çıkıyor (`opengraph.xyz` ile test et)
- [ ] Google PageSpeed Insights skorları 90+ (image optimization açık olduğu için)
- [ ] `app/about/page.tsx`'e gerçek takım üyelerinin fotoğrafları eklendi

---

*Son güncelleme: Ağustos 2026*
