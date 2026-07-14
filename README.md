# Vitrin

Vitrin; geliştiricilerin ve üreticilerin ürünlerini yayınladığı, topluluğun ürünleri keşfettiği, oyladığı, yorumladığı ve koleksiyonlara eklediği mikroservis tabanlı bir ürün keşif platformudur.

Bu depo yalnızca çalışan bir uygulama değil; servis sınırları, veri sahipliği, olay tabanlı iletişim, kalite kapıları, konteynerleştirme ve gözlemlenebilirlik konularını birlikte gösterecek bir portföy projesi olarak geliştirilmektedir. Mikroservis sayısını azaltmama kararı [ADR-0001](docs/adr/0001-mikroservis-mimarisini-koruma.md) ile kayıt altındadır.

## Mimari

| Bileşen | Sorumluluk | Veri / teknoloji |
|---|---|---|
| Auth | Kimlik, profil, roller, takip ve rozetler | .NET 8, PostgreSQL, Redis |
| Product | Ürün kataloğu, konu ve yayın akışı | .NET 8, PostgreSQL |
| Voting | Oyların authoritative write modeli | .NET 8, SQLite, Kafka |
| Comment | Yorum ve cevap yaşam döngüsü | .NET 8, PostgreSQL, Kafka |
| Notification | Kullanıcı bildirimleri | .NET 8, SQLite, Kafka |
| Analytics | Olaylardan analitik read model üretimi | .NET 8, SQLite, Kafka |
| AI | Analiz, etiket ve öneri yetenekleri | .NET 8, SQLite, Gemini (opsiyonel) |
| Gateway | Dış API giriş noktası ve yönlendirme | YARP, JWT |
| Web | Kullanıcı ve yönetim arayüzü | Next.js 16, React 19, TypeScript |

Altyapı PostgreSQL 16, Redis 7, Kafka/Zookeeper ve Docker Compose ile ayağa kalkar. Gateway dış dünyaya `5000`, web uygulaması `3000` portundan açılır; uygulama servisleri Compose ağı içinde kalır.

## Hızlı başlangıç

Gereksinimler:

- .NET 8 SDK
- Node.js 22 veya üzeri ve Corepack
- Docker Desktop / Docker Compose
- PowerShell 7 (doğrulama betiği için önerilir)

Ortam dosyasını oluşturun ve bütün `CHANGE_ME` değerlerini güçlü, yerel değerlerle değiştirin:

```powershell
Copy-Item .env.example .env
```

Tüm sistemi çalıştırın:

```powershell
docker compose up -d --build
docker compose ps
```

- Web: `http://localhost:3000`
- Gateway / API: `http://localhost:5000`
- Gateway health: `http://localhost:5000/health`

Sistemi durdurmak için `docker compose down` kullanın. Kalıcı verileri de silmek istediğinizden emin olmadıkça `--volumes` eklemeyin.

## Kalite kapıları

Backend testleri, frontend bağımlılık kontrolü, lint, strict TypeScript ve production build tek komutla çalışır:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

Kullanışlı seçenekler:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -SkipInstall
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -SkipRestore
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -CheckCompose
```

`-SkipRestore`, NuGet paketleri daha önce geri yüklendiyse çevrimdışı doğrulama için kullanılabilir.

Tekil komutlar:

```powershell
dotnet test Vitrin.sln --configuration Release

Set-Location src/Web/Vitrin.Web.UI
corepack pnpm install --frozen-lockfile
corepack pnpm lint
corepack pnpm typecheck
corepack pnpm build
```

pnpm sürümü `package.json` içinde sabitlenmiştir. npm/yarn lockfile üretmeyin; authoritative frontend lockfile `pnpm-lock.yaml` dosyasıdır.

## Depo yapısı

```text
src/
  Gateways/                YARP API Gateway
  Services/                Auth, Product, Voting, Comment, Notification, Analytics, AI
  Shared/                  Servisler arası kernel ve event contract'ları
  Web/Vitrin.Web.UI/       Next.js uygulaması
tests/                     Servis birim testleri
scripts/                   Doğrulama, smoke test ve yerel yönetim araçları
docs/                      Yol haritası ve mimari karar kayıtları
docker-compose.yml         Yerel mikroservis orkestrasyonu
```

## Dokümantasyon

- [Kapsamlı proje incelemesi ve geliştirme yol haritası](docs/PROJE-GELISTIRME-YOL-HARITASI.md)
- [ADR-0001 — Mikroservis mimarisini koruma kararı](docs/adr/0001-mikroservis-mimarisini-koruma.md)

## Güvenlik notları

- `.env` commit edilmez; yalnızca `.env.example` şablondur.
- Compose, PostgreSQL, JWT ve NextAuth sırları verilmeden başlamaz.
- OAuth, Cloudinary ve Gemini değerleri ihtiyaca göre yerel ortamdan sağlanır.
- Örnek veya test kullanıcı parolalarını kaynak koda yazmayın.
- Sır sızıntısı şüphesinde değeri yalnızca dosyadan silmeyin; sağlayıcı tarafında da döndürün (rotate).

## Proje durumu

Aşama 0 stabilizasyonunda depo hijyeni, tek paket yöneticisi, frontend kalite kapıları, backend test tabanı, güvenli konfigürasyon ve tekrarlanabilir doğrulama akışı kurulmuştur. Sonraki çalışmalar yol haritasındaki sırayla servis veri sahipliği, event güvenilirliği, entegrasyon testleri, gözlemlenebilirlik ve CI/CD üzerine ilerleyecektir.
