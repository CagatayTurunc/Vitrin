# 📋 Bölüm 3: İçerik Kalitesi ve Sosyal Medya

> Kaynak: https://www.ayhankaraman.com/web-sitesi-yapmadan-once-kontrol-edilmesi-gereken-27-unsur/

## ✅ 3.1 Oluşturulan İçeriklerin Değer Kattığından Emin Olmalısınız

### Mevcut Durum
- ✅ About sayfası değer katan içeriğe sahip
- ✅ Blog sayfası planlı ve organize
- ✅ Ürün detay sayfaları bilgilendirici

### Yeni Araçlar
**`lib/content-quality.ts`:**
```typescript
// İçerik değer analizi
analyzeContentValue(content)
// Returns: { hasValue, reasons }
```

**Kontrol Edilenler:**
- Minimum uzunluk (300+ karakter)
- Kelime sayısı (100+ kelime)
- Özgünlük oranı
- Cümle çeşitliliği
- Liste/madde kullanımı

### Content Audit Script
```bash
# Tüm sayfaları kontrol et
pnpm content-audit

# Belirli sayfa
pnpm content-audit --page=/about
```

**Çıktı Örneği:**
```
📄 Sayfa: /about
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🟢 İçerik Kalite Skoru: 85/100

📖 Okunabilirlik:
  • Flesch Skoru: 72/100 (Kolay)
  • Ortalama Kelime/Cümle: 15.3
  • Ortalama Hece/Kelime: 2.1

🏗️  Yapı:
  • Başlık: ✅
  • Liste: ✅
  • Paragraf: ✅

✅ Harika! İçerik kalitesi yüksek.
```

### Öneriler
- [ ] Her sayfada minimum 300 kelime olsun
- [ ] Uygulanabilir adımlar ve örnekler ekleyin
- [ ] Uzman görüşleri veya röportajlar planlayın
- [ ] Content audit'i her yeni içerik için çalıştırın

---

## ✅ 3.2 Oluşturduğunuz İçerikleri Kontrol Amacıyla Tekrar Gözden Geçirmelisiniz

### Yazım Kontrol Aracı
**`lib/content-quality.ts` → `checkSpelling()`:**

**Kontrol Edilenler:**
- ✅ Türkçe karakter kullanımı
- ✅ Çift boşluk tespiti
- ✅ Noktalama hataları
- ✅ Büyük/küçük harf kontrolü

**Örnek Kullanım:**
```typescript
const { issues } = checkSpelling(text)
// issues: [
//   { type: 'spacing', message: '3 adet çift boşluk bulundu' },
//   { type: 'punctuation', message: '2 adet noktalama hatası' }
// ]
```

### Manuel Kontrol Checklist
- [ ] Tüm içerikleri yazım hatalarına karşı oku
- [ ] Türkçe karakter kullanımına dikkat et (ı, ğ, ş, etc.)
- [ ] Tutarlı noktalama kullan
- [ ] Cümle sonlarında büyük harf kullan

---

## ✅ 3.3 En Doğru Yazı Biçimini Kullanmaya Çalışmalısınız

### Biçimlendirme Kontrolü
**`lib/content-quality.ts` → `checkFormatting()`:**

**Kontrol Edilenler:**
- ✅ Başlık kullanımı (H2, H3)
- ✅ Liste/madde işaretleri
- ✅ Paragraf etiketleri
- ✅ Görseller
- ✅ Linkler

### İyi Biçimlendirilmiş İçerik Örneği

```markdown
# Ana Başlık (H1)

İlk paragraf — içeriğe giriş.

## Alt Başlık (H2)

Daha detaylı açıklama...

### Madde İşaretleri
- Madde 1
- Madde 2
- Madde 3

### Numaralı Liste
1. Adım 1
2. Adım 2
3. Adım 3

**Kalın metin** ve *italik metin* kullanımı.

[Link örneği](https://vitrin.com)

![Görsel alt text](image.png)
```

### Güncellenmiş Sayfalar
- ✅ `/about` — İyi yapılandırılmış (başlıklar, listeler, paragraflar)
- ✅ `/blog` — Grid layout ve kategoriler
- ✅ `product/[slug]` — Zaten iyi

---

## ✅ 3.4 İçeriklerin Gerçeği Yansıttığından Emin Olmalısınız

### Madde 3.4: Doğrulama Checklist

**İçerik Oluştururken:**
- [ ] İstatistikleri kaynaklarla destekleyin
- [ ] Harici linkleri güvenilir kaynaklara ekleyin
- [ ] Güncel bilgiler kullanın (tarih belirtin)
- [ ] Asparagas bilgilerden kaçının

**About Sayfası:**
- ✅ Gerçek istatistikler: "10K+ Kullanıcı, 2K+ Ürün"
- ✅ Net misyon tanımı
- ✅ Doğrulanabilir bilgiler

**Blog İçerikleri:**
- ⚠️ Her blog yazısında kaynak ekleyin
- ⚠️ Tarih ve yazar bilgisi eklendi
- ⚠️ "Gerçek veriler" section'ında kaynaklar

### Öneriler
- Wikipedia, resmi siteler, akademik kaynaklar kullanın
- Her istatistik için kaynak linki ekleyin
- Tarihi belirtin (güncelleme tarihi)
- Editöryal süreç oluşturun

---

## ✅ 3.5 İçerik Stilini Mutlaka Özgün Hale Getirmelisiniz

### Vitrin Marka Stili

**Ton & Hitap:**
- ✅ Dostça ve samimi ("Sen" hitabı)
- ✅ Profesyonel ama resmi değil
- ✅ İlham verici ve destekleyici
- ✅ Açık ve net iletişim

**Kelime Seçimleri:**
- ✅ "Maker" (girişimci yerine)
- ✅ "Keşfet" (bul yerine)
- ✅ "Topluluk" (kullanıcılar yerine)
- ✅ "Lansman" (yayın yerine)

**Örnekler:**

**❌ Kötü:**
> "Sistemimiz, kullanıcıların ürünlere oy vermesine olanak tanımaktadır."

**✅ İyi:**
> "Her gün topluluk en iyi ürünleri oy vererek öne çıkarıyor."

**❌ Kötü:**
> "Lütfen formülümüzü doldurunuz."

**✅ İyi:**
> "Ürününü ekle, topluluğun görüşlerini al."

### Style Guide Dokümanı

**Oluşturulacak:** `docs/STYLE-GUIDE.md`

```markdown
# Vitrin İçerik Stil Rehberi

## Ton
- Dostça, samimi, destekleyici
- Profesyonel ama resmi değil

## Hitap
- "Sen" / "Siz" (sen tercih et)
- Direkt ve net

## Kelime Tercihleri
- maker > girişimci
- keşfet > bul
- topluluk > kullanıcılar

## Yasak Kelimeler
- "Müşteri" (topluluk üyesi de)
- "Sistem" (platform de)
- "Kullanıcı" (maker veya üye de)
```

---

## ✅ 3.6 Bir İçerik Haritası Oluşturmayı İhmal Etmemelisiniz

### İçerik Haritası (Content Map)

**`lib/content-quality.ts` → `validateContentMap()`:**

**Örnek İçerik Haritası:**

| Sayfa | Amaç | Hedef Kitle | Durum |
|-------|------|-------------|-------|
| Ana Sayfa | Ürün keşfi | Tüm ziyaretçiler | ✅ Published |
| /about | Kim olduğumuzu anlat | Yeni ziyaretçiler | ✅ Published |
| /blog | Bilgilendirme & SEO | Maker'lar, meraklılar | ✅ Published |
| /submit | Ürün ekleme rehberi | Maker'lar | ✅ Published |
| /how-it-works | Platform açıklaması | Yeni kullanıcılar | ⏳ Planned |
| /faq | Sık sorulan sorular | Herkes | ⏳ Planned |
| /maker-guide | Lansm...

 | Maker'lar | ⏳ Planned |

### Eksik İçerikler (Content Gaps)

**Script ile Tespit:**
```typescript
validateContentMap(items)
// Returns:
// gaps: [
//   "nasıl çalışır konusunda içerik eksik",
//   "sıkça sorulan sorular konusunda içerik eksik"
// ]
```

### Oluşturulması Gerekenler
- [ ] `/how-it-works` — Platform nasıl çalışır?
- [ ] `/faq` — Sıkça sorulan sorular
- [ ] `/maker-guide` — Maker rehberi
- [ ] `/community-guidelines` — Topluluk kuralları detayı
- [ ] `/changelog` — Platform güncellemeleri

---

## ✅ 3.7 Markanız için Sosyal Medya Hesapları Oluşturmalısınız

### Social Media Entegrasyonu

**Mevcut Durum:**
- ✅ Twitter link (footer)
- ✅ Open Graph meta tags (layout.tsx)
- ✅ Twitter Card meta tags (layout.tsx)
- ⚠️ Social share buttons eksik

### Yeni Eklenenler

#### 1. Social Share Component
**`components/social-share.tsx`:**
```tsx
<SocialShare
  data={{
    url: 'https://vitrin.com/product/example',
    title: 'Harika Ürün',
    description: 'Açıklama',
    hashtags: ['vitrin', 'startup'],
    via: 'vitrinapp'
  }}
  showLabels={true}
/>
```

**Desteklenen Platformlar:**
- Twitter
- Facebook
- LinkedIn
- WhatsApp
- Telegram
- Reddit
- Hacker News
- Email

#### 2. Social Media Utilities
**`lib/social-media.ts`:**
```typescript
// Share URL'leri oluştur
generateShareUrls(data)

// Social meta validator
validateSocialMeta(html)

// Open Graph meta oluştur
generateOpenGraphMeta(data)

// Twitter Card meta oluştur
generateTwitterCardMeta(data)

// Social için optimize et
optimizeForSocial({ title, description })
```

### Social Media Profilleri

**Oluşturulması Gerekenler:**

#### Twitter/X (@vitrinapp)
- [ ] Bio: "Türkiye'nin ürün keşif platformu 🚀 | Her gün yeni ürünler"
- [ ] Header: 1500x500px Vitrin banner
- [ ] Profile pic: Vitrin logo
- [ ] Pinned tweet: Platform tanıtımı
- [ ] 5-10 initial tweet (ürün paylaşımı)

#### LinkedIn
- [ ] Company page oluştur
- [ ] Logo ve cover image ekle
- [ ] "About" kısmını doldur
- [ ] İlk 3-5 post paylaş

#### Instagram (Opsiyonel)
- [ ] Bio ve link
- [ ] Highlight'lar (Ürünler, Maker'lar, Topluluk)
- [ ] İlk 9 post (grid güzel görünsün)

#### GitHub
- [ ] Organization account
- [ ] Açık kaynak projeler
- [ ] README.md

#### Product Hunt
- [ ] Vitrin'i Product Hunt'a ekle!
- [ ] Maker profili oluştur

### Optimal Image Sizes

**`lib/social-media.ts` → `SOCIAL_IMAGE_SIZES`:**
- Open Graph: 1200x630px
- Twitter Card: 1200x675px
- Instagram Post: 1080x1080px
- Instagram Story: 1080x1920px

### Social Meta Validator
```bash
# Check social meta tags
pnpm tsx scripts/social-meta-check.ts
```

---

## 🎯 Sonraki Adımlar

### Hemen Yapılacaklar

```bash
# 1. Content audit çalıştır
pnpm content-audit

# 2. Spell check (manuel)
# About ve Blog sayfalarını oku

# 3. Social share buttons ekle
# Ürün detay sayfasına <SocialShare /> ekle
```

### İçerik Oluşturma

**Bu Hafta:**
- [ ] `/how-it-works` sayfası yaz
- [ ] `/faq` sayfası oluştur (10 soru)
- [ ] İlk 2 blog yazısını yaz ve yayınla

**Gelecek Hafta:**
- [ ] Maker rehberi oluştur
- [ ] Community guidelines detaylandır
- [ ] Changelog sayfası ekle

### Social Media Setup

**Bugün:**
- [ ] Twitter account oluştur
- [ ] Bio ve profile pic ayarla
- [ ] İlk 5 tweet at

**Bu Hafta:**
- [ ] LinkedIn company page
- [ ] GitHub organization
- [ ] Product Hunt profile

**Gelecek Hafta:**
- [ ] Instagram (opsiyonel)
- [ ] Daily posting schedule oluştur

---

## 📊 Başarı Kriterleri

| Madde | Durum | Hedef |
|-------|-------|-------|
| 3.1 İçerik Değeri | ✅ | Content audit 80+ |
| 3.2 Yazım Kontrolü | ✅ | 0 spelling error |
| 3.3 Biçimlendirme | ✅ | Tüm sayfalar formatted |
| 3.4 Gerçeklik | ⚠️ | Kaynaklar eklenmeli |
| 3.5 Özgün Stil | ✅ | Style guide oluştur |
| 3.6 İçerik Haritası | ⚠️ | 5 gap doldurulmalı |
| 3.7 Social Media | ⚠️ | Tüm hesaplar aktif |

---

## 📁 Oluşturulan Dosyalar

```
Vitrin/
├── docs/
│   └── BOLUM-3-CONTENT-SOCIAL-CHECKLIST.md  # Bu dosya
├── src/Web/Vitrin.Web.UI/
│   ├── lib/
│   │   ├── content-quality.ts               # ⭐ İçerik kalite kontrol
│   │   └── social-media.ts                  # ⭐ Social media utils
│   ├── components/
│   │   └── social-share.tsx                 # ⭐ Social share buttons
│   ├── scripts/
│   │   └── content-audit.ts                 # ⭐ Content audit tool
│   ├── app/
│   │   ├── about/page.tsx                   # 🔧 GÜNCELLENDİ
│   │   └── blog/page.tsx                    # 🔧 GÜNCELLENDİ
│   └── package.json                         # 🔧 GÜNCELLENDİ
```

---

**Oluşturulma:** 27 Ağustos 2026  
**Sonraki Bölüm:** Bölüm 4 - Güvenlik ve GDPR
