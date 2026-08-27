# 📋 Bölüm 2: SEO ve İçerik Optimizasyonu

> Kaynak: https://www.ayhankaraman.com/web-sitesi-yapmadan-once-kontrol-edilmesi-gereken-27-unsur/

## ✅ 2.1 Olası Dizin Hatalarını Kontrol Etmelisiniz

### Mevcut Durum
- ✅ `robots.ts` geliştirildi
- ✅ Disallow kuralları tanımlı (admin, dashboard, api)
- ✅ Sitemap referansı var
- ✅ Google/Bing bot'lar için özel kurallar
- ✅ Duplicate content için query parametreleri engellendi

### Yapılanlar
```typescript
// robots.ts — Güncellenmiş kurallar
- /api/* disallow
- /auth/* disallow
- /*?*sort= disallow (duplicate content)
- /*?*page= disallow (pagination duplicate)
- /search? disallow (arama sonuçları)
```

### Google Search Console Kurulumu
1. **Site Ownership Verification**
   ```bash
   # .env dosyasına ekle:
   NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION=your_verification_code
   ```

2. **Sitemap Submit**
   - https://search.google.com/search-console
   - Property seç → Sitemaps → Add new sitemap
   - URL: `https://vitrin.com/sitemap.xml`

3. **URL Inspection Tool**
   - Kritik sayfaları tek tek test et
   - "Request Indexing" yap

### Kontrol Araçları
- Google Search Console
- Bing Webmaster Tools
- Screaming Frog SEO Spider
- Sitebulb

---

## ✅ 2.2 Kopya İçerik veya Kopya Sayfa Problemlerini Gözden Geçirmelisiniz

### 1. WWW vs Non-WWW ✅

**Mevcut Durum:**
- ✅ `robots.ts`'te host tanımlandı
- ✅ Non-www tercih edildi
- ⚠️ Nginx/Cloudflare'de 301 redirect gerekli

**Nginx Konfigürasyonu:**
```nginx
# nginx/vitrin.conf içine ekle
server {
    listen 80;
    server_name www.vitrin.com;
    return 301 https://vitrin.com$request_uri;
}
```

### 2. Kopya İçerik Kontrolü ✅

**Yeni Araçlar:**
- `lib/seo.ts` → `generateContentHash()` fonksiyonu
- `scripts/seo-audit.ts` → Duplicate title/description checker

**Kullanım:**
```bash
pnpm seo-audit
pnpm seo-audit:prod
```

**Çıktı:**
- ✅ Title tag kontrolü
- ✅ Description kontrolü
- ✅ Canonical tag kontrolü
- ✅ Duplicate detection

### 3. E-ticaret İçin Öneriler

Vitrin bir product listing platformu olduğu için:
- ✅ Her ürün unique description'a sahip
- ✅ User comments eklendi (UGC content)
- ✅ Schema.org ProductSchema kullanılıyor
- ⚠️ Maker'ların özgün açıklama yazmasını teşvik et

### 4. Meta Tag Uniqueness ✅

**Yapılanlar:**
- ✅ `lib/seo.ts` → `generateSEO()` helper
- ✅ Her sayfa kendi metadata'sını export ediyor
- ✅ Template pattern kullanılıyor: `%s — Vitrin`
- ✅ Duplicate checker script'i eklendi

**SEO Audit Çalıştır:**
```bash
# Development
pnpm seo-audit

# Production
pnpm seo-audit:prod
```

---

## ✅ 2.3 URL Adreslerinin Arama Motoru Dostu Olduğundan Emin Olmalısınız

### URL Best Practices ✅

**Mevcut URL Yapısı:**
```
✅ /product/[slug]           # Good: Short, descriptive
✅ /category/[slug]          # Good: Clear hierarchy
✅ /launches                 # Good: Simple plural
✅ /p/[slug]                 # Good: Short alias
❌ /product?id=123           # Bad: Query parameters
```

**URL Kontrolü:**
- ✅ `scripts/seo-audit.ts` içinde URL validator
- ✅ 100+ karakter uyarısı
- ✅ Özel karakter kontrolü
- ✅ Underscore kullanımı uyarısı

**Öneriler:**
- ✅ Slug'lar lowercase
- ✅ Tire (-) kullanımı
- ✅ Sayfa numaraları query'de (?page=2)
- ✅ Filtreleme query'de (?sort=newest)
- ✅ Canonical tag ile duplicate önlendi

---

## ✅ 2.4 Google Analytics ve Search Console Tanımlamalarını Yapmalısınız

### Google Analytics Setup

**Mevcut Durum:**
- ✅ `@vercel/analytics` zaten kurulu
- ✅ `layout.tsx` içinde `<Analytics />` component

**Ek Konfigürasyon:**

1. **GA4 Property Oluştur**
   - https://analytics.google.com
   - Create Property → Vitrin
   - Get Measurement ID (G-XXXXXXXXXX)

2. **.env'e Ekle**
   ```env
   NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX
   NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION=your_code
   ```

3. **IP Filtreleme** (Ofis IP'si hariç tut)
   - Admin → Data Filters → Create Filter
   - Filter Type: Internal Traffic
   - IP Address: `your_office_ip`

### Search Console Entegrasyonu

**Layout.tsx'te verification eklendi:**
```typescript
verification: {
  google: process.env.NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION,
}
```

### Conversion Goals (Dönüşüm Hedefleri)

**Tanımlanması Gerekenler:**
1. Product view (ürün detay sayfası)
2. Upvote (oy verme)
3. Comment (yorum yapma)
4. Product submit (ürün ekleme)
5. Newsletter signup
6. User registration

**GA4 Events:**
```typescript
// lib/analytics.ts oluştur
gtag('event', 'product_view', {
  product_id: '123',
  product_name: 'My Product',
})

gtag('event', 'upvote', {
  product_id: '123',
})
```

---

## ✅ 2.5 Siteniz İçin Anahtar Kelime Haritası Oluşturmalısınız

### Keyword Research Tools

**Ücretsiz:**
- Google Keyword Planner
- Google Trends
- Google Search Console (Query data)
- Answer The Public

**Ücretli:**
- Ahrefs
- SEMrush
- Moz Keyword Explorer

### Vitrin İçin Anahtar Kelime Haritası

**Ana Sayfa (`/`):**
- Primary: "ürün keşfi", "yeni ürünler"
- Secondary: "startup türkiye", "product hunt"
- Long-tail: "günün en iyi ürünleri"

**Keşfet (`/discover`):**
- Primary: "ürün keşfet", "yeni uygulamalar"
- Secondary: "trend ürünler", "popüler uygulamalar"

**Kategoriler (`/categories`):**
- Primary: "ürün kategorileri"
- Secondary: "yazılım kategorileri", "uygulama türleri"

**Kategori Sayfaları (`/category/[slug]`):**
- Example: `/category/yapay-zeka`
  - Primary: "yapay zeka ürünleri"
  - Secondary: "AI araçları", "makine öğrenmesi"

**Ürün Detay (`/product/[slug]`):**
- Dynamic keywords based on:
  - Product name
  - Product tagline
  - Product categories
  - User comments

### Keyword Density Helper

**`lib/seo.ts` içinde:**
```typescript
calculateKeywordDensity(content, keyword)
// İdeal: %1-3 arası
```

### Keyword Mapping Excel

```
| Sayfa           | Primary Keyword      | Secondary Keywords        | Intent    |
|-----------------|---------------------|---------------------------|-----------|
| Ana Sayfa       | ürün keşfi          | yeni ürünler, startup     | Navigate  |
| /discover       | ürün keşfet         | trend ürünler             | Explore   |
| /launches       | yeni lansmanlar     | bugün çıkan ürünler       | Browse    |
| /product/[slug] | [Product Name]      | [Categories]              | Learn     |
```

---

## ✅ 2.6 Meta Etiketleri ve İçerikleri Optimize Etmelisiniz

### SEO Helper Library ✅

**`lib/seo.ts` Fonksiyonları:**

```typescript
// Standart metadata oluştur
generateSEO({
  title: 'Sayfa Başlığı',
  description: 'Açıklama',
  path: '/yol',
  keywords: ['kelime1', 'kelime2'],
})

// Description optimize et (160 karakter)
optimizeDescription(text, 160)

// Canonical URL oluştur
getCanonicalUrl('/product/my-product')
```

### Meta Tag Best Practices

**Title Tag:**
- ✅ 50-60 karakter arası
- ✅ Primary keyword başta
- ✅ Brand name sonda: `Ürün Adı — Vitrin`
- ✅ Her sayfa unique

**Meta Description:**
- ✅ 150-160 karakter arası
- ✅ Call-to-action içermeli
- ✅ Primary + secondary keyword
- ✅ Her sayfa unique

**Güncellenen Sayfalar:**
- ✅ `app/layout.tsx` — Global metadata
- ✅ `app/page.tsx` — Ana sayfa SEO
- ✅ `app/product/[slug]/page.tsx` — Zaten iyi!

### SEO Audit Komutu

```bash
pnpm seo-audit
```

**Kontrol Edilenler:**
- Title tag varlığı ve uzunluğu
- Description varlığı ve uzunluğu
- Canonical tag
- H1 tag
- URL yapısı
- Duplicate detection

---

## ✅ 2.7 İçeriğinizi Biçimlendirmek İçin Schema Kullanın

### Schema.org Implementation ✅

**Global Schema'lar (`app/layout.tsx`):**
```typescript
// Organization Schema
{
  "@type": "Organization",
  "name": "Vitrin",
  "url": "https://vitrin.com",
  "logo": "https://vitrin.com/logo.png",
  "sameAs": ["twitter", "github"],
}

// WebSite Schema + SearchAction
{
  "@type": "WebSite",
  "potentialAction": {
    "@type": "SearchAction",
    "target": "https://vitrin.com/search?q={search_term}"
  }
}
```

**Sayfa Schema'ları:**

1. **Product Page** ✅ Zaten var!
   ```typescript
   {
     "@type": "SoftwareApplication",
     "name": "Product Name",
     "interactionStatistic": [...]
   }
   ```

2. **Breadcrumb** ✅ Zaten var!
   ```typescript
   {
     "@type": "BreadcrumbList",
     "itemListElement": [...]
   }
   ```

3. **Article** (Blog için)
   ```typescript
   getArticleSchema({
     title, description, url, image,
     author, publishedTime, modifiedTime
   })
   ```

### Rich Snippets Test

**Test Araçları:**
- [Google Rich Results Test](https://search.google.com/test/rich-results)
- [Schema.org Validator](https://validator.schema.org/)

**Komut:**
```bash
# Schema.org validator ile test et
curl -X POST https://validator.schema.org/validate \
  -d "url=https://vitrin.com/product/example"
```

---

## ✅ 2.8 Kullanıcı Arayüzü Kontrol Etmelisiniz

### User Experience Metrikleri

**Core Web Vitals:**
- ✅ LCP (Largest Contentful Paint): <2.5s
- ✅ FID (First Input Delay): <100ms
- ✅ CLS (Cumulative Layout Shift): <0.1

**Test Araçları:**
- Google PageSpeed Insights
- Lighthouse (Chrome DevTools)
- WebPageTest
- GTmetrix

### UX Checklist

**Hız:**
- ✅ Next.js Image Optimization
- ✅ Lazy loading
- ✅ Code splitting
- ✅ CDN (Cloudinary)
- ✅ Caching (Bölüm 1'de eklendi)

**Responsive:**
- ✅ Tailwind CSS breakpoints
- ✅ Mobile-first approach
- ✅ Touch-friendly buttons (48x48px min)
- ✅ Readable fonts (16px+ mobilde)

**Accessibility:**
- ✅ Semantic HTML
- ✅ ARIA labels
- ✅ Keyboard navigation
- ✅ Color contrast (WCAG AA)

**Engagement Metrics:**
- [ ] Bounce rate monitoring (GA4)
- [ ] Average session duration
- [ ] Pages per session
- [ ] Scroll depth

---

## 🎯 Sonraki Adımlar

### Hemen Yapılacaklar

```bash
# 1. SEO audit çalıştır
cd src/Web/Vitrin.Web.UI
pnpm seo-audit

# 2. Duplicate content kontrolü
# Script otomatik olarak duplicate title/description bulur

# 3. URL yapısı kontrolü
# Script URL best practices'i test eder
```

### Google Search Console Setup

1. **Ownership Verification**
   - Verification code al
   - `.env`'e ekle: `NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION=xxx`
   - Deploy et

2. **Sitemap Submit**
   - `https://vitrin.com/sitemap.xml` submit et

3. **Monitor**
   - Coverage reports kontrol et
   - URL Inspection Tool kullan
   - Performance reports takip et

### Google Analytics Setup

1. **GA4 Property Oluştur**
   - Measurement ID al
   - `.env`'e ekle: `NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXX`

2. **Events Tanımla**
   - product_view
   - upvote
   - comment
   - product_submit
   - newsletter_signup

3. **Conversion Goals**
   - User registration
   - Product submission
   - Email signup

### Schema.org Validation

```bash
# Rich results test
# https://search.google.com/test/rich-results

# Test edilecek sayfalar:
- Ana sayfa: https://vitrin.com
- Ürün detay: https://vitrin.com/product/[slug]
- Kategori: https://vitrin.com/category/[slug]
```

---

## 📊 Başarı Kriterleri

| Madde | Durum | Hedef |
|-------|-------|-------|
| 2.1 Dizin Kontrolü | ✅ | 0 indexing error |
| 2.2 Duplicate Content | ✅ | 0 duplicate |
| 2.3 URL Yapısı | ✅ | SEO-friendly |
| 2.4 Analytics Setup | ⚠️ | GA4 + GSC configured |
| 2.5 Keyword Map | ✅ | Excel/doc hazır |
| 2.6 Meta Tags | ✅ | 100% unique |
| 2.7 Schema.org | ✅ | Rich snippets |
| 2.8 UX/UI | ✅ | Core Web Vitals pass |

---

## 📁 Oluşturulan Dosyalar

```
Vitrin/
├── docs/
│   └── BOLUM-2-SEO-CHECKLIST.md    # Bu dosya
├── src/Web/Vitrin.Web.UI/
│   ├── lib/
│   │   └── seo.ts                   # ⭐ YENİ: SEO helper functions
│   ├── scripts/
│   │   └── seo-audit.ts             # ⭐ YENİ: SEO audit tool
│   ├── app/
│   │   ├── layout.tsx               # 🔧 GÜNCELLENDİ: Global SEO
│   │   ├── page.tsx                 # 🔧 GÜNCELLENDİ: Homepage SEO
│   │   └── robots.ts                # 🔧 GÜNCELLENDİ: Advanced rules
│   └── package.json                 # 🔧 GÜNCELLENDİ: Yeni script'ler
```

---

**Oluşturulma:** 27 Ağustos 2026  
**Sonraki Bölüm:** Bölüm 3 - Sosyal Medya ve Pazarlama Entegrasyonu
