# Vitrin.Web — Frontend

Bu klasör placeholder olarak oluşturulmuştu. Aktif frontend uygulaması aşağıdaki konumdadır:

```
src/Web/Vitrin.Web.UI/
```

## Aktif Frontend Hakkında

`src/Web/Vitrin.Web.UI` — **Next.js 16 + React 19 + TailwindCSS v4** ile yazılmış,
production-ready frontend uygulamasıdır. Docker Compose ile `vitrin-web` container'ı
olarak çalışır ve YARP Gateway (port 5000) üzerinden backend servislerine erişir.

### Başlatmak için

```bash
# Geliştirme (local)
cd src/Web/Vitrin.Web.UI
pnpm install
pnpm dev

# Docker ile tüm stack
cd ../../..
docker compose up -d --build
```

### Özellikler

- 20+ sayfa: ana sayfa, ürün detay, profil, arama, admin paneli, koleksiyonlar, leaderboard
- NextAuth v4 ile Google + GitHub OAuth + lokal kimlik doğrulama
- Role-based middleware (Admin / Maker / User)
- Zustand state yönetimi
- Axios ile YARP Gateway entegrasyonu
- Shadcn/ui + Radix UI komponentleri
- Vercel Analytics entegrasyonu
- Dark / Light tema desteği
- KVKK, Gizlilik, Kullanım Koşulları sayfaları
- Cloudinary görsel yükleme desteği (`.env` ile yapılandırılır)

### Yapı

```
src/Web/Vitrin.Web.UI/
├── app/                  # Next.js App Router sayfaları
│   ├── (auth)/           # Login / Register
│   ├── admin/            # Admin paneli
│   ├── product/[slug]/   # Ürün detay
│   ├── profile/          # Profil sayfaları
│   ├── search/           # Arama
│   ├── leaderboard/      # Liderboard
│   ├── collections/      # Koleksiyonlar
│   └── ...
├── components/           # Paylaşılan UI komponentleri
├── core/
│   ├── application/      # Servis katmanı (API çağrıları)
│   ├── domain/           # Tip tanımları
│   └── infrastructure/   # Axios api-client
├── lib/                  # Yardımcı fonksiyonlar
└── public/               # Statik dosyalar
```
