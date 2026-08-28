# Cloud & PaaS Architecture

## 📋 Executive Summary

Vitrin projesi, **cost-conscious hybrid cloud** stratejisi ile production-grade altyapı sunar:
- **Total Monthly Cost:** ~$30 (sadece EC2 compute)
- **PaaS Utilization:** 7 free-tier managed service
- **Self-Hosted Stack:** PostgreSQL, Redis, Kafka, Observability
- **Architecture Style:** Selective PaaS (Level 2.5)

---

## 🏗️ Infrastructure Stack

### Compute & Orchestration

```yaml
Provider: AWS EC2
Instance Type: t3.medium
  - vCPU: 2
  - RAM: 4 GB
  - Storage: 30 GB EBS
  - Network: Up to 5 Gbps
  
OS: Ubuntu 22.04 LTS
Container Runtime: Docker 24.x
Orchestration: Docker Compose 2.x
Reverse Proxy: Nginx 1.24.x

Cost: ~$30/month
```

**Why EC2 over ECS/EKS:**
- Full control over configuration
- Zero orchestration costs
- Learning experience (production deployment)
- Easy migration path (Docker Compose → Kubernetes)

---

## ☁️ PaaS Services Breakdown

### 1. GitHub Container Registry (GHCR)

**Purpose:** Container image storage and distribution

```yaml
Usage:
  - 9 microservices (auth, product, voting, comment, notification, analytics, ai, gateway, web)
  - Image naming: ghcr.io/{owner}/vitrin-{service}:{tag}
  - Tags: commit-sha (traceability) + latest (current prod)

Features:
  ✅ Unlimited storage (public images)
  ✅ Anonymous pull (no rate limit for public)
  ✅ GitHub native integration
  ✅ Package-level permissions
  ✅ Vulnerability scanning (via GitHub Security)

Cost: $0 (public repositories)
```

**Image Optimization:**
```dockerfile
# Multi-stage build pattern
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Vitrin.Auth.Api.dll"]

# Result: ~150MB (instead of 900MB with full SDK)
```

---

### 2. GitHub Actions

**Purpose:** CI/CD automation

```yaml
Pipeline Stages:
  1. check-deploy    → Conditional deployment trigger
  2. test            → Backend (xUnit) + Frontend (Vitest)
  3. security-scan   → .NET vulnerabilities, pnpm audit, Trivy
  4. build           → 9 images (matrix build, parallel)
  5. image-scan      → Trivy CRITICAL/HIGH + SBOM (Syft)
  6. deploy          → SSH to EC2, rolling restart, health checks
  7. smoke-test      → Playwright production tests
  8. rollback        → Auto-revert on failure

Free Tier:
  ✅ 2000 minutes/month (private repos)
  ✅ Unlimited (public repos) ← Vitrin kullanıyor
  ✅ Matrix builds (9 services parallel)
  ✅ BuildKit cache (layer caching)
  ✅ Artifact storage (30-90 days)

Cost: $0
```

**Optimization Techniques:**
```yaml
Cache Strategy:
  - Docker layer cache: type=gha (GitHub Actions cache)
  - Node.js dependencies: pnpm cache
  - .NET packages: NuGet cache
  
Build Time:
  - Matrix parallelization (9 services → 6 minutes)
  - Layer caching (rebuild only changed layers)
  - Throttled deployment (avoid EC2 I/O spike)
```

---

### 3. Cloudflare (Free Tier)

**Purpose:** CDN, DDoS protection, SSL termination

```yaml
Features:
  ✅ Global CDN (200+ cities)
  ✅ Unmetered DDoS protection (Layer 3/4/7)
  ✅ Universal SSL (auto-provisioning)
  ✅ Web Application Firewall (WAF)
  ✅ Bot management (basic)
  ✅ Page Rules (3 rules free)
  ✅ DNS management (DNSSEC)
  ✅ Analytics (basic)

Configuration:
  - SSL/TLS: Full (strict)
  - Security Level: Medium
  - Cache Level: Standard
  - Browser Cache TTL: 4 hours
  - Always Use HTTPS: On
  - Auto Minify: JS, CSS, HTML

Cost: $0
```

**Traffic Flow:**
```
[Client Request]
    ↓
[Cloudflare Edge] ← Cache static assets, block malicious traffic
    ↓
[Origin: AWS EC2 (Nginx)]
    ↓
[YARP Gateway]
    ↓
[Microservices]
```

---

### 4. Resend

**Purpose:** Transactional email delivery

```yaml
Free Tier Limits:
  - 100 emails/day (3000/month)
  - 1 domain
  - API access
  
Use Cases:
  - Email confirmation (registration)
  - Password reset
  - Notification digests (15-minute batches)
  - Product launch announcements

Implementation:
  Service: Auth, Notification
  Client: HttpClient (.NET)
  Endpoint: https://api.resend.com/emails
  Auth: Bearer token
  Features: Idempotency-Key (duplicate prevention)

Cost: $0
```

**Email Templates:**
```csharp
// Example: Welcome email
{
  "from": "Vitrin <onboarding@resend.dev>",
  "to": ["user@example.com"],
  "subject": "Vitrin'e hoş geldiniz!",
  "html": "<h1>Email onayı</h1><a href='{confirmUrl}'>Onayla</a>"
}
```

---

### 5. Cloudinary

**Purpose:** Image CDN and processing

```yaml
Free Tier Limits:
  - 25 GB storage
  - 25 GB bandwidth/month
  - 25 transformation credits

Use Cases:
  - Product images (upload, transform, deliver)
  - User avatars
  - Automatic WebP/AVIF conversion
  - Responsive image generation (srcset)

Integration:
  Frontend: Next.js Image component
  Remote Pattern: res.cloudinary.com
  Upload: Direct client-side upload (unsigned preset)

Cost: $0
```

**Next.js Configuration:**
```typescript
// next.config.mjs
export default {
  images: {
    remotePatterns: [
      { protocol: "https", hostname: "res.cloudinary.com" }
    ]
  }
}

// Usage
<Image 
  src="https://res.cloudinary.com/{cloud}/image/upload/v1/{public_id}.jpg"
  width={400} 
  height={300}
  alt="Product"
/>
```

---

### 6. Google Gemini API

**Purpose:** AI/LLM capabilities

```yaml
Free Tier Limits:
  - 60 requests/minute
  - 1500 requests/day

Use Cases:
  - Product description analysis
  - Automatic tagging
  - Content summarization
  - Smart recommendations

Model: gemini-1.5-flash
Quota Management: 
  - Application-level: 10 requests/day/user
  - Gateway rate limit: 5 requests/minute

Implementation:
  Service: AI Service
  Endpoint: https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent
  Database: SQLite (quota tracking)

Cost: $0
```

---

### 7. OAuth Providers (Google, GitHub)

**Purpose:** Social authentication

```yaml
Google OAuth 2.0:
  - Client ID + Secret
  - Server-side token verification
  - Profile fetch (email, name, avatar)
  
GitHub OAuth:
  - Client ID + Secret
  - User profile + public repos
  
Implementation:
  Frontend: NextAuth.js
  Backend: External identity verification
  Flow: Authorization Code + PKCE

Cost: $0
```

---

### 8. Vercel Analytics

**Purpose:** Frontend performance monitoring

```yaml
Metrics:
  - Web Vitals (LCP, FID, CLS, FCP, TTFB)
  - Page views
  - User demographics
  - Performance scores

Integration:
  Package: @vercel/analytics
  Usage: Self-hosted deployment (no Vercel hosting required)
  Data: Sent to Vercel edge

Cost: $0 (SDK usage, not hosting)
```

---

## 🏠 Self-Hosted Infrastructure

### Why Self-Host Core Components?

| Component | Monthly Savings | Control Benefits |
|---|---|---|
| PostgreSQL | ~$40 (vs RDS) | Performance tuning, custom extensions, backup control |
| Redis | ~$20 (vs ElastiCache) | Cache pattern flexibility, memory optimization |
| Kafka | ~$200 (vs MSK) | Event sourcing architecture, partition control |
| Observability | ~$100 (vs Datadog) | Unlimited metrics/logs, custom dashboards |
| **Total Savings** | **~$360/month** | Full infrastructure control |

---

### Self-Hosted Stack Details

#### PostgreSQL (Docker)

```yaml
Version: 16-alpine
Databases: vitrin_auth, vitrin_product, vitrin_comment
Configuration:
  - shared_buffers: 1GB
  - effective_cache_size: 3GB
  - max_connections: 200
  - work_mem: 4MB

Backup:
  - Schedule: Daily @ 02:00 UTC
  - Retention: 7 days
  - Format: pg_dump (gzip)
  - Optional: S3 upload (ready, not active)

Monitoring:
  - postgres-exporter (Prometheus)
  - Query performance tracking
  - Connection pool metrics
```

#### Redis (Docker)

```yaml
Version: 7-alpine
Use Cases:
  - JWT token blacklist
  - Distributed rate limiting
  - Product listing cache (future)
  - Session storage

Configuration:
  - maxmemory: 512MB
  - maxmemory-policy: allkeys-lru
  - Persistence: RDB + AOF (hybrid)

Monitoring:
  - redis-exporter (Prometheus)
  - Hit/miss ratio
  - Memory usage
  - Command latency
```

#### Kafka + Zookeeper (Docker)

```yaml
Version: Confluent 7.6.0
Topics:
  - user-events
  - product-events
  - voting-events
  - comment-events
  - notification-events

Configuration:
  - Replication Factor: 1 (single broker)
  - Auto-create topics: Enabled
  - Retention: 7 days

Patterns:
  - Transactional Outbox
  - At-least-once delivery
  - Dead Letter Queue (DLQ)
  - Consumer groups

Monitoring:
  - kafka-exporter (Prometheus)
  - Topic lag
  - Message rate
  - Broker health
```

#### Observability Stack (Docker)

```yaml
Prometheus:
  - Retention: 7 days
  - Scrape interval: 15s
  - Targets: All services, exporters (postgres, redis, kafka)

Grafana:
  - Dashboards: Pre-configured
  - Data source: Prometheus
  - Alerting: Configured (not active)

Jaeger:
  - Distributed tracing
  - Sampling: 100% (dev), 10% (prod recommended)
  - Storage: In-memory (ephemeral)

Elasticsearch + Kibana:
  - Log aggregation
  - Retention: 7 days
  - Index: vitrin-logs-{service}-{yyyy-MM}
```

---

## 💰 Cost Analysis

### Current Monthly Costs

| Item | Cost |
|---|---|
| AWS EC2 (t3.medium) | $30.37 |
| AWS EBS (30 GB) | $2.40 |
| AWS Data Transfer (10 GB) | ~$1.00 |
| **PaaS Services** | **$0.00** |
| **Total** | **~$34/month** |

### Cost Comparison: Self-Host vs Managed

| Service | Self-Host | Managed (AWS) | Savings |
|---|---|---|---|
| PostgreSQL | Included | RDS db.t3.micro: $15-40 | $15-40 |
| Redis | Included | ElastiCache: $15-20 | $15-20 |
| Kafka | Included | MSK: $200+ | $200+ |
| Observability | Included | CloudWatch+X-Ray: $50+ | $50+ |
| Load Balancer | Nginx (included) | ALB: $16 | $16 |
| **Total** | **$34** | **$330-400** | **$300-370** |

### Scalability Cost Projection

| Scale Level | Infrastructure | Monthly Cost |
|---|---|---|
| **Current** (MVP) | 1x t3.medium | ~$34 |
| **Small** (1k users) | 2x t3.medium + ALB | ~$80 |
| **Medium** (10k users) | 3x t3.large + RDS + ElastiCache | ~$250 |
| **Large** (100k users) | EKS + RDS Multi-AZ + MSK | ~$800 |

---

## 🎯 Architecture Decision Rationale

### Selective PaaS Strategy (Level 2.5)

**Philosophy:** Use PaaS for **commodity services**, self-host for **differentiation**.

#### PaaS Adoption Criteria:

✅ **Use PaaS when:**
- Service is commodity (email, CDN, image processing)
- Maintenance burden is high
- Expertise is specialized (DDoS protection)
- Free tier covers usage
- Vendor lock-in risk is low

❌ **Self-host when:**
- Data sovereignty is critical (databases)
- Cost savings are significant (Kafka: $200/mo → $0)
- Learning value is high (observability)
- Control over performance tuning is needed
- Migration flexibility is important

---

### Why Not Kubernetes?

**Current Decision: Docker Compose**

| Aspect | Docker Compose | Kubernetes (EKS) |
|---|---|---|
| **Complexity** | Low | High |
| **Cost** | $0 | $75/mo (control plane) + $60+ (nodes) |
| **Learning Curve** | Minimal | Steep |
| **Overkill for 9 services** | No | Yes |
| **Production-grade** | Yes (with proper setup) | Yes |

**Future Consideration:** Migrate to Kubernetes when:
- Scale requires auto-scaling (HPA)
- Multi-zone deployment needed
- Service mesh benefits outweigh complexity
- Team size justifies operational overhead

---

## 🔐 Security Layers

### Multi-Layer Defense

```
┌────────────────────────────────────────────┐
│ Layer 1: Cloudflare                         │
│ - DDoS protection (L3/L4/L7)                │
│ - Bot filtering                             │
│ - WAF rules                                 │
│ - SSL/TLS termination                       │
└────────────────────────────────────────────┘
             ↓
┌────────────────────────────────────────────┐
│ Layer 2: Nginx (EC2)                        │
│ - Security headers (CSP, HSTS, etc.)        │
│ - Endpoint protection (/metrics → 403)      │
│ - Rate limiting (backup)                    │
│ - Request logging                           │
└────────────────────────────────────────────┘
             ↓
┌────────────────────────────────────────────┐
│ Layer 3: YARP Gateway                       │
│ - JWT validation (HMAC SHA256)              │
│ - Token blacklist (Redis)                   │
│ - Distributed rate limiting (Redis Lua)     │
│ - Circuit breaker (Polly)                   │
│ - CORS policies                             │
└────────────────────────────────────────────┘
             ↓
┌────────────────────────────────────────────┐
│ Layer 4: Microservices                      │
│ - Domain authorization                      │
│ - Input validation                          │
│ - SQL injection prevention (parameterized)  │
│ - XSS prevention (output encoding)          │
└────────────────────────────────────────────┘
```

---

## 📊 Monitoring & Alerting

### Metrics Collection

```yaml
Business Metrics:
  - vitrin_user_registrations_total
  - vitrin_product_submissions_total
  - vitrin_votes_total
  - vitrin_comments_total

System Metrics:
  - CPU, memory, disk usage
  - Network I/O
  - Container restarts

Application Metrics:
  - HTTP request duration (p50, p95, p99)
  - Error rate (by endpoint)
  - Database query duration
  - Cache hit/miss ratio
  - Event processing lag

Infrastructure Metrics:
  - PostgreSQL: connections, transactions, locks
  - Redis: memory usage, commands/sec, hit rate
  - Kafka: consumer lag, throughput, partition health
```

### Dashboards

**Grafana Dashboards (Pre-configured):**
1. **Production Overview** — All services health, request rate, error rate
2. **Business KPIs** — User growth, product submissions, engagement
3. **System Resources** — CPU, memory, disk, network
4. **Database Performance** — Query duration, connection pool, slow queries
5. **Event Processing** — Kafka lag, outbox dispatch rate, DLQ monitoring

---

## 🚀 Deployment Process

### Rolling Deployment Strategy

```yaml
Deploy Trigger: git commit -m "feat: xyz [deploy]"

Steps:
  1. Pre-deployment:
     - Save current image tags (rollback reference)
     - Pull new images (throttled, sequential)
  
  2. Rolling restart:
     - Service order: auth → product → voting → comment → notification → analytics → ai → gateway → web
     - Per service: docker compose up -d --no-deps {service}
     - Health check: 12 retries, 5s interval, curl http://localhost:8080/health
  
  3. Post-deployment:
     - Nginx reload
     - Verify all containers running
     - Log image tags
  
  4. Smoke test:
     - Wait 60s (stabilization)
     - Playwright smoke tests (critical paths)
     - Accessibility checks (axe-core)
  
  5. Rollback (on failure):
     - Auto-triggered if smoke test fails
     - Pull previous image tags
     - Restart affected services
     - Verify health
```

### Zero-Downtime Considerations

- **Health checks** enforce readiness before traffic routing
- **Nginx reload** (not restart) maintains connections
- **Database migrations** run before deployment (backward compatible)
- **Feature flags** enable gradual rollout

---

## 🎓 Learning & Portfolio Value

### Skills Demonstrated

**Infrastructure:**
- ✅ AWS EC2 deployment and management
- ✅ Docker multi-container orchestration
- ✅ Nginx reverse proxy configuration
- ✅ SSL/TLS certificate management

**Cost Optimization:**
- ✅ Free-tier PaaS maximization
- ✅ Self-hosting economic analysis
- ✅ Resource utilization optimization
- ✅ Build cache strategies

**DevOps:**
- ✅ CI/CD pipeline automation (GitHub Actions)
- ✅ Container security scanning (Trivy)
- ✅ Rolling deployment with health checks
- ✅ Auto-rollback on failure

**Observability:**
- ✅ Prometheus metrics collection
- ✅ Grafana dashboard creation
- ✅ Distributed tracing (Jaeger)
- ✅ Centralized logging (Elasticsearch)

**Security:**
- ✅ Multi-layer defense strategy
- ✅ JWT authentication + blacklist
- ✅ Rate limiting (distributed)
- ✅ Vulnerability scanning (CI/CD)

---

## 📈 Future Enhancements (Cost-Permitting)

### Phase 1: AWS Integration ($5-10/mo additional)

```yaml
1. S3 Backup Integration (READY)
   - Automated PostgreSQL backups → S3
   - Cost: ~$3/mo (storage + transfer)
   - Script: scripts/backup-postgres.sh (S3 support coded)

2. Parameter Store (Secrets)
   - Centralized secret management
   - Cost: $0 (standard parameters free)
   - Benefit: Remove .env files from production

3. CloudWatch Logs
   - Centralized log aggregation
   - Cost: ~$5/mo (5 GB logs)
   - Benefit: Replace self-hosted Elasticsearch
```

### Phase 2: Horizontal Scaling ($50-100/mo additional)

```yaml
1. Multi-EC2 Setup
   - 2-3 EC2 instances
   - Application Load Balancer (ALB)
   - Shared PostgreSQL (single master, read replicas)

2. Redis Cluster
   - Sentinel or Cluster mode
   - High availability

3. Database Read Replicas
   - PostgreSQL replication
   - Offload read queries
```

### Phase 3: Managed Services (Kubernetes) ($150-200/mo additional)

```yaml
1. AWS EKS (Managed Kubernetes)
   - Control plane: $75/mo
   - Worker nodes: 2x t3.medium ($60/mo)
   - Auto-scaling, rolling updates

2. RDS PostgreSQL (Multi-AZ)
   - db.t3.micro: $30/mo
   - Automatic backups, point-in-time recovery

3. ElastiCache Redis
   - cache.t3.micro: $15/mo
   - Automatic failover
```

---

## 🎯 Conclusion

Vitrin's cloud architecture demonstrates **strategic pragmatism**:

✅ **Cost-effective:** $34/mo for production-grade infrastructure
✅ **Scalable:** Clear migration path (Docker Compose → Kubernetes)
✅ **Secure:** Multi-layer defense + automated scanning
✅ **Observable:** Full-stack monitoring with free tools
✅ **Maintainable:** Self-hosted components under control
✅ **Portfolio-ready:** Demonstrates real-world cloud engineering

**Key Takeaway:** Not every project needs Kubernetes or fully-managed services. Smart architecture choices can deliver production quality at student-friendly costs.

---

**Last Updated:** January 2025
**Maintainer:** Cagatay Kayalar
**Questions:** Open an issue or discussion in the repository
