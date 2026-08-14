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

## Observability & Monitoring

Vitrin, production-ready monitoring ve observability özellikleri ile gelir:

### Monitoring Stack

| Bileşen | Port | Açıklama |
|---------|------|----------|
| **Jaeger** | 16686 | Distributed tracing ve request flow analizi |
| **Prometheus** | 9091 | Metrics collection ve time series database |
| **Grafana** | 3004 | Dashboards ve görselleştirme |
| **Elasticsearch** | 9200 | Log aggregation ve full-text search |
| **Kibana** | 5601 | Log analysis ve visualization |

### Infrastructure Exporters

| Exporter | Port | Monitörlediği Servis |
|----------|------|---------------------|
| **Postgres Exporter** | 9187 | PostgreSQL veritabanı metrikleri |
| **Redis Exporter** | 9121 | Redis cache performansı |
| **Kafka Exporter** | 9308 | Kafka message broker metrikleri |

### Observability Özellikleri

- **📊 Custom Business Metrics**: User registrations, product submissions, voting activity, authentication success rates
- **🔗 Distributed Tracing**: OpenTelemetry ile end-to-end request tracking
- **📝 Structured Logging**: Serilog ile JSON formatında logs (Console, File, Elasticsearch)
- **🏥 Health Checks**: Database, Redis, Kafka connectivity monitoring
- **🔄 Correlation IDs**: Request'ler arası correlation tracking
- **⚡ Performance Monitoring**: Request/response times, database query durations
- **🚨 Error Tracking**: Detailed error metrics ve categorization

### Quick Start

```powershell
# Observability stack'i de dahil tüm servisleri başlat
docker compose up -d

# Veya sadece observability servislerini başlat
docker compose up -d jaeger prometheus grafana postgres-exporter redis-exporter kafka-exporter

# Observability setup script'i çalıştır
.\scripts\setup-observability.ps1
```

### Access URLs

- **🔍 Jaeger Tracing**: http://localhost:16686
- **📊 Prometheus Metrics**: http://localhost:9091  
- **📈 Grafana Dashboards**: http://localhost:3004
- **📋 Kibana Logs**: http://localhost:5601
- **🔎 Elasticsearch**: http://localhost:9200

### Grafana Credentials

```
Username: admin
Password: [GRAFANA_ADMIN_PASSWORD from .env]
```

### Pre-built Dashboards

1. **System Overview**: Request rates, response times, error rates, active users, cache hit rates
2. **Business Metrics**: User registrations, product submissions, voting activity, authentication metrics
3. **Infrastructure**: Database performance, Redis metrics, Kafka throughput

### Custom Metrics Examples

```prometheus
# Business Metrics
vitrin_user_registrations_total
vitrin_product_submissions_total  
vitrin_votes_total{vote_type="upvote"}
vitrin_comments_total
vitrin_auth_attempts_total{success="true"}

# Performance Metrics
vitrin_request_duration_seconds
vitrin_database_query_duration_seconds
vitrin_cache_operation_duration_seconds

# System Metrics
vitrin_active_users
vitrin_cache_hits_total / vitrin_cache_misses_total
vitrin_errors_total{error_type="validation"}
```

### Distributed Tracing Flow

1. **Gateway** → JWT validation, routing
2. **Auth Service** → User authentication, authorization
3. **Business Services** → Product, Voting, Comment operations
4. **Event Bus** → Kafka async messaging
5. **Database** → PostgreSQL/SQLite queries

### Log Analysis

Logs Elasticsearch'te şu format ile indexlenir:

```json
{
  "@timestamp": "2024-08-14T10:30:00.000Z",
  "level": "Information",
  "messageTemplate": "Business operation: {Operation} executed by {UserId}",
  "properties": {
    "ServiceName": "Auth",
    "Operation": "UserRegistration", 
    "UserId": "user123",
    "CorrelationId": "abc123",
    "TraceId": "def456",
    "RequestPath": "/api/auth/register",
    "StatusCode": 200,
    "Duration": 1250
  }
}
```

### Monitoring Best Practices

- **Golden Signals**: Latency, Traffic, Errors, Saturation tracking
- **Business KPIs**: Custom metrics for product success measurement
- **Correlation**: Logs, metrics ve traces arasında correlation ID kullanımı
- **Sampling**: High traffic'te trace sampling configuration
- **Alerts**: Critical metrics için Grafana alerting rules

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

### Gerekli Environment Variables

```bash
# Core Services
POSTGRES_PASSWORD=your_strong_password
JWT_SECRET=your_jwt_secret_at_least_32_chars
NEXTAUTH_SECRET=your_nextauth_secret_at_least_32_chars

# Observability
GRAFANA_ADMIN_PASSWORD=your_grafana_password
LOG_LEVEL=Information

# Optional Services  
GOOGLE_CLIENT_ID=your_google_oauth_client_id
RESEND_API_KEY=your_resend_api_key
GEMINI_API_KEY=your_gemini_api_key
```

İmajları oluşturun, her servisin migration job'ını tek sefer çalıştırın ve sistemi başlatın:

```powershell
docker compose build
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-migrations.ps1
docker compose up -d
docker compose ps
```

Uygulama process'leri normal startup sırasında schema değiştirmez. Yeni bir sürüm migration içeriyorsa rollout öncesinde aynı migration job'ı tekrar çalıştırılır.

- Web: `http://localhost:3003`
- Gateway / API: `http://localhost:5000`
- Gateway health: `http://localhost:5000/health`

### Monitoring URLs

- Jaeger Tracing: `http://localhost:16686`
- Prometheus Metrics: `http://localhost:9091`
- Grafana Dashboards: `http://localhost:3004`

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
observability/             Monitoring stack konfigürasyonları (Prometheus, Grafana, vb.)
  ├── grafana/             Grafana dashboards ve provisioning
  ├── prometheus/          Prometheus configuration
  └── README.md            Detaylı observability rehberi
docker-compose.yml         Yerel mikroservis orkestrasyonu + monitoring stack
```

## Dokümantasyon

### Architecture & Design
- [Kapsamlı proje incelemesi ve geliştirme yol haritası](docs/PROJE-GELISTIRME-YOL-HARITASI.md)
- [ADR-0001 — Mikroservis mimarisini koruma kararı](docs/adr/0001-mikroservis-mimarisini-koruma.md)
- [ADR-0002 — Merkezi JWT ve güvenilir kimlik sınırı](docs/adr/0002-merkezi-jwt-ve-kimlik-siniri.md)
- [ADR-0003 — API hata, kota, rate limit ve audit katmanları](docs/adr/0003-api-koruma-katmanlari.md)
- [ADR-0004 — Güvenilir event teslimatı, Outbox ve Inbox](docs/adr/0004-event-teslimati-outbox-inbox.md)
- [ADR-0005 — Migration deployment job](docs/adr/0005-migration-deployment-job.md)

### Operations & Monitoring
- [📊 Observability Stack Guide](observability/README.md) - Comprehensive monitoring setup
- [Event Catalog — topic, producer ve consumer matrisi](docs/event-catalog.md)
- [Veri erişimi, indeks ve EXPLAIN ANALYZE rehberi](docs/data-access-performance.md)

### Quality & Testing
- [Test stratejisi ve kalite kapıları](docs/testing-strategy.md)

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

**Aşama 4 - Observability (✅ Tamamlandı)**

Aşama 0 stabilizasyonu, Aşama 1 güvenlik/doğruluk çalışmaları, Aşama 2 veri/event mimarisi ve Aşama 3 test mimarisi tamamlanmıştır. Aşama 4'te comprehensive observability ve monitoring sistemi eklendi.

### Tamamlanan Observability Özellikleri:

- ✅ **Distributed Tracing** (OpenTelemetry + Jaeger)
- ✅ **Metrics Collection** (Prometheus + custom business metrics)
- ✅ **Structured Logging** (Serilog + Elasticsearch)  
- ✅ **Monitoring Dashboards** (Grafana + pre-built dashboards)
- ✅ **Health Checks** (Database, Redis, Kafka connectivity)
- ✅ **Request Correlation** (End-to-end tracking)
- ✅ **Infrastructure Monitoring** (PostgreSQL, Redis, Kafka exporters)
- ✅ **Performance Monitoring** (Response times, query durations)
- ✅ **Error Tracking** (Categorized error metrics)

### Backend Güvenlik & Kalite:

OAuth sağlayıcı doğrulaması, merkezi JWT/policy katmanı, güvenilir caller identity sınırı, sahiplik kontrolleri, Gateway rate limiting, merkezi ProblemDetails, Transactional Outbox/Inbox, Kafka DLQ ve event schema versioning uygulanmıştır. 

### Test Coverage:

Test katmanı; unit, Testcontainers, WebApplicationFactory güvenlik/IDOR, OpenAPI compatibility, Vitest/RTL, Playwright/axe senaryoları, k6 ve ölçülen coverage eşiklerini kapsar.

**Sonraki Aşama**: Production deployment ve infrastructure automation (Aşama 5).

## Troubleshooting & Maintenance

### Common Issues

#### Services Not Starting
```powershell
# Container durumunu kontrol et
docker compose ps

# Belirli bir servisin loglarını kontrol et
docker compose logs vitrin-auth

# Servisleri yeniden başlat
docker compose restart vitrin-auth
```

#### Observability Services
```powershell
# Prometheus metrics endpoint'leri test et
curl http://localhost:5000/metrics

# Jaeger health check
curl http://localhost:16686/api/v1/health

# Grafana health
curl http://localhost:3004/api/health
```

#### Database Issues
```powershell
# PostgreSQL connection test
docker compose exec postgres pg_isready -U postgres

# Database migration çalıştır
.\scripts\run-migrations.ps1

# Database logs
docker compose logs postgres
```

### Performance Monitoring

#### Key Metrics to Monitor

- **Request Rate**: `rate(vitrin_request_duration_seconds_count[5m])`
- **Error Rate**: `rate(vitrin_errors_total[5m])`  
- **Response Time P95**: `histogram_quantile(0.95, vitrin_request_duration_seconds_bucket)`
- **Cache Hit Rate**: `vitrin_cache_hits_total / (vitrin_cache_hits_total + vitrin_cache_misses_total)`

#### Alerting Thresholds

- Error rate > 5% for 5 minutes
- Response time P95 > 2 seconds  
- Cache hit rate < 80%
- Database connection pool > 80% usage

### Maintenance Commands

```powershell
# Cleanup unused Docker resources
docker system prune

# Backup Grafana dashboards
docker compose exec grafana grafana-cli admin export-dashboard

# Rotate logs (automatic with Serilog file sink)
# Check logs/ directory for automatic rotation

# Update observability stack
docker compose pull jaeger prometheus grafana
docker compose up -d jaeger prometheus grafana
```
