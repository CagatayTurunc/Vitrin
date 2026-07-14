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

İmajları oluşturun, her servisin migration job'ını tek sefer çalıştırın ve sistemi başlatın:

```powershell
docker compose build
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-migrations.ps1
docker compose up -d
docker compose ps
```

Uygulama process'leri normal startup sırasında schema değiştirmez. Yeni bir sürüm migration içeriyorsa rollout öncesinde aynı migration job'ı tekrar çalıştırılır.

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
- [ADR-0002 — Merkezi JWT ve güvenilir kimlik sınırı](docs/adr/0002-merkezi-jwt-ve-kimlik-siniri.md)
- [ADR-0003 — API hata, kota, rate limit ve audit katmanları](docs/adr/0003-api-koruma-katmanlari.md)
- [ADR-0004 — Güvenilir event teslimatı, Outbox ve Inbox](docs/adr/0004-event-teslimati-outbox-inbox.md)
- [ADR-0005 — Migration deployment job](docs/adr/0005-migration-deployment-job.md)
- [Event Catalog — topic, producer ve consumer matrisi](docs/event-catalog.md)
- [Veri erişimi, indeks ve EXPLAIN ANALYZE rehberi](docs/data-access-performance.md)

## Güvenlik notları

- `.env` commit edilmez; yalnızca `.env.example` şablondur.
- Compose, PostgreSQL, JWT ve NextAuth sırları verilmeden başlamaz.
- Gateway ve servisler JWT imzası, issuer, audience, süre ve algoritmayı ortak yapılandırmayla doğrular.
- Caller kimliği request body'den değil, doğrulanmış token içindeki `sub` claim'inden alınır.
- Admin ve Maker işlemleri ortak authorization policy'leriyle korunur.
- Google ID tokenı ve GitHub access tokenı Auth servisinde sağlayıcıya karşı doğrulanmadan Vitrin tokenı üretilmez.
- Login, register ve external-login istekleri Gateway'de istemci IP'sine göre sınırlanır.
- AI analizi hem kullanıcı bazlı dakikalık rate limit hem de SQLite'ta kalıcı UTC günlük kota uygular.
- API hata yanıtları RFC 7807 ProblemDetails biçimini ve izleme için `traceId` alanını kullanır.
- Kimlik, yönetim ve AI güvenlik olayları yapılandırılmış audit olayları olarak loglanır; token ve parola audit verisine yazılmaz.
- OAuth, Cloudinary ve Gemini değerleri ihtiyaca göre yerel ortamdan sağlanır.
- Örnek veya test kullanıcı parolalarını kaynak koda yazmayın.
- Sır sızıntısı şüphesinde değeri yalnızca dosyadan silmeyin; sağlayıcı tarafında da döndürün (rotate).

## Proje durumu

Aşama 0 stabilizasyonu, Aşama 1 güvenlik/doğruluk çalışmaları ve Aşama 2 tamamlanmıştır. OAuth sağlayıcı doğrulaması, merkezi JWT/policy katmanı, güvenilir caller identity sınırı, sahiplik kontrolleri, Gateway rate limiting, merkezi ProblemDetails, kalıcı AI kotası ve yapılandırılmış audit log temeline ek olarak semantik event catalog, Transactional Outbox, Inbox idempotency, bounded retry/backoff, Kafka DLQ ve event schema versioning uygulanmıştır. Voting oyların tek yazma otoritesidir; Product oy verisini event-driven read model olarak tutar. Veri katmanında keyset cursor pagination, DTO projection, `AsNoTracking`, sorguya göre bileşik indeksler, PostgreSQL `pg_trgm` araması, yarışa dayanıklı slug üretimi ve ayrı migration deployment job'ı bulunur. Sıradaki çalışma Aşama 3 test mimarisidir.
