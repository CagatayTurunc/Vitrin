# Vitrin Observability Stack

Bu klasör Vitrin mikro servis platformu için kapsamlı monitoring ve observability altyapısını içerir.

## 🏗️ Mimarı

### Bileşenler

| Bileşen | Port | Açıklama |
|---------|------|----------|
| **Jaeger** | 16686 | Distributed tracing ve request flow analizi |
| **Prometheus** | 9090 | Metrics collection ve time series database |
| **Grafana** | 3001 | Dashboards ve görselleştirme |
| **Elasticsearch** | 9200 | Log aggregation ve full-text search |
| **Kibana** | 5601 | Log analysis ve visualization |

### Exporters

| Exporter | Port | Monitörlediği Servis |
|----------|------|---------------------|
| **Postgres Exporter** | 9187 | PostgreSQL veritabanı metrikleri |
| **Redis Exporter** | 9121 | Redis cache performansı |
| **Kafka Exporter** | 9308 | Kafka message broker metrikleri |

## 🚀 Hızlı Başlangıç

### 1. Environment Konfigürasyonu

`.env` dosyanızda observability ayarlarını yapın:

```bash
# Grafana Admin
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=your_strong_password

# Log Level
LOG_LEVEL=Information
```

### 2. Stack'i Başlatma

```bash
# Tüm servisleri başlat
docker compose up -d

# Sadece observability servisleri
docker compose up -d jaeger prometheus grafana elasticsearch kibana
```

### 3. Access URLs

- **Grafana Dashboards**: http://localhost:3001
- **Jaeger Tracing**: http://localhost:16686  
- **Prometheus**: http://localhost:9090
- **Kibana Logs**: http://localhost:5601
- **Elasticsearch**: http://localhost:9200

## 📊 Dashboards

### Pre-built Dashboards

1. **System Overview** (`vitrin-overview.json`)
   - Request rates ve response times
   - Error rates
   - Active users
   - Cache hit rates
   - Database query performance

2. **Business Metrics** (`vitrin-business-metrics.json`)
   - User registrations
   - Product submissions
   - Voting activity
   - Authentication success rates
   - Comments activity

### Custom Metrics

Vitrin servisleri aşağıdaki özel metrikleri sunar:

```csharp
// Business Metrics
vitrin_user_registrations_total
vitrin_product_submissions_total  
vitrin_votes_total
vitrin_comments_total
vitrin_auth_attempts_total

// Performance Metrics
vitrin_request_duration_seconds
vitrin_database_query_duration_seconds
vitrin_cache_operation_duration_seconds
vitrin_event_processing_duration_seconds

// System Metrics
vitrin_active_users
vitrin_cache_hits_total
vitrin_cache_misses_total
vitrin_errors_total
```

## 🔍 Tracing

### Distributed Tracing Flow

1. **Gateway** - İlk giriş noktası
2. **Auth Service** - Kimlik doğrulama
3. **Business Services** - Product, Voting, Comment vb.
4. **Event Bus** - Kafka üzerinden async messaging
5. **Database** - PostgreSQL/SQLite operations

### Trace Attributes

Her trace aşağıdaki attributeları içerir:

```csharp
- service.name
- service.version  
- deployment.environment
- user.id
- correlation.id
- operation.type
- http.method
- http.status_code
```

## 📝 Logging

### Log Levels

- **Debug**: Development detayları
- **Information**: Normal operasyonlar
- **Warning**: Potansiyel problemler
- **Error**: Hatalar ve exceptionlar

### Log Sinks

1. **Console**: Development ortamı
2. **File**: Rotating log files (`logs/` klasörü)
3. **Elasticsearch**: Production log aggregation

### Structured Logging

```json
{
  "@timestamp": "2024-08-13T10:30:00.000Z",
  "level": "Information",
  "messageTemplate": "Business operation: {Operation} executed by {UserId}",
  "message": "Business operation: UserRegistration executed by user123",
  "properties": {
    "ServiceName": "Auth",
    "Operation": "UserRegistration", 
    "UserId": "user123",
    "CorrelationId": "abc123",
    "TraceId": "def456"
  }
}
```

## 🔧 Konfigürasyon

### Prometheus Configuration

`prometheus/prometheus.yml` dosyasında:

- Scrape interval: 15s
- Retention: 7 gün
- Tüm Vitrin servisleri auto-discovery

### Grafana Data Sources

- **Prometheus**: Metrics
- **Jaeger**: Tracing
- **Elasticsearch**: Logs

## 🚨 Alerting

### Recommended Alerts

1. **High Error Rate**: >5% error rate 5 dakika boyunca
2. **Slow Response**: >2s response time P95
3. **Low Cache Hit**: <80% cache hit rate
4. **High Database Load**: >80% connection pool usage
5. **Service Down**: Health check failure

### Alert Channels

- Slack webhook
- Email notifications  
- PagerDuty integration

## 🏥 Health Checks

Her servis aşağıdaki health check endpoint'lerini sunar:

- `/health` - Genel sağlık durumu
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Health Check Components

- Database connectivity
- Redis availability
- Kafka broker connection
- External service dependencies

## 🔒 Security

### Güvenlik Considerations

- Grafana admin credentials güvenli şekilde saklanmalı
- Elasticsearch authentication production'da aktif edilmeli
- Sensitive data metric'lerden exclude edilmeli
- Log masking için PII data kontrolleri

## 🎯 Production Recommendations

### Scaling

- **Prometheus**: HA setup with remote storage
- **Grafana**: Load balancer arkasında multiple instance
- **Elasticsearch**: Cluster setup minimum 3 node

### Retention Policies

- **Metrics**: 30 gün (production)  
- **Traces**: 7 gün
- **Logs**: 90 gün

### Performance Tuning

```yaml
# Elasticsearch
ES_JAVA_OPTS: "-Xms2g -Xmx2g"

# Prometheus  
storage.tsdb.retention.time: 30d
storage.tsdb.retention.size: 10GB
```

## 🛠️ Troubleshooting

### Common Issues

1. **Metrics not appearing**: Check service discovery ve scrape config
2. **Traces missing**: Verify Jaeger endpoint configuration
3. **Logs not indexed**: Elasticsearch template ve mapping kontrolü
4. **Dashboard errors**: Data source connectivity kontrolü

### Debug Commands

```bash
# Service logs
docker compose logs vitrin-auth

# Prometheus targets
curl http://localhost:9090/api/v1/targets

# Elasticsearch indices  
curl http://localhost:9200/_cat/indices

# Jaeger health
curl http://localhost:16686/api/v1/health
```

## 📈 Monitoring Best Practices

1. **Golden Signals**: Latency, Traffic, Errors, Saturation
2. **Business Metrics**: KPI tracking için custom metrics
3. **Correlation**: Logs, metrics ve traces arasında correlation ID kullanımı
4. **Sampling**: High traffic'te trace sampling oranları
5. **Context Propagation**: Microservice'ler arası trace context iletimi

## 🔄 Maintenance

### Regular Tasks

- Elasticsearch indices cleanup
- Prometheus data retention check
- Grafana dashboard updates
- Alert rule validations

### Backup Strategy

- Grafana dashboard export
- Prometheus config backup
- Alert rules version control