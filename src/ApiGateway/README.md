# ApiGateway

Bu klasör placeholder olarak oluşturulmuştu. Aktif API Gateway uygulaması aşağıdaki konumdadır:

```
src/Gateways/Vitrin.Gateway/
```

## Aktif Gateway Hakkında

`src/Gateways/Vitrin.Gateway` — **YARP (Yet Another Reverse Proxy)** tabanlı,
.NET 8 ile yazılmış API Gateway'dir. Docker Compose'da `vitrin-gateway` container'ı
olarak port `5000`'den dış trafiği alır, iç servislere `8080`'den yönlendirir.

### Sorumluluklar

- **Reverse proxy** — tüm istemci isteklerini ilgili mikroservise yönlendirir
- **JWT doğrulama** — her istekte Bearer token'ı validate eder, servislere güvenli erişim sağlar
- **CORS** — Next.js frontend'inden (`localhost:3000`, `vitrin-web:3000`) gelen isteklere izin verir
- **Health check** — `GET /health` endpoint'i ile container sağlık durumunu raporlar

### Route Tablosu

| Prefix | Yönlendiği Servis | Container |
|--------|-------------------|-----------|
| `/api/auth/**` | Auth API | `vitrin-auth:8080` |
| `/api/products/**` `/api/topics/**` `/api/collections/**` | Product API | `vitrin-product:8080` |
| `/api/votes/**` | Voting API | `vitrin-voting:8080` |
| `/api/comments/**` | Comment API | `vitrin-comment:8080` |
| `/api/notifications/**` | Notification API | `vitrin-notification:8080` |
| `/api/analytics/**` | Analytics API | `vitrin-analytics:8080` |
| `/api/ai/**` | AI API | `vitrin-ai:8080` |

### Yapı

```
src/Gateways/Vitrin.Gateway/
├── Program.cs                    # YARP + JWT + CORS yapılandırması
├── appsettings.json              # Route ve cluster tanımları (local)
├── appsettings.Docker.json       # Docker container adresleri
├── Dockerfile                    # Multi-stage build, non-root user
└── Vitrin.Gateway.csproj
```

### Başlatmak için

```bash
# Sadece gateway (docker)
docker compose up -d vitrin-gateway

# Tüm stack
docker compose up -d --build

# Local geliştirme (start-dev.ps1 ile — gateway Docker, servisler hot-reload)
cd ../..
./start-dev.ps1
```
