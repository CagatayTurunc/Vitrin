# Vitrin

Vitrin; geliştiricilerin ve üreticilerin ürünlerini yayınladığı, topluluğun ürünleri keşfettiği, oyladığı, yorumladığı ve koleksiyonlara eklediği **mikroservis tabanlı ürün keşif platformudur**.

Bu repo yalnızca çalışan bir uygulama değil; servis sınırları, veri sahipliği, olay tabanlı iletişim, kalite kapıları, konteynerleştirme ve gözlemlenebilirlik konularını birlikte gösterecek bir **portföy projesi** olarak geliştirilmektedir.

---

## Mimari

```
İnternet → Nginx (HTTPS, security headers)
         → Next.js Web UI :3003
         → YARP API Gateway :5000
              ├── Auth Service      → PostgreSQL, Redis, Kafka
              ├── Product Service   → PostgreSQL, Kafka
              ├── Voting Service    → SQLite, Kafka
              ├── Comment Service   → PostgreSQL, Kafka
              ├── Notification      → SQLite, Kafka, SSE
              ├── Analytics Service → SQLite, Kafka
              └── AI Service        → SQLite, Gemini
```

| Bileşen | Sorumluluk | Teknoloji |
|---|---|---|
| Auth | Kimlik, profil, roller, takip, rozetler, KVKK | .NET 8, PostgreSQL, Redis, Kafka |
| Product | Ürün kataloğu, topic, launch akışı, koleksiyonlar | .NET 8, PostgreSQL, Kafka |
| Voting | Oyların tek yazma otoritesi (authoritative write) | .NET 8, SQLite, Kafka |
| Comment | Yorum, cevap, tepkiler, moderasyon | .NET 8, PostgreSQL, Kafka |
| Notification | Bildirim inbox'ı, SSE stream, tercihler | .NET 8, SQLite, Kafka |
| Analytics | Event ingestion, aggregate/read model, maker dashboard | .NET 8, SQLite, Kafka |
| AI | Analiz, etiketleme, öneri (Gemini) | .NET 8, SQLite |
| Gateway | Edge routing, JWT, rate limiting, circuit breaker | YARP, .NET 8, Redis |
| Web | Kullanıcı ve yönetim arayüzü | Next.js 16, React 19, TypeScript |

---

## Cloud & Deployment Architecture

### Infrastructure Strategy: Cost-Optimized Hybrid Cloud

Vitrin, **selective PaaS** yaklaşımı ile maliyet-performans dengesini optimize eder: Mission-critical altyapı bileşenleri self-hosted (kontrol ve maliyet avantajı), commodity servisler ise yönetilen PaaS çözümleriyle sağlanır.

#### Deployment Stack

| Katman | Teknoloji | Hosting Model | Maliyet |
|---|---|---|---|
| **Compute** | AWS EC2 (t3.medium) | IaaS | ~$30/ay |
| **Container Orchestration** | Docker Compose | Self-managed | $0 |
| **Container Registry** | GitHub Container Registry (GHCR) | PaaS (Free) | $0 |
| **CI/CD** | GitHub Actions | PaaS (Free) | $0 |
| **Reverse Proxy** | Nginx | Self-hosted | $0 |
| **API Gateway** | YARP (.NET) | Self-hosted | $0 |
| **Database** | PostgreSQL (Docker) | Self-hosted | $0 |
| **Cache** | Redis (Docker) | Self-hosted | $0 |
| **Message Broker** | Kafka + Zookeeper (Docker) | Self-hosted | $0 |
| **Observability** | Prometheus + Grafana + Jaeger | Self-hosted | $0 |
| **Log Storage** | Elasticsearch + Kibana (Docker) | Self-hosted | $0 |
| **CDN & DDoS** | Cloudflare Free Tier | PaaS (Free) | $0 |
| **Email Delivery** | Resend Free Tier | PaaS (Free) | $0 |
| **Image CDN** | Cloudinary Free Tier | PaaS (Free) | $0 |
| **AI/LLM** | Google Gemini API | PaaS (Free) | $0 |
| **OAuth** | Google/GitHub OAuth 2.0 | PaaS (Free) | $0 |
| **Analytics** | Vercel Analytics | PaaS (Free) | $0 |
| **Total** | | | **~$30/ay** |

#### Multi-Layer Security Architecture

```
[Internet]
    ↓
[Cloudflare]  ← Layer 1: DDoS protection, bot filtering, CDN, SSL/TLS
    ↓
[AWS EC2 / Nginx]  ← Layer 2: HTTPS termination, security headers, endpoint protection
    ↓
[YARP Gateway]  ← Layer 3: JWT validation, distributed rate limiting, circuit breaker
    ↓
[Microservices]  ← Layer 4: Domain-specific authorization, data validation
```

#### Why Self-Host Core Infrastructure?

**Strategic Decision Rationale:**

| Bileşen | Self-Host Nedeni |
|---|---|
| **PostgreSQL** | Veri sahipliği, performance tuning kontrolü, migration esnekliği, RDS maliyetinden kaçınma (~$30-100/ay) |
| **Redis** | Cache pattern özgürlüğü, rate limiting store kontrolü, ElastiCache maliyetinden kaçınma (~$15-50/ay) |
| **Kafka** | Event sourcing tam kontrolü, MSK/Confluent Cloud maliyetinden kaçınma (~$200+/ay) |
| **Observability Stack** | Unlimited metrics/logs, custom dashboards, Datadog/New Relic maliyetinden kaçınma (~$50-200/ay) |

**Toplam Tasarruf:** ~$300-500/ay (managed servislere göre)

#### Why Use Selective PaaS?

**Strategic PaaS Adoption:**

| PaaS Servisi | Kullanım Nedeni |
|---|---|
| **Resend (Email)** | SMTP complexity, deliverability optimization, SPF/DKIM/DMARC setup, IP reputation management |
| **Cloudinary (Images)** | Automatic image optimization (WebP/AVIF), responsive srcset, CDN distribution, transformation API |
| **Cloudflare (CDN/DDoS)** | Global edge network, unmetered DDoS protection, free SSL, Web Application Firewall |
| **GitHub Actions (CI/CD)** | Zero maintenance, built-in secrets management, matrix builds, extensive ecosystem |
| **Gemini AI (LLM)** | No model training/hosting, auto-scaling, API simplicity, free tier sufficient |

#### CI/CD Pipeline Architecture

```yaml
Trigger: git push [deploy]
    ↓
1. Test Stage
   ├─ Backend: xUnit (113 tests)
   ├─ Frontend: Vitest (11 tests)
   └─ Integration: Testcontainers
    ↓
2. Security Scan
   ├─ .NET: dotnet list package --vulnerable
   ├─ Frontend: pnpm audit
   └─ Filesystem: Trivy scan
    ↓
3. Build & Push (Matrix: 9 services)
   ├─ Multi-stage Dockerfile (layer optimization)
   ├─ BuildKit cache (GitHub Actions cache)
   └─ Push to ghcr.io (2 tags: commit-sha + latest)
    ↓
4. Image Security
   ├─ Trivy container scan (CRITICAL/HIGH)
   ├─ SBOM generation (Syft CycloneDX)
   └─ SARIF upload to GitHub Security
    ↓
5. Deploy
   ├─ SSH to EC2
   ├─ Pull images (throttled, sequential)
   ├─ Rolling restart (service-by-service)
   └─ Health check (12 retries, 5s interval)
    ↓
6. Smoke Test
   ├─ Playwright production tests
   └─ Accessibility checks (axe-core)
    ↓
7. Auto Rollback (on failure)
   └─ Revert to previous image tags
```

#### Backup & Disaster Recovery

```yaml
Strategy: Automated Daily Backups
Schedule: 02:00 UTC (Cron)
Retention: 7 days
Databases:
  - vitrin_auth (PostgreSQL)
  - vitrin_product (PostgreSQL)
  - vitrin_comment (PostgreSQL)
Storage:
  - Local: /backups (gzip compressed)
  - Remote: AWS S3 (optional, ready to enable)
Recovery:
  - scripts/restore-postgres.sh
  - Interactive confirmation (safety)
  - Connection termination + drop-create
```

#### Cost Optimization Techniques

1. **Single EC2 Instance:** Docker Compose multi-container deployment (resource sharing)
2. **Free Tier PaaS:** Cloudflare, Resend, Cloudinary, GHCR, GitHub Actions
3. **Self-Hosted Observability:** Prometheus + Grafana + Jaeger (zero cost)
4. **SQLite for Non-Critical:** Voting, Notification, Analytics (volume storage instead of PostgreSQL)
5. **Throttled Image Pulls:** Sequential deployment (avoid bandwidth spikes)
6. **BuildKit Cache:** GitHub Actions cache (reduce build time + transfer)

#### Scalability Path (Future)

**Current:** Single EC2 + Docker Compose (~$30/ay)

**Next Steps (if needed):**
- Horizontal scaling: Multiple EC2 behind load balancer
- Database: Read replicas, connection pooling (PgBouncer)
- Cache: Redis cluster mode
- Kubernetes: AWS EKS or self-managed (k3s)

**Cost-Performance Trade-offs Considered:**
- Kubernetes adds ~$150/ay (EKS control plane + nodes) but enables auto-scaling
- Managed RDS adds ~$40/ay but provides Multi-AZ + automatic backups
- Current setup handles ~1000 concurrent users (sufficient for MVP/portfolio)

---

## Sistem Tasarımı

Bu projede uygulanan tüm sistem tasarım pattern'leri, API gateway özellikleri, distributed sistem kavramları ve mimari kararlar için:

**[→ docs/SYSTEM-DESIGN.md](docs/SYSTEM-DESIGN.md)**

Öne çıkan özellikler:

- **Resilience** — Circuit Breaker, Retry (exponential backoff), Timeout: Polly v8, cluster başına profil (Critical/Voting/Tolerant)
- **Distributed Rate Limiting** — Redis sliding window (Lua, atomik), 9 policy, restart'ta sayaç korunur
- **API Versioning** — Gateway'de YARP `PathRemovePrefix` transform; `/api/v1/*` → `/api/*`, servisler değişmez
- **Batch Processing** — Günlük analitik agregasyon, haftalık Outbox/Inbox cleanup, KVKK retention worker
- **Event-Driven** — Kafka, Transactional Outbox/Inbox, DLQ, at-least-once, event schema versioning
- **Observability** — OpenTelemetry + Jaeger + Prometheus + Grafana + Elasticsearch + Kibana

---

## API Dokümantasyonu (Swagger)

Her servis geliştirme ortamında JWT Bearer destekli Swagger UI sunar.

| Servis | Swagger URL (local) | Port |
|---|---|---|
| Auth | http://localhost:5104/swagger | 5104 |
| Product | http://localhost:5177/swagger | 5177 |
| Voting | http://localhost:5143/swagger | 5143 |
| Comment | http://localhost:5100/swagger | 5100 |
| Notification | http://localhost:5101/swagger | 5101 |
| Analytics | http://localhost:5102/swagger | 5102 |
| AI | http://localhost:5103/swagger | 5103 |
| Gateway (API) | http://localhost:5000 | 5000 |
| Web UI | http://localhost:3003 | 3003 |

**JWT ile test etmek için:**
1. `/api/auth/login` endpoint'ini çağırın → `token` alın
2. Swagger UI'da sağ üstteki **Authorize** butonuna tıklayın
3. Token değerini girin (prefix gerekmez, otomatik eklenir)

---

## Observability & Monitoring

### Monitoring Stack

| Araç | Port | Görev |
|---|---|---|
| **Jaeger** | 16686 | Distributed tracing, end-to-end request akışı |
| **Prometheus** | 9091 | Metrics toplama, time-series DB |
| **Grafana** | 3004 | Dashboard ve görselleştirme |
| **Elasticsearch** | 9200 | Log agregasyonu, full-text arama |
| **Kibana** | 5601 | Log analizi ve görselleştirme |
| **postgres-exporter** | 9187 | PostgreSQL metrikleri |
| **redis-exporter** | 9121 | Redis cache performansı |
| **kafka-exporter** | 9308 | Kafka topic lag, mesaj oranları |

### Özel Business Metrics

```prometheus
vitrin_user_registrations_total
vitrin_product_submissions_total
vitrin_votes_total{vote_type="upvote"}
vitrin_auth_attempts_total{success="true"}
vitrin_request_duration_seconds
vitrin_cache_hits_total / vitrin_cache_misses_total
vitrin_errors_total{error_type="validation"}
```

---

## Hızlı Başlangıç

**Gereksinimler:** .NET 8 SDK, Node.js 22+, Docker Desktop, PowerShell 7

```powershell
# 1. Ortam dosyasını oluştur, CHANGE_ME değerlerini doldur
Copy-Item .env.example .env

# 2. Image'ları derle
docker compose build

# 3. Migration'ları çalıştır (ilk kurulumda bir kez)
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-migrations.ps1

# 4. Tüm servisleri başlat
docker compose up -d
docker compose ps
```

### Gerekli Environment Variables

```bash
# Zorunlu
POSTGRES_PASSWORD=güçlü_şifre
JWT_SECRET=en_az_32_karakter_jwt_secret
NEXTAUTH_SECRET=en_az_32_karakter_nextauth_secret
GRAFANA_ADMIN_PASSWORD=grafana_şifresi

# Opsiyonel
GOOGLE_CLIENT_ID=google_oauth_id
RESEND_API_KEY=resend_email_api_key
GEMINI_API_KEY=gemini_ai_api_key
```

### Erişim URL'leri

| Servis | URL |
|---|---|
| Web UI | http://localhost:3003 |
| API Gateway | http://localhost:5000 |
| Grafana | http://localhost:3004 |
| Jaeger | http://localhost:16686 |
| Prometheus | http://localhost:9091 |
| Kibana | http://localhost:5601 |

---

## Kalite Kapıları

```powershell
# Tüm testler, lint, typecheck ve build
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1

# Seçenekler
.\scripts\verify.ps1 -SkipInstall    # NuGet/pnpm install atla
.\scripts\verify.ps1 -SkipBuild      # Build adımını atla
.\scripts\verify.ps1 -CheckCompose   # Docker Compose doğrula
```

| Katman | Araç | Durum |
|---|---|---|
| Backend unit | xUnit | ✅ 113 test |
| Backend integration | Testcontainers (PG/Kafka/Redis) | ✅ 19 test |
| Backend coverage | Cobertura | ✅ %38 (eşik: %35) |
| Frontend unit | Vitest + RTL | ✅ 11 test |
| Frontend coverage | V8 | ✅ %89 (kritik modüller) |
| E2E / Accessibility | Playwright + axe | ✅ Smoke testler |
| Load | k6 | ✅ p95: 371ms, %0 hata |

---

## CI/CD Pipeline

```
push to main
    ↓
[check-deploy] commit'te [deploy] var mı?
    ↓ evet              ↓ hayır
[test]              sadece test çalışır
    ↓
[security-scan] .NET + pnpm + Trivy filesystem
    ↓
[build] 9 servis image → ghcr.io
    ↓
[image-scan] Trivy container scan + Syft SBOM
    ↓
[deploy] EC2'ya rolling restart + health check
    ↓
[smoke-test] Playwright production smoke
    ↓ başarısız
[rollback] önceki image'a otomatik rollback
```

**Deploy tetiklemek için:**
```bash
git commit -m "feat: yeni özellik [deploy]"
git push origin main
```

---

## Depo Yapısı

```
src/
  Gateways/              YARP API Gateway (JWT, rate limiting, circuit breaker, versioning)
  Services/              Auth, Product, Voting, Comment, Notification, Analytics, AI
  Shared/                Kernel, event contracts, Outbox/Inbox, rate limiting, Swagger
  Web/Vitrin.Web.UI/     Next.js uygulaması
tests/                   Unit, integration, security/IDOR, contract testleri
scripts/                 verify.ps1, run-migrations.ps1, backup
docs/                    ADR'ler, event catalog, test stratejisi, SYSTEM-DESIGN.md
observability/           Prometheus, Grafana, Jaeger konfigürasyonları
```

---

## Mimari Karar Kayıtları (ADR)

| ADR | Konu |
|---|---|
| [ADR-0001](docs/adr/0001-mikroservis-mimarisini-koruma.md) | Mikroservis mimarisini koruma kararı |
| [ADR-0002](docs/adr/0002-merkezi-jwt-ve-kimlik-siniri.md) | Merkezi JWT ve güvenilir kimlik sınırı |
| [ADR-0003](docs/adr/0003-api-koruma-katmanlari.md) | API hata, kota, rate limit ve audit katmanları |
| [ADR-0004](docs/adr/0004-event-teslimati-outbox-inbox.md) | Güvenilir event teslimatı, Outbox ve Inbox |
| [ADR-0005](docs/adr/0005-migration-deployment-job.md) | Migration deployment job |

---

## Güvenlik Notları

- `.env` commit edilmez; yalnızca `.env.example` şablondur.
- JWT: 1 saatlik token, Redis token blacklist (logout sonrası anlık revoke)
- Gateway'de IP bazlı rate limiting: login (5/dk), register (3/10dk), AI (5/dk)
- OAuth: Google/GitHub token'ları backend'de sağlayıcıya karşı doğrulanır
- Caller kimliği her zaman doğrulanmış `ClaimsPrincipal`'dan alınır, body'den değil
- RFC 7807 ProblemDetails, `traceId` ile tüm hata yanıtlarında
- Güvenlik ve audit olayları yapılandırılmış log olarak Elasticsearch'e yazılır
- Production: `/metrics` ve `/health/detail` endpoint'leri nginx'te kapalı
- Container image'ları Trivy ile CRITICAL/HIGH seviyesinde taranır, SBOM üretilir

---

## Dokümantasyon

| Belge | İçerik |
|---|---|
| [SYSTEM-DESIGN.md](docs/SYSTEM-DESIGN.md) | Tüm sistem tasarım kararları, pattern'ler, mevcut/eksik özellikler |
| **[CLOUD-ARCHITECTURE.md](docs/CLOUD-ARCHITECTURE.md)** | **Cloud & PaaS stratejisi, maliyet analizi, deployment mimarisi** |
| [event-catalog.md](docs/event-catalog.md) | Kafka topic, producer ve consumer matrisi |
| [testing-strategy.md](docs/testing-strategy.md) | Test piramidi ve kalite kapıları |
| [SECURITY-CHECKLIST.md](docs/SECURITY-CHECKLIST.md) | Güvenlik kontrol listesi ve uygulananlar |
| [data-access-performance.md](docs/data-access-performance.md) | Index'ler, EXPLAIN ANALYZE rehberi |
| [PROJE-GELISTIRME-YOL-HARITASI.md](docs/PROJE-GELISTIRME-YOL-HARITASI.md) | Kapsamlı teknik denetim ve yol haritası |
| [observability/README.md](observability/README.md) | Monitoring stack detayları |
| **[27-unsur/](docs/27-unsur/)** | **27 Unsur Kontrol Listesi — Production Launch Checklist** ✅ |

---

## Proje Durumu

**Aşama 5 — Sistem Tasarımı Güçlendirme (✅ Tamamlandı)**

| Özellik | Durum |
|---|---|
| Circuit Breaker + Retry + Timeout (Polly v8) | ✅ |
| Distributed Rate Limiting (Redis sliding window) | ✅ |
| API Versioning (`/api/v1/*` YARP rewrite) | ✅ |
| Batch Processing (analytics aggregation, outbox/inbox cleanup) | ✅ |
| Container Image Scan (Trivy) + SBOM (Syft) | ✅ |
| Swagger JWT auth + API info (tüm servisler) | ✅ |

Önceki aşamalarda tamamlananlar: Observability (Aşama 4), Test mimarisi (Aşama 3), Event-driven mimari (Aşama 2), Güvenlik/doğruluk (Aşama 1), Repo hijyeni (Aşama 0).

**Sonraki adımlar:** Cloudflare CDN, Secret Manager, test coverage artırma.
