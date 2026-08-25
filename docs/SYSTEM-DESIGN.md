# Vitrin — Sistem Tasarımı ve Mimari Belge

> Son güncelleme: Ağustos 2026 — Resilience layer (Polly) + Distributed Rate Limiting (Redis) eklendi
> Bu belge projedeki mevcut sistem tasarım kararlarını, uygulanan pattern'leri ve kullanılan kavramları tek bir referans noktasında toplar.

---

## İçindekiler

1. [Genel Mimari](#1-genel-mimari)
2. [Design Patterns](#2-design-patterns)
3. [API Gateway](#3-api-gateway)
4. [API Kavramları](#4-api-kavramları)
5. [Mesajlaşma — Batch vs Stream](#5-mesajlaşma--batch-vs-stream)
6. [System Design Temel Kavramları](#6-system-design-temel-kavramları)
7. [Observability Stack](#7-observability-stack)
8. [Güvenlik Mimarisi](#8-güvenlik-mimarisi)
9. [Veri Katmanı](#9-veri-katmanı)
10. [Test Mimarisi](#10-test-mimarisi)
11. [Event-Driven Mimari](#11-event-driven-mimari)
12. [Eksikler ve Sonraki Adımlar](#12-eksikler-ve-sonraki-adımlar)

---

## 1. Genel Mimari

Vitrin, mikroservis tabanlı bir ürün keşif platformudur. Servisler birbirinden bağımsız çalışır, Gateway tek dış giriş noktasıdır.

```
İnternet
    │
    ▼
[Nginx] ──── HTTPS termination, security headers
    │
    ▼
[Next.js Web UI :3003]
    │  /api/* rewrites
    ▼
[YARP API Gateway :5000] ──── JWT doğrulama, Rate Limiting, Routing
    │
    ├── [Auth Service :8080]         → PostgreSQL, Redis, Kafka, Elasticsearch
    ├── [Product Service :8080]      → PostgreSQL, Kafka
    ├── [Voting Service :8080]       → SQLite, Kafka
    ├── [Comment Service :8080]      → PostgreSQL, Kafka
    ├── [Notification Service :8080] → SQLite, Kafka
    ├── [Analytics Service :8080]    → SQLite, Kafka
    └── [AI Service :8080]           → SQLite, Gemini API
```

### Bileşen Tablosu

| Bileşen | Sorumluluk | Teknoloji |
|---|---|---|
| Auth | Kimlik, profil, roller, takip, rozetler | .NET 8, PostgreSQL, Redis, Kafka |
| Product | Ürün kataloğu, topic, launch akışı, koleksiyonlar | .NET 8, PostgreSQL, Kafka |
| Voting | Oyların tek yazma otoritesi (authoritative write) | .NET 8, SQLite, Kafka |
| Comment | Yorum, cevap, düzenleme, soft-delete | .NET 8, PostgreSQL, Kafka |
| Notification | Bildirim inbox'ı, teslim kanalları | .NET 8, SQLite, Kafka |
| Analytics | Event ingestion, aggregate/read model | .NET 8, SQLite, Kafka |
| AI | Analiz, etiketleme, öneri (Gemini) | .NET 8, SQLite |
| Gateway | Edge routing, JWT, rate limiting, proxy | YARP, .NET 8, Redis |
| Web | Kullanıcı arayüzü | Next.js 16, React 19, TypeScript |

---

## 2. Design Patterns

### ✅ Uygulanmış Pattern'ler

#### Creational (Oluşturucu)

| Pattern | Nerede | Açıklama |
|---|---|---|
| **Factory** | `JwtProvider`, servis kayıt dosyaları | Servisleri soyutlayarak bağımlılıkları üretir |
| **Builder** | YARP route/cluster konfigürasyonu, FluentValidation | Karmaşık nesneleri adım adım inşa eder |
| **Singleton** | `IJwtTokenBlacklist` (Redis), `IEventCatalog` | Uygulama ömrü boyunca tek instance |

#### Structural (Yapısal)

| Pattern | Nerede | Açıklama |
|---|---|---|
| **Facade** | `IAuditLogger`, `IVitrinHealthChecks` | Karmaşık alt sistemleri tek arayüzle örtmek |
| **Proxy** | YARP API Gateway | Gelen istekleri iç servislere iletir, araya girer |
| **Adapter** | Kafka producer wrapper, Outbox dispatcher | Farklı arayüzleri birbirine bağlar |
| **Decorator** | Middleware pipeline (auth, rate limit, tracing) | İşleyicilere davranış ekler |

#### Behavioral (Davranışsal)

| Pattern | Nerede | Açıklama |
|---|---|---|
| **Observer** | Kafka event bus, domain events | Üreticiden tükericilere asenkron bildirim |
| **Command** | MediatR CQRS command'ları | İşlemi nesne olarak kapsüller |
| **Strategy** | Rate limiter policy'leri (`auth-login`, `social-write`, `ai-analysis`) | Algoritmaları değiştirilebilir yapar |
| **Repository** | EF Core repository pattern | Veri erişimini soyutlar |

#### Mimari Pattern'ler

| Pattern | Nerede | Açıklama |
|---|---|---|
| **CQRS** | Tüm servisler — Command/Query ayrımı | Okuma ve yazma modellerini ayırır |
| **Outbox Pattern** | Auth, Product, Voting, Comment | DB + event kaydını aynı transaction'da güvence altına alır |
| **Inbox Pattern** | Product, Analytics, Notification | Event tüketiminde idempotency sağlar |
| **Saga** | Voting → Product read model akışı | Dağıtık transaction koordinasyonu |
| **Domain-Driven Design** | Tüm servisler | Bounded context, aggregate, value object |

---

## 3. API Gateway

Gateway, `YARP (Yet Another Reverse Proxy)` tabanlıdır ve tek dış giriş noktasıdır.

### ✅ Uygulanan API Gateway Use Cases

| # | Use Case | Durum | Detay |
|---|---|---|---|
| 1 | **Rate Limiting** | ✅ Var | Redis sliding window (distributed), 9 policy, `Retry-After` header |
| 2 | **Authentication** | ✅ Var | JWT imzası, issuer, audience, algorithm kontrolü |
| 3 | **Load Balancing** | ❌ Tek instance | Birden fazla destination tanımlanabilir ama aktif değil |
| 4 | **Request Routing** | ✅ Var | Path-based routing, her servis için ayrı cluster |
| 5 | **SSL Termination** | ✅ Hazır | Nginx'te HTTPS, Gateway iç ağda HTTP |
| 6 | **Caching** | ✅ Var | Redis üzerinden response cache |
| 7 | **Transformation** | ✅ Var | YARP transform middleware |
| 8 | **Circuit Breaking** | ✅ Var | Polly v8 — cluster başına profil (Critical/Voting/Tolerant) |
| 9 | **Logging & Metrics** | ✅ Var | OpenTelemetry, Serilog, Prometheus |
| 10 | **API Versioning** | ✅ Var | Gateway'de YARP path rewrite — `/api/v1/*` → `/api/*`, servisler değişmez |

### Rate Limiter Policy'leri

Redis sliding window algoritması (Lua script, atomik). Gateway yeniden başlasa veya ileride ikinci instance eklense sayaçlar korunur.

```
auth-login          → 5 istek / dakika    — IP bazlı (brute-force koruması)
auth-registration   → 3 istek / 10 dakika — IP bazlı (kayıt spam koruması)
auth-external-login → 10 istek / dakika   — IP bazlı (OAuth koruması)
api-write           → 60 istek / dakika   — User/IP bazlı (yazma limiti)
social-write        → 30 istek / dakika   — User/IP bazlı (oy, yorum, topic)
search-query        → 90 istek / dakika   — User/IP bazlı (arama)
analytics-event     → 30 istek / dakika   — User/IP bazlı
analytics-query     → 45 istek / dakika   — User/IP bazlı
ai-analysis         → 5 istek / dakika    — User/IP bazlı + günlük SQLite kotası
```

**Fallback:** Redis erişilemezse fail-open (istek geçer, log yazılır) + in-memory FixedWindow devreye girer.

### Circuit Breaker Profilleri

```
Critical (auth, product, comment) → 8s timeout | 3 retry | CB: %50 hata / 30s break
Voting                            → 5s timeout | 1 retry | CB: %60 hata / 20s break
Tolerant (analytics, notif, ai)   → 15s timeout | 2 retry | CB: %70 hata / 60s break
```

Pipeline sırası (içten dışa): `Timeout → Retry → Circuit Breaker`

### Token Blacklist

Logout olan kullanıcının JWT token'ı Redis'te `jti` bazlı kara listeye eklenir. Token süresi dolana kadar TTL ile tutulur, expire olunca otomatik silinir.

---

## 4. API Kavramları

### ✅ Uygulanan 12 API Kavramı

| Kavram | Durum | Uygulama Detayı |
|---|---|---|
| **REST** | ✅ | Tüm servisler REST; kaynak odaklı URL'ler, doğru HTTP fiilleri |
| **Idempotency** | ✅ | Kafka Inbox'ta `EventId` bazlı — aynı event tekrar işlenmez |
| **Pagination** | ✅ | Cursor-based pagination (offset değil, sonraki sayfa token'ı) |
| **Rate Limiting** | ✅ | Gateway'de Redis sliding window, 9 policy, distributed, `Retry-After` header |
| **Versioning** | 🔄 | Altyapı hazır; `v1` path prefix planlandı |
| **Auth** | ✅ | Bearer token (JWT), 401 = token yok/geçersiz, 403 = yetki yok |
| **Retries** | ✅ | Kafka consumer: exponential backoff retry, son deneyde DLQ |
| **Timeouts** | ✅ | YARP proxy timeout, Kafka consumer timeout |
| **Status Codes** | ✅ | RFC 7807 ProblemDetails; 400/401/403/404/409/429 tutarlı kullanımı |
| **GraphQL** | ❌ | Yok — REST tercih edildi |
| **gRPC** | ❌ | Yok — servisler arası HTTP kullanılıyor |
| **Webhooks** | ❌ | Yok — Kafka event'leri iç iletişimi karşılıyor |

### Hata Formatı (RFC 7807 ProblemDetails)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation failed",
  "status": 400,
  "detail": "Product name is required",
  "traceId": "00-abc123...-01"
}
```

`traceId` alanı Jaeger'daki trace'e doğrudan erişim sağlar.

---

## 5. Mesajlaşma — Batch vs Stream

### ✅ Stream Processing (Gerçek Zamanlı)

Kafka üzerinden event-driven akış ile çalışır. Her domain eventi publish edildiği anda tüketilir.

| Topic | Producer | Consumer | Gecikme |
|---|---|---|---|
| `voting-events` | Voting Outbox | Product (read model), Analytics | ~ms |
| `notification-events` | Auth, Comment Outbox | Notification servisi | ~ms |
| `analytics-events` | Web, arama akışı | Analytics servisi | ~ms |
| `social-events` | Product, Comment Outbox | Analytics, gelecek read model | ~ms |
| `user-events` | Auth yaşam döngüsü | Analytics, gelecek read model | ~ms |

**Kullanım alanları:** Oy sayacı güncelleme, bildirim gönderme, analitik kayıt, kullanıcı kaydı takibi.

### ✅ Batch Processing (Planlı)

| İş | Zamanlama | Açıklama |
|---|---|---|
| PostgreSQL backup | Her gece 02:00 UTC | `pg_dump` ile tüm DB'ler, 7 gün saklanır |
| Outbox dispatcher | Sürekli çalışır | İşlenmemiş Outbox satırlarını Kafka'ya iletir |
| Token cleanup | TTL otomatik | Redis'te expire olan blacklist token'ları silme |

**Eksik:** ETL pipeline, günlük analitik agregasyon job'ı, haftalık digest e-postası (bkz. Sonraki Adımlar).

---

## 6. System Design Temel Kavramları

### ✅ Uygulananlar

| Kavram | Durum | Uygulama Detayı |
|---|---|---|
| **Caching** | ✅ | Redis — token blacklist, response cache, session |
| **Message Queues** | ✅ | Kafka + Zookeeper — 5 topic, at-least-once garantisi |
| **Rate Limiting** | ✅ | Gateway'de Redis sliding window, distributed, restart'ta sayaç korunur |
| **DB Indexing** | ✅ | Composite, partial ve GIN index'ler, `pg_trgm` full-text |
| **Health Checks** | ✅ | `/health` (basit), `/health/detail` (sadece iç ağ) |
| **Distributed Tracing** | ✅ | OpenTelemetry + Jaeger, correlation ID end-to-end |
| **Structured Logging** | ✅ | Serilog → Elasticsearch → Kibana |
| **CAP Theorem** | ✅ | CP tercihi — consistency ve partition tolerance öncelikli |
| **Circuit Breaking** | ✅ | Polly v8 — Critical/Voting/Tolerant profilleri |

### ❌ Eksik Olanlar

| Kavram | Neden Yok | Öncelik |
|---|---|---|
| **Load Balancing** | Tek instance çalışıyor | Yüksek (production öncesi) |
| **CDN** | Frontend static asset'ler sunucudan servis ediliyor | Orta |
| **DB Sharding** | Tek PostgreSQL node | Düşük (şu an gerekmez) |
| **DB Replication** | Master-slave yok | Orta (production öncesi) |
| **Consistent Hashing** | Dağıtık cache routing yok | Düşük |

### Veri Sahibi Sınırları

Her servis kendi verisinin tek sahibidir. Başka servisin tablosuna ya da DbContext'ine doğrudan erişim yasaktır.

```
Auth     → vitrin_auth     (Users, Roles, Follows, Badges, OAuthAccounts)
Product  → vitrin_product  (Products, Topics, Categories, Collections, Launches)
Voting   → voting_db       (Votes) ← tek yazma otoritesi
Comment  → vitrin_comment  (Comments, Replies)
Notification → SQLite      (Notifications)
Analytics    → SQLite      (Events, Aggregates)
AI           → SQLite      (AnalysisResults, QuotaUsage)
```

---

## 7. Observability Stack

Projedeki tam observability yığını:

### Monitoring Bileşenleri

| Araç | Port | Görev |
|---|---|---|
| **Jaeger** | 16686 | Distributed tracing — end-to-end request akışı |
| **Prometheus** | 9091 | Metrics toplama ve time-series DB |
| **Grafana** | 3004 | Dashboard ve görselleştirme |
| **Elasticsearch** | 9200 | Log agregasyonu ve full-text arama |
| **Kibana** | 5601 | Log analizi ve görselleştirme |

### Infrastructure Exporter'lar

| Exporter | Port | Ne İzler |
|---|---|---|
| postgres-exporter | 9187 | PostgreSQL sorgu süresi, bağlantı sayısı |
| redis-exporter | 9121 | Redis bellek, komut istatistikleri |
| kafka-exporter | 9308 | Kafka topic lag, mesaj oranları |

### Özel Business Metrics

```
vitrin_user_registrations_total
vitrin_product_submissions_total
vitrin_votes_total{vote_type="upvote"}
vitrin_comments_total
vitrin_auth_attempts_total{success="true"}
vitrin_request_duration_seconds
vitrin_database_query_duration_seconds
vitrin_cache_hits_total / vitrin_cache_misses_total
vitrin_errors_total{error_type="validation"}
```

### Alerting Eşikleri

| Metrik | Eşik | Aksiyon |
|---|---|---|
| Hata oranı | > %5 (5 dakika) | Alarm |
| p95 yanıt süresi | > 2 saniye | Alarm |
| Cache hit rate | < %80 | Uyarı |
| DB bağlantı havuzu | > %80 | Uyarı |

### Golden Signals (RED Method)

- **Rate**: İstek başına saniye — `rate(vitrin_request_duration_seconds_count[5m])`
- **Errors**: Hata oranı — `rate(vitrin_errors_total[5m])`
- **Duration**: p95 gecikme — `histogram_quantile(0.95, vitrin_request_duration_seconds_bucket)`

---

## 8. Güvenlik Mimarisi

### Kimlik Doğrulama Katmanları

```
1. Nginx           → HTTPS termination, security headers
2. Gateway         → JWT imza + blacklist kontrolü, rate limiting
3. Servis          → Policy-based authorization (AdminOnly, MakerOrAdmin, ResourceOwner)
4. Domain katmanı  → İş kuralı sahiplik kontrolleri
```

### JWT Yönetimi

- Süre: **1 saat** (eskiden 7 gün)
- Her token için benzersiz `jti` claim'i
- Logout sonrası `jti` Redis'e yazılır (TTL = kalan süre)
- Gateway her istekte blacklist kontrolü yapar
- Caller kimliği **her zaman** doğrulanmış `ClaimsPrincipal`'dan alınır, body'den değil

### Güvenlik Başlıkları (Nginx)

```
Strict-Transport-Security   → HSTS, 1 yıl
X-Content-Type-Options      → MIME sniffing engeli
X-Frame-Options: DENY       → Clickjacking engeli
Content-Security-Policy     → XSS, injection
Referrer-Policy             → Bilgi sızması engeli
Permissions-Policy          → Kamera/mikrofon/konum kapalı
```

### Korunan Endpoint'ler

| Kategori | Korunum |
|---|---|
| `/metrics` | Nginx'te 403 — iç ağdan Prometheus erişir |
| `/health/detail` | Nginx'te 403 — sadece iç ağ |
| `/api/admin/*` | `AdminOnly` policy |
| Tüm yazma endpoint'leri | `RequireAuthorization` + sahiplik kontrolü |
| AI analiz | Rate limit + günlük kota (SQLite UPSERT, atomik) |

### Prompt Injection Koruması

AI servisi kullanıcı girdisini Gemini'ye göndermeden önce:
- Maksimum uzunluk kısıtlaması (isim: 200, açıklama: 2000 karakter)
- Kontrol karakterleri temizleme
- `SanitizeForPrompt()` metoduyla sanitize etme

---

## 9. Veri Katmanı

### Veritabanı Seçimleri ve Gerekçeleri

| Servis | DB | Gerekçe |
|---|---|---|
| Auth | PostgreSQL | İlişkisel veri, ACID, full-text gereksinimi |
| Product | PostgreSQL | Karmaşık sorgu, indeks, full-text search |
| Voting | SQLite | Yüksek yazma hızı, basit şema, single-writer |
| Comment | PostgreSQL | İlişkisel (parent-child), soft delete |
| Notification, Analytics, AI | SQLite | Hafif, portabl, single-instance yeterli |

### Uygulanmış Index'ler

```sql
-- Oy unique kısıtı
CREATE UNIQUE INDEX idx_votes_product_user ON ProductUpvotes(ProductItemId, UserId);

-- Ürün listeleme (status + tarih)
CREATE INDEX idx_products_status_published ON Products(Status, PublishedAt);

-- Yorum listeleme
CREATE INDEX idx_comments_product_date ON Comments(ProductId, CreatedAt);
CREATE INDEX idx_comments_parent ON Comments(ParentCommentId);

-- Bildirim inbox
CREATE INDEX idx_notifications_user_unread ON Notifications(UserId, IsRead, CreatedAt);

-- Full-text search (pg_trgm)
CREATE INDEX idx_products_name_trgm ON Products USING gin(Name gin_trgm_ops);
```

### Sorgu Optimizasyonları

- `AsNoTracking()` tüm read-only sorguları için
- Projection DTO'ları (sadece gerekli kolonlar)
- Cursor-based pagination (offset yerine)
- PostgreSQL `pg_trgm` full-text search
- Slug üretiminde race condition: `FOR UPDATE` kilitleme

### Outbox/Inbox Pattern

```
[Servis DB Transaction]
    ├── İş verisi yaz (Products, Votes, Comments...)
    └── OutboxMessage yaz ← AYNI transaction

[Outbox Dispatcher] (background)
    └── Kafka'ya publish et → ProcessedAtUtc güncelle

[Consumer]
    ├── InboxMessage yaz (EventId = primary key)
    └── İş sonucu yaz ← AYNI transaction (idempotent)
```

**Garanti:** DB commit başarılıysa event kaybolmaz. Kafka failure durumunda dispatcher tekrar dener.

---

## 10. Test Mimarisi

### Test Piramidi

| Katman | Araç | Risk | Sayı |
|---|---|---|---|
| **Unit** | xUnit, Vitest, RTL | Domain kuralları, handler davranışı | 113 backend + 11 frontend |
| **Integration** | Testcontainers (PG/Kafka/Redis) | Migration, constraint, cache, retry | 19 |
| **API/Security** | WebApplicationFactory | JWT, IDOR, ProblemDetails | — |
| **Contract** | OpenAPI baseline, event-topic | Breaking change tespiti | — |
| **E2E** | Playwright, axe-core | Gerçek tarayıcı smoke akışı | — |
| **Load** | k6 | p95 gecikme, hata oranı | — |

### Coverage Eşikleri

| Kapsam | Eşik | Mevcut |
|---|---|---|
| Backend satır | %35 minimum | %38.03 |
| Frontend kritik modüller | %70 satır/function | %89.06 |

### k6 Load Test Sonuçları (15 Temmuz 2026)

```
Profil    : 5 VU, 10 saniye (sıcak koşu)
İstekler  : 129
Hata oranı: %0
p95       : 371.92 ms
```

---

## 11. Event-Driven Mimari

### Event Catalog

| Event | Topic | Producer | Consumer |
|---|---|---|---|
| `voting.vote_added` | `voting-events` | Voting | Product (read model), Analytics |
| `voting.vote_removed` | `voting-events` | Voting | Product (read model), Analytics |
| `notification.send` | `notification-events` | Auth, Comment | Notification |
| `analytics.product_viewed` | `analytics-events` | Web/API | Analytics |
| `analytics.search_performed` | `analytics-events` | Arama akışı | Analytics |
| `analytics.comment_created` | `analytics-events` | Comment akışı | Analytics |
| `product.published` | `social-events` | Product | Analytics |
| `comment.added` | `social-events` | Comment | Gelecek social read model |
| `user.registered` | `user-events` | Auth | Analytics |
| `user.role_changed` | `user-events` | Auth | Gelecek user read model |

### Event Envelope Standardı

Her event şu alanları taşır:

```json
{
  "eventId": "uuid",
  "eventType": "voting.vote_added",
  "timestamp": "2026-08-24T10:00:00Z",
  "version": "1.0",
  "correlationId": "request-uuid",
  "causationId": "parent-event-uuid",
  "payload": { ... }
}
```

### Teslimat Garantisi

- **at-least-once** — mesaj en az bir kez teslim edilir
- Producer idempotence açık
- Consumer offset: işleme bittikten sonra commit
- Bounded retry + exponential backoff
- Son başarısız deneme → `<topic>.dlq` topic'ine yazar

### Uyumluluk Kuralları

- Aynı major sürümde yalnızca opsiyonel alan eklenebilir
- Alan silme veya tip değiştirme = breaking change → yeni major
- Event type adı yayınlandıktan sonra yeniden kullanılmaz

---

## 12. Eksikler ve Sonraki Adımlar

> **Karar kriteri:** Aşağıdaki maddeler "yapılabilir" değil "şu an yapılmaya değer" filtresinden geçirilmiştir.
> Tek EC2 t3.medium üzerinde 13 container çalıştırıldığı göz önünde bulundurularak
> maliyet ve karmaşıklık / fayda dengesi dikkate alınmıştır.

---

### ~~Öncelik 1 — Resilience~~ ✅ Tamamlandı

| Özellik | Pattern | Araç | Durum |
|---|---|---|---|
| **Circuit Breaking** | Circuit Breaker | Polly v8 | ✅ Eklendi — `ResilienceForwarderHttpClientFactory` |
| **Retry with backoff** | Retry Policy | Polly v8 | ✅ Eklendi — exponential + jitter |
| **Timeout** | Timeout Policy | Polly v8 | ✅ Eklendi — cluster başına farklı süre |
| **Distributed Rate Limiting** | Redis Sliding Window | StackExchange.Redis + Lua | ✅ Eklendi — `RedisRateLimitStore` |

**Commit'ler:**
- `f75bfb2` — feat: resilience layer - Circuit Breaker, Retry, Timeout via Polly
- `1c47188` — feat: distributed rate limiting via Redis sliding window

### ~~Öncelik 2 — Developer Experience~~ ✅ Tamamlandı

| Özellik | Araç | Durum |
|---|---|---|
| **API Versioning** | YARP `PathRemovePrefix` transform | ✅ Eklendi — `/api/v1/*` → `/api/*`, 38 v1 route |
| **OpenAPI / Swagger geliştirme** | Swashbuckle, Redoc | 🔄 Planlandı |
| **TypeScript SDK üretimi** | OpenAPI Generator | 🔄 Planlandı |

**Nasıl çalışır:**
```
İstemci:  GET /api/v1/products
Gateway:  PathRemovePrefix: /v1  →  /api/products
Servis:   /api/products'ı dinler, değişmez
```
Geriye uyumlu — mevcut `/api/` path'leri çalışmaya devam eder.

**Commit:** `a1333ee` — feat: API versioning - /api/v1/* prefix via YARP path rewrite

### Öncelik 3 — Batch Processing ✅ Düşük maliyet, orta etki

Mevcut Kafka akışı stream processing yapıyor; analitik agregasyon ve temizlik job'ları eksik.

| İş | Araç | Zamanlama |
|---|---|---|
| Günlük analitik agregasyonu | Hangfire | Gece 03:00 |
| Haftalık digest e-postası | Hangfire + Resend | Pazartesi 09:00 |
| Inbox / Outbox satır temizleme | Hangfire | Haftada bir |

### Öncelik 4 — Production Altyapısı ✅ Orta maliyet, zorunlu

Bunlar mevcut tek-sunucu kurulumuna uygun, ölçek gerektirmeyen production gereksinimleri.

| Özellik | Araç | Not |
|---|---|---|
| **CDN + DDoS koruması** | Cloudflare (ücretsiz katman) | DNS önüne eklemek 10 dakika; SSL + CDN + DDoS ücretsiz |
| **Secret Manager** | AWS Secrets Manager / Vault | `.env` dosyası production'da riskli |
| **IaC** | Terraform | Altyapı tekrarlanabilirliği için |
| **Container Scan** | Trivy (CI'da) | CVE tespiti; CI pipeline'a eklenir |
| **SBOM** | Syft | Artifact üretimi |

---

### Şu An İçin Erken / Gereksiz

Aşağıdaki kavramlar mimari olarak bilinmesi gereken şeyler, ama mevcut kurulumda
uygulanması ya imkânsız ya anlamsız ya da aşırı karmaşıklık getirir.

| Özellik | Neden Şimdi Değil |
|---|---|
| **Load Balancer + multiple instance** | Tek EC2 üzerinde replica koymanın faydası yok — aynı donanımı paylaşırlar. Gerçek trafik artışında yatay ölçek için geçerli. |
| **DB Replication (master-slave)** | Tek node kurulumunda operasyonel karmaşıklık yaratır, okuma trafiği buna değmez. |
| **Redis Cluster / Sentinel** | Tek instance Redis zaten %99.9 sağlıyor; cluster, ölçek gerektiğinde yapılır. |
| **DB Sharding** | Şu anki veri hacmiyle PostgreSQL'in limitlerine ulaşmak yıllar sürer. |
| **Service Mesh (Istio/Linkerd)** | Kubernetes bile yok. Onlarca servisli büyük organizasyonlar için tasarlanmış; 8 servisli projede over-engineering. |
| **Feature Flags (LaunchDarkly/Unleash)** | A/B testing veya gradual rollout ihtiyacı yok. Gerekirse basit DB tabanlı flag yeter. |

---

## Referans Belgeler

| Belge | Konum |
|---|---|
| ADR-0001: Mikroservis mimarisini koruma | `docs/adr/0001-mikroservis-mimarisini-koruma.md` |
| ADR-0002: Merkezi JWT ve kimlik sınırı | `docs/adr/0002-merkezi-jwt-ve-kimlik-siniri.md` |
| ADR-0003: API koruma katmanları | `docs/adr/0003-api-koruma-katmanlari.md` |
| ADR-0004: Event teslimatı, Outbox/Inbox | `docs/adr/0004-event-teslimati-outbox-inbox.md` |
| ADR-0005: Migration deployment job | `docs/adr/0005-migration-deployment-job.md` |
| Event Catalog | `docs/event-catalog.md` |
| Test Stratejisi | `docs/testing-strategy.md` |
| Güvenlik Kontrol Listesi | `docs/SECURITY-CHECKLIST.md` |
| Veri Erişimi ve Index Rehberi | `docs/data-access-performance.md` |
| Observability Stack | `observability/README.md` |
| Geliştirme Yol Haritası | `docs/PROJE-GELISTIRME-YOL-HARITASI.md` |
