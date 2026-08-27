# 🎯 27 Unsur Kontrol Listesi — Genel Özet

> Kaynak: [27 Unsur](https://www.ayhankaraman.com/web-sitesi-yapmadan-once-kontrol-edilmesi-gereken-27-unsur/)

## 📊 Genel Durum

### ✅ Tamamlanan Bölümler

- **Bölüm 1: Teknik Kontroller** (5/5) ✅
- **Bölüm 2: SEO ve İçerik** (8/8) ✅
- **Bölüm 3: İçerik Kalitesi ve Social** (7/7) ✅

**Toplam İlerleme:** 20/27 (74%) 🎉

---

## 📂 Oluşturulan Dosyalar

### Bölüm 1
```
src/Web/Vitrin.Web.UI/
├── middleware.ts                    # Cache + Security headers
├── next.config.mjs                  # Bundle analyzer + optimizations
├── app/api/health/route.ts          # Health check endpoint
├── lib/link-checker.ts              # Link validation library
└── scripts/check-links.ts           # CLI link checker
```

### Bölüm 2
```
src/Web/Vitrin.Web.UI/
├── lib/seo.ts                       # SEO helper functions
├── scripts/seo-audit.ts             # SEO audit tool
├── app/layout.tsx                   # Global SEO + Schema.org
├── app/page.tsx                     # Homepage SEO
└── app/robots.ts                    # Advanced robots rules
```

### Bölüm 3
```
src/Web/Vitrin.Web.UI/
├── lib/
│   ├── content-quality.ts           # Content quality checker
│   └── social-media.ts              # Social media utilities
├── components/
│   └── social-share.tsx             # Social share buttons
├── scripts/
│   └── content-audit.ts             # Content audit tool
├── app/
│   ├── about/page.tsx               # Enhanced about page
│   └── blog/page.tsx                # Enhanced blog page
```

### Dokümantasyon
```
docs/
├── 27-UNSUR-OZET.md                 # Bu dosya
├── 27-UNSUR-BOLUM-1.md              # Bölüm 1 özeti
├── BOLUM-1-CHECKLIST.md             # Bölüm 1 detaylı
└── BOLUM-2-SEO-CHECKLIST.md         # Bölüm 2 detaylı
```

---

## 🚀 Yeni Komutlar

### Bölüm 1 Komutları
```bash
# Link kontrolü
pnpm check-links
pnpm check-links:full
pnpm check-links --url=https://vitrin.com

# Health check
curl http://localhost:3000/api/health

# Bundle analizi
pnpm analyze
```

### Bölüm 2 Komutları
```bash
# SEO audit
pnpm seo-audit
pnpm seo-audit:prod
```

### Bölüm 3 Komutları
```bash
# Content quality audit
pnpm content-audit
pnpm content-audit --page=/about
```

---

## 🎯 Hemen Yapılacaklar

### 1. Package'ları Yükle
```bash
cd src/Web/Vitrin.Web.UI
pnpm install
```

### 2. Development Test
```bash
# Server başlat
pnpm dev

# Yeni terminal'de testler
pnpm check-links
pnpm seo-audit
```

### 3. Google Setup (Production)
```bash
# .env dosyasına ekle:
NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX
NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION=your_code
NEXT_PUBLIC_BING_SITE_VERIFICATION=your_code
```

### 4. Google Search Console
1. https://search.google.com/search-console
2. Property ekle: vitrin.com
3. Verification code'u `.env`'e ekle
4. Sitemap submit: `https://vitrin.com/sitemap.xml`

### 5. Google Analytics 4
1. https://analytics.google.com
2. Property oluştur: Vitrin
3. Measurement ID'yi `.env`'e ekle
4. IP filtreleme ayarla (ofis IP'si)
5. Conversion goals tanımla

---

## 📊 Özellikler

### Bölüm 1: Teknik

#### ✅ 1.1 URL ve Link Kontrolü
- Otomatik broken link checker
- 4xx/5xx/302 detection
- Health check endpoint
- Critical pages monitoring

#### ✅ 1.2 Site Hızı
- Akıllı cache stratejisi (1 yıl static, 7 gün image, 1 saat HTML)
- Gzip/Brotli compression
- Bundle analyzer
- Security headers (XSS, clickjacking)
- SWC minification

#### ✅ 1.3 404 Sayfası
- Özel not-found.tsx (zaten vardı)
- Türkçe kullanıcı dostu
- SEO uyumlu

#### ✅ 1.4 Responsive Design
- Tailwind CSS (zaten vardı)
- Mobile-first approach
- Breakpoints

#### ✅ 1.5 Code Quality
- ESLint + TypeScript (zaten vardı)
- Automated checks

### Bölüm 2: SEO

#### ✅ 2.1 Dizin Kontrolleri
- Gelişmiş robots.txt
- Google/Bing bot kuralları
- Duplicate content prevention
- Query parameter filtering

#### ✅ 2.2 Kopya İçerik
- Duplicate detection script
- WWW vs non-WWW setup
- Canonical tags
- Content hash generator

#### ✅ 2.3 URL Yapısı
- SEO-friendly slugs
- URL validator
- 100+ karakter uyarısı
- Özel karakter kontrolü

#### ✅ 2.4 Analytics Setup
- Vercel Analytics (zaten vardı)
- GA4 hazırlığı
- Search Console verification
- Conversion tracking hazırlığı

#### ✅ 2.5 Anahtar Kelime
- SEO helper library
- Keyword density calculator
- Keyword mapping guide
- Optimize edilmiş meta tags

#### ✅ 2.6 Meta Etiketler
- `generateSEO()` helper
- Title template: `%s — Vitrin`
- Description optimizer (160 char)
- Canonical URL generator
- Duplicate checker

#### ✅ 2.7 Schema.org
- Organization schema
- WebSite schema + SearchAction
- Product schema (zaten vardı)
- Breadcrumb schema (zaten vardı)
- Article schema helper

#### ✅ 2.8 Kullanıcı Arayüzü
- Core Web Vitals monitoring
- Responsive design (zaten vardı)
- Accessibility (zaten vardı)
- UX metrikleri

---

## 🔍 Test Checklist

### Pre-Deploy Tests
- [ ] `pnpm lint` — 0 errors
- [ ] `pnpm typecheck` — 0 errors
- [ ] `pnpm check-links` — 0 broken links
- [ ] `pnpm seo-audit` — 0 critical issues
- [ ] `pnpm test:e2e` — All pass
- [ ] `pnpm analyze` — Bundle size OK

### Post-Deploy Tests
- [ ] Health check: `curl https://vitrin.com/api/health`
- [ ] Sitemap: `https://vitrin.com/sitemap.xml`
- [ ] Robots: `https://vitrin.com/robots.txt`
- [ ] PageSpeed: 90+ score
- [ ] Mobile-friendly test
- [ ] Rich results test
- [ ] SSL certificate OK
- [ ] WWW redirect working

### SEO Tests
- [ ] Google Search Console verification
- [ ] Sitemap submitted
- [ ] URL inspection (5+ pages)
- [ ] Rich results validation
- [ ] Mobile usability check
- [ ] Core Web Vitals pass

### Analytics Tests
- [ ] GA4 tracking working
- [ ] Real-time users visible
- [ ] Events firing correctly
- [ ] Conversions tracking
- [ ] IP filter working

---

## 📈 Metrikler ve Hedefler

### Performance Targets
- **PageSpeed Score:** 90+ (mobile & desktop)
- **LCP:** <2.5s
- **FID:** <100ms
- **CLS:** <0.1
- **TTFB:** <600ms

### SEO Targets
- **Broken Links:** 0
- **4xx Errors:** 0
- **5xx Errors:** 0
- **Duplicate Titles:** 0
- **Duplicate Descriptions:** 0
- **Missing Meta Tags:** 0
- **Schema Errors:** 0

### UX Targets
- **Bounce Rate:** <40%
- **Avg Session Duration:** >2 min
- **Pages/Session:** >2
- **Mobile Traffic:** 50%+

---

## 🔄 Sürekli İzleme

### Günlük
- [ ] Health check monitoring
- [ ] Error rate (Sentry)
- [ ] Uptime (99.9%+)

### Haftalık
- [ ] Broken link scan
- [ ] PageSpeed test
- [ ] GA4 reports
- [ ] Search Console errors

### Aylık
- [ ] Full SEO audit
- [ ] Competitor analysis
- [ ] Keyword ranking check
- [ ] Content gap analysis

---

## 🎯 Sonraki Bölümler

### Bölüm 3: Sosyal Medya (9-13)
- Social media meta tags
- Open Graph optimization
- Twitter Cards
- Social sharing buttons
- Social proof widgets

### Bölüm 4: İçerik ve Güvenlik (14-19)
- Content strategy
- Security headers (partly done)
- SSL/HTTPS
- GDPR compliance
- Privacy policy
- Terms of service

### Bölüm 5: Conversion ve Test (20-27)
- A/B testing setup
- Heatmap tracking
- Conversion funnel
- Email marketing
- Newsletter
- Exit intent popups
- Live chat
- User feedback

---

## 📞 Yardım ve Kaynaklar

### Test Araçları
- [Google PageSpeed Insights](https://pagespeed.web.dev/)
- [Google Mobile-Friendly Test](https://search.google.com/test/mobile-friendly)
- [Google Rich Results Test](https://search.google.com/test/rich-results)
- [GTmetrix](https://gtmetrix.com/)
- [WebPageTest](https://www.webpagetest.org/)
- [Lighthouse CI](https://github.com/GoogleChrome/lighthouse-ci)

### SEO Araçları
- [Google Search Console](https://search.google.com/search-console)
- [Bing Webmaster Tools](https://www.bing.com/webmasters)
- [Screaming Frog](https://www.screamingfrog.co.uk/seo-spider/)
- [Ahrefs](https://ahrefs.com/)
- [SEMrush](https://www.semrush.com/)

### Validation Araçları
- [W3C HTML Validator](https://validator.w3.org/)
- [W3C CSS Validator](https://jigsaw.w3.org/css-validator/)
- [Schema.org Validator](https://validator.schema.org/)

---

**Oluşturulma:** 27 Ağustos 2026  
**Son Güncelleme:** 27 Ağustos 2026  
**Durum:** Bölüm 1 & 2 tamamlandı ✅  
**Sonraki:** Bölüm 3
