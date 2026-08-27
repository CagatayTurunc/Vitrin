# 📋 Bölüm 1: Teknik Kontroller ve İyileştirmeler

> Kaynak: https://www.ayhankaraman.com/web-sitesi-yapmadan-once-kontrol-edilmesi-gereken-27-unsur/

## ✅ 1.1 Sitenin Tüm URL Adreslerinin Çalıştığından Emin Olmalısınız

### Mevcut Durum
- ✅ Next.js App Router yapısı kullanılıyor
- ✅ Sitemap.ts dosyası mevcut (`/sitemap.xml`)
- ✅ Robots.ts dosyası mevcut (`/robots.txt`)
- ✅ Health check endpoint'i mevcut (`/api/health`)

### Yeni Eklenenler
1. **Otomatik Link Checker** (`lib/link-checker.ts`)
   - Tüm internal linkleri kontrol eder
   - 4xx (client errors) tespit eder
   - 5xx (server errors) tespit eder
   - 302/301 (redirects) tespit eder
   - Broken linkleri listeler

2. **Link Kontrol Script'i** (`scripts/check-links.ts`)
   ```bash
   # Hızlı kontrol (kritik sayfalar)
   pnpm check-links
   
   # Tam kontrol (tüm sitemap)
   pnpm check-links:full
   
   # Production URL ile
   pnpm check-links --url=https://vitrin.com
   ```

3. **Gelişmiş Health Check** (`/api/health`)
   - Frontend uptime
   - Backend API bağlantı kontrolü
   - Memory usage
   - Version bilgisi

### Öneriler
- CI/CD pipeline'a link kontrolü ekleyin
- Her deploy öncesi otomatik kontrol
- Production'da haftalık broken link taraması

---

## ✅ 1.2 Sitenin Hızını Kontrol Etmelisiniz

### Mevcut Optimizasyonlar
- ✅ Next.js standalone output mode (Docker için optimize)
- ✅ Image optimization aktif (WebP, responsive)
- ✅ Production source maps kapalı (güvenlik)
- ✅ CDN remote patterns tanımlı
- ✅ SWC minification aktif

### Yeni Eklenenler
1. **Middleware ile Caching** (`middleware.ts`)
   - Static assets: 1 yıl cache (immutable)
   - Images: 7 gün cache + stale-while-revalidate
   - HTML: 1 saat cache + revalidate
   - API: No cache

2. **Compression Middleware**
   - Gzip/Brotli desteği
   - Accept-Encoding header kontrolü
   - Vary header ekleme

3. **Bundle Analyzer**
   ```bash
   # Bundle boyutlarını analiz et
   pnpm analyze
   ```

4. **Security Headers**
   - X-Frame-Options: DENY
   - X-Content-Type-Options: nosniff
   - Referrer-Policy
   - HSTS (production)
   - Permissions-Policy

### Performans Kontrol Araçları
- [Google PageSpeed Insights](https://pagespeed.web.dev/)
- [WebPageTest](https://www.webpagetest.org/)
- [GTmetrix](https://gtmetrix.com/)
- Lighthouse (Chrome DevTools)

### Öneriler
- [ ] PageSpeed Insights ile test edin (hedef: 90+ mobil)
- [ ] Lighthouse audit'i çalıştırın
- [ ] Bundle analyzer ile gereksiz dependency'leri tespit edin
- [ ] CDN kullanımını değerlendirin (Cloudflare, Vercel)

---

## ✅ 1.3 404 Sayfalarının Doğru Biçimde Yapılandırıldığından Emin Olmalısınız

### Mevcut Durum
- ✅ Özel `not-found.tsx` sayfası mevcut
- ✅ Türkçe kullanıcı dostu mesajlar
- ✅ Navigasyon linkleri (Ana Sayfa, Keşfet, Arama)
- ✅ Back button ile geri dönüş
- ✅ SEO uyumlu meta tags (`noindex, follow`)
- ✅ Visual feedback (404 icon + animation)

### Öneriler
- [x] Tasarım mevcut ve güzel!
- [x] SEO best practices uygulanmış
- [ ] Google Search Console'da 404 hatalarını izleyin
- [ ] Analytics'te 404 oranını takip edin

---

## ⚠️ 1.4 Sitenin Çoklu Cihaz Desteğine Sahip Olduğundan Emin Olmalısınız

### Mevcut Durum
- ✅ Tailwind CSS ile responsive design
- ✅ Mobile-first yaklaşım
- ✅ Tüm componentlerde responsive class'lar (`sm:`, `md:`, `lg:`)

### Test Edilmesi Gerekenler
1. **Mobile Optimizasyon**
   - [ ] Google Mobile-Friendly Test
   - [ ] Touch target'lar yeterince büyük mü? (min 48x48px)
   - [ ] Viewport meta tag doğru mu?
   - [ ] Font boyutları okunabilir mi?

2. **Tablet ve Desktop**
   - [ ] Tüm breakpoint'lerde test edin
   - [ ] Menü ve navigasyon çalışıyor mu?
   - [ ] Images responsive mi?

3. **Browser Uyumluluğu**
   - [ ] Chrome, Firefox, Safari, Edge
   - [ ] iOS Safari (özellikle önemli)
   - [ ] Android Chrome

### Öneriler
```bash
# Playwright ile responsive test
pnpm test:e2e

# Manuel test
# - Chrome DevTools Device Mode
# - BrowserStack / LambdaTest
```

### AMP Değerlendirmesi
- AMP şu an için gerekli değil (Next.js zaten hızlı)
- İhtiyaç duyulursa `next-amp` paketi kullanılabilir

---

## ✅ 1.5 Uygun Kodlar Kullanmaya Çalışmalısınız

### Mevcut Araçlar
- ✅ ESLint configured (`eslint-config-next`)
- ✅ TypeScript strict mode
- ✅ Prettier (varsayılan Next.js formatı)

### Validation Araçları
1. **HTML Validation**
   - [W3C Markup Validator](https://validator.w3.org/)
   - [Nu Html Checker](https://validator.github.io/validator/)

2. **CSS Validation**
   - [W3C CSS Validator](https://jigsaw.w3.org/css-validator/)
   - Tailwind CSS kullandığınız için otomatik valid

3. **Accessibility (A11y)**
   - ✅ `@axe-core/playwright` paketi mevcut
   - Lighthouse accessibility audit

### Komutlar
```bash
# Lint kontrolü
pnpm lint

# Type check
pnpm typecheck

# Tüm kontroller
pnpm check

# A11y test
pnpm test:e2e
```

### Öneriler
- [ ] Pre-commit hook ekleyin (husky + lint-staged)
- [ ] CI/CD'de lint ve type check zorunlu
- [ ] Accessibility testlerini otomatikleştirin
- [ ] HTML validator'ı production'a deploy etmeden önce çalıştırın

---

## 🎯 Sonraki Adımlar

1. **Hemen Yapılacaklar**
   ```bash
   # Package'ları yükle
   cd src/Web/Vitrin.Web.UI
   pnpm install
   
   # Link kontrolü yap
   pnpm check-links
   
   # Bundle analizi
   pnpm analyze
   
   # E2E testler
   pnpm test:e2e
   ```

2. **CI/CD Pipeline'a Ekle**
   ```yaml
   # .github/workflows/ci.yml içine
   - name: Check Links
     run: pnpm check-links
   
   - name: Lighthouse CI
     run: npx @lhci/cli@latest autorun
   ```

3. **Monitoring Ekle**
   - Uptime monitoring (UptimeRobot, Pingdom)
   - Broken link monitoring (haftalık cron job)
   - Performance monitoring (Vercel Analytics, Sentry)

4. **Dokümantasyon**
   - README'ye performance metrikleri ekleyin
   - Team'e yeni script'leri tanıtın
   - Runbook oluşturun (404 spike vs.)

---

## 📊 Başarı Kriterleri

- [ ] Tüm kritik sayfalarda 0 broken link
- [ ] PageSpeed Insights skoru 90+ (mobil ve desktop)
- [ ] 404 sayfası kullanıcı dostu
- [ ] Tüm cihazlarda responsive
- [ ] 0 ESLint error
- [ ] 0 TypeScript error
- [ ] Accessibility score 95+

---

**Oluşturulma:** 27 Ağustos 2026  
**Sonraki Bölüm:** Bölüm 2 - SEO ve İçerik Optimizasyonu
