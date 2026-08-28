# Vitrin — Backend Mimari ve Production Kılavuzu

> Son güncelleme: 27 Ağustos 2026
>
> Bu belge Vitrin'in backend'ini **tek bir yerden** anlatan referans dokümandır.
> Projeyi yeni birine anlatırken, production'a çıkmadan önce kontrol listesi
> olarak kullanırken veya bir karar gerekçesine bakarken bu belgeye bakın.
>
> **Son Güncelleme Özeti:**
> - ✅ Observability stack tamamlandı (OpenTelemetry, enhanced health checks, Serilog standardizasyonu)
> - ✅ Nginx güvenlik başlıkları ve endpoint koruması (/metrics, /health/detail) aktif
> - ✅ CI/CD secret validation ve container security scanning entegre edildi
> - ✅ Backup & restore scriptleri hazır (S3 desteği dahil)
> - ✅ Redis cache & password auth desteği eklendi

---

## İçindekiler

1. [Projeye Genel Bakış](#1-projeye-genel-bakış)
2. [Mimari — Büyük Resim](#2-mimari--büyük-resim)
3. [Her Servis Ne Yapar?](#3-her-servis-ne-yapar)
4. [API Gateway Detayı](#4-api-gateway-detayı)
5. [Güvenlik Mimarisi](#5-güvenlik-mimarisi)
6. [Hata Yönetimi ve Loglama](#6-hata-yönetimi-ve-loglama)
7. [Veritabanı Katmanı](#7-veritabanı-katmanı)
8. [Mesajlaşma ve Arka Plan İşleri](#8-mesajlaşma-ve-arka-plan-işleri)
9. [Observability Stack](#9-observability-stack)
10. [CI/CD ve Deployment](#10-cicd-ve-deployment)
11. [Resilience — Dayanıklılık Katmanı](#11-resilience--dayanıklılık-katmanı)
12. [Design Pattern'ler](#12-design-patternler)
13. [API Tasarım Standartları](#13-api-tasarım-standartları)
14. [Test Stratejisi](#14-test-stratejisi)
15. [Production Kontrol Listesi](#15-production-kontrol-listesi)
16. [Neden Yapılmadı? — Bilinçli Kararlar](#16-neden-yapılmadı--bilinçli-kararlar)

---

## 1. Projeye Genel Bakış

Vitrin, Türkiye'ye yönelik bir ürün keşif platformudur. Kullanıcılar ürün keşfeder,
oy verir, yorum yapar; maker'lar ürünlerini sergiler ve analitiklerini takip eder.

**Tech Stack (Backend):**
- Dil / Framework: **C# .NET 8** — Minimal API pattern (controller yok)
- Mimari: **Microservices + DDD + CQRS**
- Mesajlaşma: **Apache Kafka**
- Cache / Blacklist: **Redis**
- Veritabanı: **PostgreSQL** (kritik servisler) + **SQLite** (hafif servisler)
- Gateway: **YARP** (Yet Another Reverse Proxy)
- Container: **Docker + Docker Compose**
- Registry: **GitHub Container Registry (GHCR)**
- CI/CD: **GitHub Actions**
- Prod Sunucu: **AWS EC2 t3.medium**
- CDN / DDoS: **Cloudflare Free**

---

## 2. Mimari — Büyük Resim

```
İnternet
    │
    ▼
[Cloudflare] ── CDN, DDoS absorpsiyon, SSL katmanı
    │
    ▼
[Nginx]  ── HTTPS termination, security headers, port 80/443
    │
    ├── Port 3000 → [Next.js Web UI]
    │                   │ /api/* rewrites (internal)
    │                   ▼
    └── Port 5000 → [YARP API Gateway]
                        │
                        │  JWT doğrulama
                        │  Rate Limiting (9 policy)
                        │  Token Blacklist kontrolü
                        │  CORS
                        │  Circuit Breaker + Retry + Timeout (Polly)
                        │
            ┌───────────┼────────────────────────┐
            │           │                        │
         [Auth]     [Product]               [Voting]
        :8080        :8080                   :8080
       PostgreSQL   PostgreSQL              SQLite
        Redis        Kafka                  Kafka
        Kafka
            │           │                        │
         [Comment]  [Notification]          [Analytics]
          :8080       :8080                   :8080
         PostgreSQL  SQLite                  SQLite
          Kafka       Kafka                  Kafka
            │
          [AI]
          :8080
          SQLite
          Gemini API

Ortak altyapı (tüm servisler erişir):
  ├── PostgreSQL :5432  (Auth, Product, Comment)
  ├── Redis :6379        (Gateway: blacklist + rate limit; Product: cache)
  ├── Kafka :9092        (Event bus)
  └── Elasticsearch :9200 (Log sink — Serilog)
```

**Temel kural:** Her servis kendi verisinin tek sahibidir. Başka servisin
tablosuna doğrudan erişim yasaktır. Servisler arası iletişim Kafka event'leri
veya Gateway üzerinden HTTP ile yapılır.

---

## 3. Her Servis Ne Yapar?

### Auth Service
Kimlik doğrulama, profil yönetimi ve platform güvenliğinin tüm sorumluluğu burada.

**Ne yapar:**
- Kayıt (e-posta + şifre) ve Google OAuth girişi
- E-posta doğrulama ve şifre sıfırlama (HMAC-signed token, purpose-scoped)
- JWT üretimi ve logout (Redis blacklist)
- Kullanıcı rolleri: `User`, `Maker`, `Admin`
- Takip sistemi (follow/unfollow)
- Rozetler ve gamification (streak takibi)
- Maker başvurusu ve onay akışı
- Moderasyon: raporlar, ban/kaldırma, itirazlar, audit log
- Feature flags (DB-backed, rollout percentage, role-based)
- KVKK: veri export + 30 günlük silinme bekleme + anonimleştirme

**Veritabanı:** PostgreSQL `vitrin_auth`
**Dış bağımlılık:** Redis (blacklist), Kafka (event), Elasticsearch (log), Resend API (e-posta)

---

### Product Service
Platformun katalog kalbi. Her şey ürün etrafında döner.

**Ne yapar:**
- Ürün oluşturma, düzenleme, yayınlama, arşivleme
- İnceleme akışı (UnderReview → Published/Rejected/Scheduled)
- Tam metin arama: PostgreSQL full-text search + trigram similarity + ILike
- Gelişmiş filtreleme + keyset cursor pagination
- Trend skoru hesaplama (upvote + yorum + görüntülenme + yaş faktörü)
- Topic, kategori, koleksiyon yönetimi
- Takip sistemi (ürün + topic follow)
- Launch akışı (zamanlanmış yayınlama)
- Karşılaştırma (en fazla 4 ürün)
- Kayıtlı aramalar + bildirim
- Sahiplik transferi ve ekip yönetimi
- Revizyon geçmişi

**Veritabanı:** PostgreSQL `vitrin_product`
**Dış bağımlılık:** Kafka (event), Redis (cache — `ICacheService`)

---

### Voting Service
Oy sisteminin tek yazma otoritesi. Karmaşıklık kasıtlı olarak minimize edilmiştir.

**Ne yapar:**
- Oy ekleme ve geri alma
- Upvote sayımı (single source of truth)
- Fraud signal tespiti (hızlı oy patlaması, tek kullanıcı yoğunluğu)

**Veritabanı:** SQLite `voting_db.sqlite`
**Dış bağımlılık:** Kafka (VoteAdded, VoteRemoved event'leri → Product read model)

---

### Comment Service
Yorum sisteminin tam stack'i.

**Ne yapar:**
- Yorum ekleme, düzenleme, silme (soft-delete)
- İç içe yanıt (parent-child)
- @mention desteği
- Emoji reaction (like, love, laugh vb.)
- Moderasyon (hide/restore + audit)
- Activity feed

**Veritabanı:** PostgreSQL `vitrin_comment`
**Dış bağımlılık:** Kafka (event)

---

### Notification Service
Tüm bildirim kanallarının koordinatörü.

**Ne yapar:**
- In-app bildirim inbox'ı
- Server-Sent Events (SSE) ile gerçek zamanlı bildirim stream
- E-posta digest (Resend API veya SMTP) — 15 dakikada bir kontrol
- Bildirim tercihleri (tip bazlı açma/kapama)
- Newsletter abonelik yönetimi
- `NotificationDigestWorker` — 15dk polling ile düzenliyDue kontrol

**Veritabanı:** SQLite `notification_db.sqlite`
**Dış bağımlılık:** Kafka (consume), Resend API (e-posta)

---

### Analytics Service
Platform ve ürün metriklerinin toplanıp sorgulandığı yer.

**Ne yapar:**
- Event ingestion (view, upvote, comment, search vb.)
- Ürün bazlı özet (views + upvotes + comments)
- Günlük time-series (görüntülenme trendi)
- Referrer istatistikleri
- Retention istatistikleri
- Maker dashboard (en fazla 50 ürün batch query)
- Platform geneli özet (admin için)
- `AnalyticsDailyAggregationWorker` — her gece 03:30 UTC, günlük metrik + 90 gün+ temizlik

**Veritabanı:** SQLite `analytics_db.sqlite`
**Dış bağımlılık:** Kafka (consume)

---

### AI Service
Gemini API tabanlı ürün analizi.

**Ne yapar:**
- Ürün adı + açıklamasından otomatik özet ve etiket üretimi
- Etiket eşleşmesiyle "benzer ürünler" önerisi
- Kullanıcı başına günlük kota (SQLite UPSERT, atomik)
- Prompt injection koruması (sanitize + karakter limiti)

**Veritabanı:** SQLite `ai_db.sqlite`
**Dış bağımlılık:** Gemini API

---

### API Gateway (YARP)
Dışarıdan gelen tüm HTTP trafiğinin tek giriş noktası.

Ayrıntılı açıklaması bir sonraki bölümde.

---

## 4. API Gateway Detayı

Gateway, **YARP (Yet Another Reverse Proxy)** tabanlıdır ve şunları yapar:

### Sorumluluklar

| Görev | Nasıl |
|-------|-------|
| JWT doğrulama | `AddVitrinJwtAuthentication` — imza, issuer, audience, lifetime |
| Token blacklist kontrolü | Her authenticated istekte Redis'te `jti` kontrolü |
| Ban kontrolü | `vitrin:banned` claim'i varsa 403; appeal/me/notification hariç |
| Rate limiting | 9 policy, Redis sliding window + in-memory fallback |
| CORS | `Cors:AllowedOrigins` whitelist — `*` yok |
| Circuit breaking | Polly v8 — 3 profil (Critical/Voting/Tolerant) |
| Retry + Timeout | Polly pipeline — cluster başına farklı değer |
| Routing | Path-based → her servis kendi cluster'ında |
| API versiyonlama | `/api/v1/*` → `PathRemovePrefix: /v1` → backend `/api/*` alır |

### Rate Limiter Policy'leri

Redis sliding window (Lua script, atomik). Gateway yeniden başlasa bile sayaçlar korunur.

```
Policy              Kapsam    Limit   Pencere
────────────────────────────────────────────────
auth-login          IP        5       1 dakika
auth-registration   IP        3       10 dakika
auth-external-login IP        10      1 dakika
api-write           User/IP   60      1 dakika
social-write        User/IP   30      1 dakika
search-query        User/IP   90      1 dakika
analytics-event     User/IP   30      1 dakika
analytics-query     User/IP   45      1 dakika
ai-analysis         User/IP   5       1 dakika
```

Fallback: Redis erişilemezse fail-open + in-memory FixedWindow devreye girer.
429 yanıtında `Retry-After` header döner.

### Circuit Breaker Profilleri

```
Profil     Servisler                Timeout  Retry  CB Eşiği  Break
───────────────────────────────────────────────────────────────────
Critical   auth, product, comment   8s       3      %50       30s
Voting     voting                   5s       1      %60       20s
Tolerant   analytics, notif, ai     15s      2      %70       60s
```

Pipeline sırası (içten dışa): `Timeout → Retry → Circuit Breaker`

### API Versiyonlama Mantığı

```
İstemci:  GET /api/v1/products
Gateway:  PathRemovePrefix: /v1  →  /api/products
Servis:   /api/products dinler — değişmez

Geriye uyumlu: /api/* path'leri hâlâ çalışır.
```

---

## 5. Güvenlik Mimarisi

### Katmanlı Savunma

```
Katman 1: Cloudflare   → DDoS absorpsiyon, bot filtresi, CDN
Katman 2: Nginx        → HTTPS, security headers, internal path'leri 403
Katman 3: Gateway      → JWT, blacklist, rate limit, CORS, ban kontrolü
Katman 4: Servis       → Policy-based authorization (Admin, MakerOrAdmin)
Katman 5: Domain       → İş kuralı sahiplik kontrolü (MakerId == userId?)
```

### JWT Yönetimi

- Süre: **1 saat** (kısa tutuldu, refresh token ihtiyacını azaltır)
- Her token benzersiz `jti` claim taşır
- Logout → `jti` Redis'e yazılır, TTL = tokenın kalan ömrü
- Gateway her istekte blacklist kontrolü yapar
- Kullanıcı kimliği her zaman `ClaimsPrincipal`'dan alınır, body/query'den asla
- JWT Secret minimum 32 byte — uygulama başlarken guard kontrolü yapar

### Şifre Güvenliği

- BCrypt ile hash + salt
- Minimum kural: 8-128 karakter, büyük harf + küçük harf + rakam + özel karakter
- Forgot password: HMAC-signed token, 1 saat geçerli, purpose-scoped

### CORS Politikası

```json
// Gateway appsettings.json veya .env
"Cors:AllowedOrigins": [
  "https://vitrin.it.com",
  "http://localhost:3001"
]
```

`*` asla kullanılmaz. Sadece güvenilen origin'ler.

### Güvenlik HTTP Başlıkları (Nginx)

```
Strict-Transport-Security: max-age=31536000; includeSubDomains
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Content-Security-Policy: default-src 'self'; ...
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
```

### Input Validation

- `ValidationEndpointFilter<T>` — MediatR command'larında otomatik validation
- Inline validation: karakter limiti, boş kontrol, enum parse
- EF Core parametrized query — SQL injection yok
- AI servisi: prompt injection için sanitize + length limit

### Korunan Endpoint'ler

| Endpoint | Koruma |
|----------|--------|
| `/metrics` | Nginx 403 — sadece iç ağdan Prometheus scrape eder |
| `/health/detail` | Nginx 403 — sadece iç ağ |
| `/api/admin/*` | `AdminOnly` policy |
| `/api/ai/analyze` | `MakerOrAdmin` policy + rate limit + günlük kota |
| Tüm yazma endpoint'leri | `RequireAuthorization` + kaynak sahipliği kontrolü |

### Container ve Dependency Güvenliği

- **Trivy**: Her deploy'da gateway, auth, web image'larını CRITICAL/HIGH tarar
- **SBOM (Syft)**: CycloneDX JSON format, 90 gün artifact — hangi paket, sürüm, lisans
- **`dotnet list package --vulnerable`**: CI'da çalışır, Critical → PR bloklayıcı
- **`pnpm audit`**: Frontend bağımlılık taraması
- `docker-compose.yml`'de `POSTGRES_PASSWORD`, `JWT_SECRET`, `NEXTAUTH_SECRET` `:?` ile zorunlu

---

## 6. Hata Yönetimi ve Loglama

### Global Exception Handler

Her serviste `VitrinGlobalExceptionHandler` aktif:
- `BadHttpRequestException` → 400 + "request malformed"
- Diğer tüm exception'lar → 500 + jenerik mesaj
- **Stack trace hiçbir zaman kullanıcıya dönmez**
- Her yanıtta `traceId` — Jaeger'a doğrudan erişim sağlar

### HTTP Status Code Sayfaları

400, 401, 403, 404, 405, 429, 500 için standart JSON body:

```json
{
  "status": 404,
  "title": "The requested resource was not found.",
  "detail": "Product not found.",
  "code": "product.not_found",
  "instance": "/api/v1/products/abc",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

RFC 9457 ProblemDetails formatı. `code` alanı frontend'in string-based hata mesajı göstermesini sağlar.

### Serilog — Yapılandırılmış Loglama

**Tüm 7 servis** (bu güncellemeyle tamamlandı):

```
Console   → Development ortamında (okunabilir)
File      → Rolling daily, 7 gün retention, 100MB limit
Elasticsearch → Production'da Kibana ile analiz
```

Log enrichment — her log satırında:
```
ServiceName = "Vitrin.Product"
Environment = "Docker"
Version     = "1.0.0"
```

Minimum seviye: `LOG_LEVEL` env değişkeniyle override edilebilir (varsayılan: `Information`).

### OpenTelemetry — Distributed Tracing

**Tüm 7 servis** aktif. Bir kullanıcı isteği Gateway'den Product'a gidip Kafka'ya event yazana kadar tek bir trace altında izlenebilir.

Instrumentation:
- ASP.NET Core HTTP pipeline
- HttpClient (servisler arası çağrılar)
- EF Core (SQL sorguları)
- Redis (StackExchange)

Exporter: Jaeger (OTLP) + Console (dev)

### Prometheus Metrics

Her serviste `/metrics` endpoint aktif. Grafana dashboard'da görüntülenir.

---

## 7. Veritabanı Katmanı

### Hangi Servis Hangi DB?

| Servis | Veritabanı | Gerekçe |
|--------|------------|---------|
| Auth | PostgreSQL `vitrin_auth` | İlişkisel, ACID, full-text |
| Product | PostgreSQL `vitrin_product` | Karmaşık sorgu, index, full-text search |
| Comment | PostgreSQL `vitrin_comment` | Parent-child ilişki, soft delete |
| Voting | SQLite | Yüksek yazma hızı, basit şema |
| Notification | SQLite | Hafif, portabl |
| Analytics | SQLite | Hafif, portabl |
| AI | SQLite | Hafif, portabl |

> **Not:** Yoğun trafik veya multi-replica senaryosunda Voting, Analytics, Notification ve AI
> servislerinin PostgreSQL'e geçirilmesi gerekebilir. SQLite concurrent write'ı desteklemez.

### Migration Stratejisi

Her servis başlangıcında `MigrateDatabaseAndExitAsync<TDbContext>()` çalışır.
Ayrı bir migration container veya manuel müdahale gerekmez.

### Index Stratejisi

```sql
-- Performans index'leri (Auth)
CREATE INDEX idx_users_email        ON Users(Email);
CREATE INDEX idx_users_username     ON Users(Username);
CREATE UNIQUE INDEX uk_users_email  ON Users(lower(Email));  -- case-insensitive

-- Ürün listeleme
CREATE INDEX idx_products_status_date ON Products(Status, PublishedAt DESC);

-- Full-text search (Product)
-- SearchVector: computed tsvector column
CREATE INDEX idx_products_fts   ON Products USING gin("SearchVector");
CREATE INDEX idx_products_trgm  ON Products USING gin(Name gin_trgm_ops);

-- Yorum listeleme
CREATE INDEX idx_comments_product ON Comments(ProductId, CreatedAt DESC);
CREATE INDEX idx_comments_parent  ON Comments(ParentCommentId);

-- Bildirim inbox
CREATE INDEX idx_notif_user_unread ON Notifications(UserId, IsRead, CreatedAt DESC);
```

### Connection Pooling

EF Core varsayılan Npgsql pool kullanılıyor. Tüm servislerin connection count'u
Prometheus'taki `postgres-exporter` metrikleriyle izleniyor.

### Redis Cache

`ICacheService` / `RedisCacheService` — Product servisine inject edilmiş.

```csharp
// Kullanım örneği
var cached = await _cache.GetAsync<List<TopicResponse>>("topics:all");
if (cached is null)
{
    cached = await _db.Topics.ToListAsync();
    await _cache.SetAsync("topics:all", cached, TimeSpan.FromMinutes(30));
}
```

Invalidasyon: key bazlı veya pattern bazlı (`InvalidatePatternAsync`).
Redis'e ulaşılamazsa cache miss gibi davranır, servis çalışmaya devam eder.

### Veritabanı Yedekleme

```
postgres-backup container — Her gece 02:00 UTC
├── vitrin_auth.sql.gz
├── vitrin_product.sql.gz
└── vitrin_comment.sql.gz

Retention: 7 gün
Opsiyonel: S3_BUCKET env tanımlanırsa S3'e de yükler
```

**⚠️ Kritik:** Yedeklerin gerçekten geri yüklenebilir olduğu düzenli aralıklarla test edilmeli.

---

## 8. Mesajlaşma ve Arka Plan İşleri

### Kafka Event Akışı

Servisler birbirine **Kafka** üzerinden event göndererek konuşur.
Hiçbir servis başka bir servisin veritabanına doğrudan erişmez.

#### Event Catalog

| Event | Topic | Producer | Consumer |
|-------|-------|----------|----------|
| `voting.vote_added` | `voting-events` | Voting | Product (read model güncelleme), Analytics |
| `voting.vote_removed` | `voting-events` | Voting | Product, Analytics |
| `notification.send` | `notification-events` | Auth, Comment | Notification |
| `analytics.product_viewed` | `analytics-events` | Product | Analytics |
| `analytics.search_performed` | `analytics-events` | Product | Analytics |
| `product.published` | `social-events` | Product | Analytics |
| `user.registered` | `user-events` | Auth | Analytics |
| `user.role_changed` | `user-events` | Auth | Analytics |

#### Teslimat Garantisi

- **at-least-once** — mesaj en az bir kez teslim edilir
- Producer idempotence açık
- Consumer offset: işleme bittikten sonra commit
- Bounded retry + exponential backoff
- Son başarısız deneme → `<topic>.dlq` topic'ine yazar

### Outbox / Inbox Pattern

**Neden gerekli:** Kafka'ya yazma ile DB'ye yazma ayrı sistemler. Biri başarılı
diğeri başarısız olabilir. Outbox bu ikisini aynı transaction'a bağlar.

```
[Servis DB Transaction — tek commit]
    ├── İş verisi (ürün oluştu, oy verildi...)
    └── OutboxMessage (event serialized)

[OutboxDispatcher — background, polling]
    └── Kafka'ya publish → ProcessedAtUtc = now

[Consumer — başka servis]
    ├── InboxMessage yaz (EventId = primary key = idempotent)
    └── İş sonucu yaz (read model güncelle, bildirim oluştur...)
```

**Garanti:** DB commit başarılıysa event kaybolmaz.

### BackgroundService İşleri

| Worker | Zamanlama | Ne Yapar |
|--------|-----------|----------|
| `OutboxDispatcher<TDbContext>` | Sürekli polling | Outbox mesajlarını Kafka'ya iletir |
| `OutboxCleanupWorker<TDbContext>` | Pazar 04:00 UTC | İşlenmiş outbox/inbox satırlarını temizler |
| `NotificationDigestWorker` | 15 dakikada bir | E-posta digest gönderimini kontrol eder |
| `AnalyticsDailyAggregationWorker` | Her gece 03:30 UTC | Günlük metrik hesabı + 90 gün+ event temizliği |
| `RetentionCleanupWorker` | Her gece 03:00 UTC | 30 günü dolan KVKK silme taleplerini anonimleştirir |
| `ScheduledLaunchWorker` | Periyodik | Zamanlanmış ürün yayınlarını kontrol eder |
| `postgres-backup` | Her gece 02:00 UTC | pg_dump + 7 gün retention |

---

## 9. Observability Stack

"Bir şey yanlış giderse nasıl fark ederiz ve nereden bakarız?" sorusunun yanıtı.

### Bileşenler

| Araç | Adres (dev) | Görev |
|------|-------------|-------|
| Grafana | http://localhost:3004 | Dashboard, alarm, görselleştirme |
| Prometheus | http://localhost:9091 | Metrik toplama |
| Jaeger | http://localhost:16686 | Distributed tracing |
| Elasticsearch | http://localhost:9200 | Log depolama |
| Kibana | http://localhost:5601 | Log analizi |

### Infrastructure Exporter'lar

| Exporter | Neyi İzler |
|----------|------------|
| `postgres-exporter` | Sorgu süresi, bağlantı sayısı, tablo boyutu |
| `redis-exporter` | Bellek kullanımı, komut istatistikleri, hit/miss oranı |
| `kafka-exporter` | Topic lag, mesaj oranları, consumer group durumu |

### Golden Signals (RED Method)

Her servis için izlenen üç temel metrik:

```
Rate    → rate(http_requests_total[5m])
Errors  → rate(http_requests_total{code=~"5.."}[5m])
Duration → histogram_quantile(0.95, http_request_duration_seconds_bucket[5m])
```

### SLO Hedefleri

| Metrik | Hedef |
|--------|-------|
| Public API availability | %99.5 |
| p95 yanıt süresi | < 1000ms |
| p99 yanıt süresi | < 2000ms |
| Vote command başarı | %99.0 |

### Önemli Health Endpoint'ler

```
/health          → {"status":"healthy"} veya 503
                   Nginx üzerinden dışarıya açık, bilgi sızdırmaz

/health/detail   → Tüm bağımlılıkların detaylı durumu
                   Sadece Docker iç ağından erişilebilir (Nginx 403 blokar)
                   Örnek yanıt:
                   {
                     "status": "healthy",
                     "entries": {
                       "database": {"status": "healthy", "durationMs": 12},
                       "redis":    {"status": "healthy", "durationMs": 3},
                       "kafka":    {"status": "healthy", "durationMs": 8}
                     }
                   }

/metrics         → Prometheus scrape endpoint
                   Nginx ile sadece iç ağa açık
```

---

## 10. CI/CD ve Deployment

### Pipeline Akışı (deploy.yml)

```
git push main + commit mesajında [deploy]
    │
    ▼
1. check-deploy  → [deploy] var mı? workflow_dispatch?
    │
    ▼
2. test          → dotnet restore → build → test
    │
    ▼
3. security-scan → dotnet vuln scan + pnpm audit + Trivy FS scan
    │
    ▼
4. build         → 9 servis paralel Docker build → GHCR push
                   Her image: :sha1234 (rollback) + :latest tag
    │
    ▼
5. image-scan    → Trivy image scan (gateway, auth, web) → SARIF → GitHub Security
                   Syft SBOM üretimi → CycloneDX JSON → 90 gün artifact
    │
    ▼
6. deploy        → SSH to EC2, throttled sıralı image pull
                   Her servis: restart → health check → sonraki servis
    │
    ▼
7. smoke-test    → Playwright e2e (vitrin.it.com üzerinde)
    │
    ├── Başarılı → pipeline biter ✅
    │
    └── Başarısız ▼
8. rollback      → Önceki image'lara otomatik geri dön
```

### Rolling Deployment Stratejisi

```bash
for service in auth product voting comment notification analytics ai gateway; do
  docker compose pull $service
  docker compose up -d --no-deps $service
  # health check: 12 deneme, 5s aralık
  curl -sf http://localhost:8080/health || exit 1
done
```

Her servis ayrı restart edilir. Gateway en son yeniden başlatılır.

### Ortam Yönetimi

```
docker-compose.yml          → Development / Staging (local build)
docker-compose.prod.yml     → Production override (GHCR image'ları kullan)
.env                        → Tüm secret'lar (git'e commit edilmez!)
.env.example                → Hangi değişkenler gerekli (commit edilir)
```

---

## 11. Resilience — Dayanıklılık Katmanı

### Polly Pipeline'ı

Gateway'deki her YARP cluster'ı Polly pipeline'ından geçer:

```
İstek → [Timeout] → [Retry] → [Circuit Breaker] → Servis
```

**Timeout:** İstek bu süreyi aşarsa iptal et.
**Retry:** Başarısız istekleri exponential backoff + jitter ile tekrar dene.
**Circuit Breaker:** Belirli hata oranı aşılırsa devreyi aç, servis toparlanana kadar bekle.

### Profil Detayları

```
Critical (Auth, Product, Comment):
  Timeout:  8s
  Retry:    3 deneme, 150ms base delay, jitter ekli
  CB:       30s örnekleme, min 5 istek, %50 hata → 30s break

Voting:
  Timeout:  5s
  Retry:    1 deneme, 100ms base delay
  CB:       20s örnekleme, min 10 istek, %60 hata → 20s break

Tolerant (Analytics, Notification, AI):
  Timeout:  15s
  Retry:    2 deneme, 300ms base delay
  CB:       60s örnekleme, min 3 istek, %70 hata → 60s break
```

### Başarısızlık Senaryoları

| Senaryo | Davranış |
|---------|----------|
| Redis erişilemez (rate limit) | Fail-open: istek geçer, in-memory fallback aktif |
| Redis erişilemez (cache) | Cache miss gibi davran, DB'den al |
| Servis timeout | Retry → Circuit Breaker açılır → 503 döner |
| Kafka producer hatası | Outbox'ta bekler, dispatcher tekrar dener |
| Kafka consumer hatası | Exponential backoff retry → DLQ |

---

## 12. Design Pattern'ler

Projedeki önemli pattern'ler ve nerelerde uygulandığı:

### Mimari Pattern'ler

| Pattern | Nerede | Neden |
|---------|--------|-------|
| **Microservices** | Tüm proje | Her domain bağımsız deploy, scale, fail edilebilir |
| **CQRS** | Tüm servisler | Okuma ve yazma modeli ayrımı, her biri optimize edilebilir |
| **Domain-Driven Design** | Tüm servisler | Bounded context, aggregate root, domain event |
| **Outbox Pattern** | Auth, Product, Voting, Comment | DB + event kaydı atomik; event kaybolmaz |
| **Inbox Pattern** | Product, Analytics, Notification | Consumer idempotency; aynı event iki kez işlenmez |
| **Event-Driven** | Kafka üzerinden | Servisler arası loose coupling |
| **API Gateway** | YARP | Tek giriş noktası; cross-cutting concern'ler merkezi |

### Uygulama Pattern'leri

| Pattern | Nerede |
|---------|--------|
| **Repository** | `IProductRepository`, `INotificationRepository` vb. — DB erişimini soyutlar |
| **MediatR (Mediator)** | Tüm `Command`/`Query` handler'lar — handler'ları doğrudan çağırmaz, mediator üzerinden |
| **Strategy** | Rate limiter policy'leri — her endpoint için farklı algoritma |
| **Decorator** | Middleware pipeline — auth, rate limit, tracing birbirini wrap eder |
| **Factory** | `ResilienceForwarderHttpClientFactory` — cluster'a göre doğru HttpHandler döner |

---

## 13. API Tasarım Standartları

### URL Yapısı

```
/api/v1/{kaynak}/{id}/{alt-kaynak}

Örnekler:
GET  /api/v1/products              → Ürün listesi (filtre + cursor pagination)
GET  /api/v1/products/{slug}       → Tek ürün
POST /api/v1/products              → Ürün oluştur (MakerOrAdmin)
PUT  /api/v1/products/{id}/team    → Ekip güncelle
GET  /api/v1/analytics/product/{id}/timeseries → Time-series metrik
```

### HTTP Method'ları

| Method | Ne Zaman |
|--------|----------|
| `GET` | Okuma, yan etkisiz |
| `POST` | Kaynak oluşturma, tetikleme (process) |
| `PUT` | Tam güncelleme |
| `PATCH` | Kısmi güncelleme |
| `DELETE` | Silme |

### Hata Yanıtı (RFC 9457)

```json
{
  "status": 400,
  "title": "The request could not be processed.",
  "detail": "Product name must contain between 1 and 200 characters.",
  "code": "product.name_invalid",
  "instance": "/api/v1/products",
  "traceId": "00-abc..."
}
```

### Pagination (Cursor-based)

Offset pagination kullanılmaz. Büyük tablolarda performansı bozar.

```json
{
  "items": [...],
  "nextCursor": "eyJzb3J0IjoibmV3ZXN0...",
  "hasMore": true
}
```

`nextCursor` bir sonraki isteğe `?cursor=...` olarak geçilir.
Cursor; sort, timestamp, id ve filter scope hash'ini içerir.

### Swagger / OpenAPI

Her servis kendi Swagger UI'ını sunar (dev ortamında):
- `http://localhost:{port}/swagger`
- Bearer token desteği mevcut

---

## 14. Test Stratejisi

### Test Piramidi

```
         E2E (Playwright)
        ─────────────────
      Integration (Testcontainers)
      ──────────────────────────────
    Unit (xUnit — backend, Vitest — frontend)
    ──────────────────────────────────────────
```

| Katman | Araç | Ne Test Eder | Adet |
|--------|------|--------------|------|
| Unit | xUnit, Vitest, RTL | Domain kuralları, handler davranışı | 113 backend + 11 frontend |
| Integration | Testcontainers (PG/Kafka/Redis) | Migration, constraint, retry, cache | 19 |
| E2E | Playwright + axe-core | Gerçek tarayıcı smoke akışı | prod'da çalışır |

### Coverage Eşikleri

```
Backend satır coverage  : min %35 (mevcut: %38)
Frontend kritik modüller: min %70 (mevcut: %89)
```

### CI'daki Test Akışı

```
PR açıldığında (ci.yml):
  backend tests → frontend type check + lint + build → security audit

main'e deploy (deploy.yml):
  test → security-scan → build → image-scan → deploy → smoke-test
```

---

## 15. Production Kontrol Listesi

### ✅ Tamamlandı (27 Ağustos 2026)

#### Observability & Monitoring
- [x] **OpenTelemetry entegrasyonu** (v1.10.0)
  - Product, Voting, Comment, Notification, Analytics, AI servislerinde aktif
  - Distributed tracing + Activity source instrumentation
  - OTLP exporter (Jaeger/Tempo için hazır)
- [x] **Gelişmiş Health Checks**
  - Database connectivity (PostgreSQL)
  - Redis connectivity
  - Kafka connectivity
  - `/health/detail` (iç ağ) + `/health` (dışa açık)

#### Güvenlik
- [x] **Nginx güvenlik başlıkları** (tamamlandı)
  - HSTS (Strict-Transport-Security)
  - CSP (Content-Security-Policy)
  - X-Frame-Options, X-Content-Type-Options
  - Referrer-Policy, Permissions-Policy
- [x] `/metrics` endpoint dışarıya kapalı (nginx 403)
- [x] `/health/detail` dışarıya kapalı (nginx 403)

#### Infrastructure & CI/CD
- [x] **Redis cache entegrasyonu** (Product servisi)
  - `IDistributedCache` registered
  - Password auth support
- [x] **CI/CD Secret Validation** (smoke test öncesi)
  - E2E_TEST_EMAIL, E2E_TEST_PASSWORD kontrolü
  - Missing secret detection + fail early
- [x] **Security scanning pipeline**
  - .NET dependency scan
  - Frontend pnpm audit
  - Container image scan (Trivy)
  - SBOM generation (Syft)

#### Backup & Restore
- [x] **Otomatik PostgreSQL backup** (`scripts/backup-postgres.sh`)
  - 3 DB için ayrı dump (auth, product, comment)
  - Gzip compression + retention (7 gün)
  - S3 upload desteği (opsiyonel)
  - Cron-ready format
  - Dosya boyutu kontrol + alarm
- [x] **Restore scripti** (`scripts/restore-postgres.sh`)
  - S3'ten otomatik download desteği
  - İnteraktif onay mekanizması (güvenlik)
  - Bağlantı termination + DB drop-create
  - Gzip auto-detect
  - Post-restore validation (tablo + satır sayısı)

#### Logging
- [x] **Serilog standardizasyonu** (6 serviste güncel)
  - JSON structured logging
  - Console + File + Elasticsearch sinks
  - Request logging middleware
  - Exception enrichment

### 🔴 Zorunlu — Bunlar Olmadan Yayına Çıkma

### 🔴 Zorunlu — Bunlar Olmadan Yayına Çıkma

> **ℹ️ Not:** Altyapı kodu hazır ✅ — sadece manuel konfigürasyon gerekiyor.

#### Çevre Değişkenleri (Manuel Yapılacak)
- [ ] `.env` dosyasını `.env.example`'dan oluştur ve tüm boş değerleri doldur
  ```bash
  cp .env.example .env
  ```
- [ ] `JWT_SECRET` — minimum 32 karakter, güçlü random string
- [ ] `POSTGRES_PASSWORD` — güçlü şifre
- [ ] `NEXTAUTH_SECRET` — minimum 32 karakter
- [ ] `REDIS_PASSWORD` — güçlü şifre (boş bırakılırsa şifresiz çalışır)

#### Elasticsearch Güvenliği
- [x] ~~docker-compose.yml'de `xpack.security.enabled: true`~~ ✅ (27 Ağustos 2026)
- [ ] Elasticsearch şifrelerini oluştur (manuel):
  ```bash
  docker exec vitrin-elasticsearch bin/elasticsearch-setup-passwords auto
  # Üretilen şifreleri .env'e ekle
  ```

#### Backup & Restore Infrastructure
- [x] ~~Backup scripti~~ ✅ `scripts/backup-postgres.sh` (27 Ağustos 2026)
- [x] ~~Restore scripti~~ ✅ `scripts/restore-postgres.sh` (27 Ağustos 2026)
- [ ] Backup/restore test senaryosu çalıştır (manuel test):
  ```bash
  # 1. Backup al
  ./scripts/backup-postgres.sh
  
  # 2. Dry-run test
  ./scripts/restore-postgres.sh --dry-run --latest vitrin_auth
  
  # 3. Gerçek restore
  ./scripts/restore-postgres.sh --latest vitrin_auth
  ```

#### CI/CD Secrets
- [x] ~~Secret validation kodu~~ ✅ `.github/workflows/deploy.yml` (27 Ağustos 2026)
- [ ] GitHub Settings'ten secret'ları ekle (manuel):
  - `E2E_TEST_EMAIL` → Test kullanıcı email'i
  - `E2E_TEST_PASSWORD` → Test kullanıcı şifresi
  - `EC2_HOST` → Production sunucu IP
  - `EC2_USER` → SSH kullanıcı adı
  - `EC2_SSH_KEY` → SSH private key

### 🟡 Önerilir — Kısa Sürede Yapılmalı

- [ ] `OPENTELEMETRY_OTLP_ENDPOINT` — Grafana Tempo veya harici Jaeger
- [ ] `RESEND_API_KEY` — e-posta bildirimleri için
- [ ] Cloudflare DNS propagasyonunu doğrula
- [ ] Grafana alarm kurallarını test et (`.yml` dosyaları zaten hazır)
- [ ] Redis cache uygulama katmanı: `/api/topics` ve `/api/products/trending` endpoint'lerine `ICacheService` ekle
- [ ] **S3 backup testi** (YENİ):
  ```bash
  # .env'e ekle:
  S3_BUCKET=vitrin-backups
  # Test:
  ./scripts/backup-postgres.sh
  # S3'te dosyaları doğrula
  aws s3 ls s3://vitrin-backups/postgres/
  ```

### 🟢 Opsiyonel — Trafik Arttıkça

- [ ] Voting, Analytics, Notification, AI → PostgreSQL geçişi (SQLite concurrent write limiti)
- [ ] Sentry/Rollbar entegrasyonu (Grafana alarmlarıyla kısmen karşılanıyor)
- [ ] PostgreSQL read replica
- [ ] Redis Sentinel

---

## 16. Neden Yapılmadı? — Bilinçli Kararlar

Bazı şeylerin neden olmadığını bilmek, onları eklemenin neden önerilmediğini anlamak açısından önemlidir.

| Özellik | Neden Yok |
|---------|-----------|
| **Load Balancer (çoklu instance)** | Tek EC2 t3.medium üzerinde replica koymanın faydası yok — aynı donanımı paylaşırlar. Gerçek trafik artışında anlamlı. |
| **gRPC (servisler arası)** | 8 servis, hepsi aynı sunucuda. HTTP yeterince hızlı. gRPC ek araç seti karmaşıklığı getirir. |
| **GraphQL** | REST pattern tutarlılığı tercih edildi. GraphQL; çok farklı istemci ihtiyaçları veya N+1 sorunları olduğunda değer katar. |
| **Webhooks** | Kafka internal event bus iç iletişimi karşılıyor. Harici integrasyonlar için yapılabilir. |
| **Refresh Token** | 1 saatlik kısa token ömrüyle güvenlik riski azaltıldı. Kullanıcı deneyimi yeterli. |
| **Service Mesh (Istio/Linkerd)** | Kubernetes yok, 8 servis var. Service mesh onlarca servisli büyük organizasyonlar için. |
| **DB Sharding** | PostgreSQL mevcut yükü yıllarca kaldırır. Erken optimizasyon. |
| **Redis Cluster/Sentinel** | Tek instance %99.9 availability sağlıyor. Ölçek gerektiğinde yapılır. |
| **LaunchDarkly/Unleash (Feature Flags)** | DB-backed basit implementasyon yeterli. Harici servis gereksiz maliyet. |

---

## Referans

| Belge | Nerede |
|-------|--------|
| Sistem Tasarımı (detaylı mimari) | `docs/SYSTEM-DESIGN.md` |
| Event Catalog | `docs/event-catalog.md` |
| Test Stratejisi | `docs/testing-strategy.md` |
| Güvenlik Kontrol Listesi | `docs/SECURITY-CHECKLIST.md` |
| Veri Erişimi ve Index Rehberi | `docs/data-access-performance.md` |
| Observability Kurulum | `observability/README.md` |
| ADR'ler (mimari kararlar) | `docs/adr/` klasörü |
| Geliştirme Yol Haritası | `docs/PROJE-GELISTIRME-YOL-HARITASI.md` |
| Ortam Değişkenleri | `.env.example` |
